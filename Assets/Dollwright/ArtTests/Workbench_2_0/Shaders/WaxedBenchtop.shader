Shader "Dollwright/Workbench2/WaxedBenchtop"
{
 Properties
 {
  [MainTexture] _BaseMap("Painted grain",2D)="white"{}
  [MainColor] _BaseColor("Wood tint",Color)=(1,1,1,1)
  _PaintColor("Old paint",Color)=(.1,.2,.16,1)
  _PaintCoverage("Paint coverage",Range(0,1))=0
  _Smoothness("Smoothness",Range(0,1))=.25
  _Metallic("Metallic",Range(0,1))=0
  _Cull("Cull",Float)=2
 }
 SubShader
 {
  Tags {"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry"}
  Pass
  {
   Name "ForwardLit"
   Tags {"LightMode"="UniversalForward"}
   Cull [_Cull]
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma target 3.0
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
   #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
   #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
   #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
   #pragma multi_compile_fragment _ _LIGHT_COOKIES
   #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
   #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
   #pragma multi_compile_fog
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
   CBUFFER_START(UnityPerMaterial)
   float4 _BaseMap_ST; half4 _BaseColor; half4 _PaintColor;
   half _PaintCoverage; half _Smoothness; half _Metallic; half _Cull;
   CBUFFER_END
   struct Attributes {float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;};
   struct Varyings {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;half3 normalWS:TEXCOORD1;float2 uv:TEXCOORD2;half fog:TEXCOORD3;};
   float hash21(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
   float noise21(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash21(i),hash21(i+float2(1,0)),f.x),lerp(hash21(i+float2(0,1)),hash21(i+1),f.x),f.y);}
   Varyings vert(Attributes a){Varyings o;VertexPositionInputs p=GetVertexPositionInputs(a.positionOS.xyz);o.positionCS=p.positionCS;o.positionWS=p.positionWS;o.normalWS=TransformObjectToWorldNormal(a.normalOS);o.uv=TRANSFORM_TEX(a.uv,_BaseMap);o.fog=ComputeFogFactor(p.positionCS.z);return o;}
   half4 frag(Varyings i):SV_Target
   {
    half3 tex=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb;
    half lum=dot(tex,half3(.299,.587,.114));
    float n=noise21(i.uv*float2(9,37))*.7+noise21(i.uv*float2(31,89))*.3;
    half mask=_PaintCoverage>0 ? smoothstep(1-_PaintCoverage-.06,1-_PaintCoverage+.06,n):0;
    half3 base=lerp(tex*_BaseColor.rgb,_PaintColor.rgb*(.72+lum*.85),mask);
    float2 p=i.positionWS.xz;
    float platter=length((p-float2(-.085,-.208))/float2(.556,.53));
    float stand=length((p-float2(-1.035,-.487))/float2(.165,.165));
    float cup=length((p-float2(-.84,-.05))/float2(.080,.080));
    float jar=length((p-float2(1.23,-.20))/float2(.054,.054));
    float nearEdge=min(min(platter,stand),min(cup,jar));
    float contact=1-smoothstep(.91,1.17,nearEdge);
    float top=smoothstep(.7,.98,normalize(i.normalWS).y);
    base*=lerp(1.,.48,top*contact);
    InputData d=(InputData)0;
    d.positionWS=i.positionWS;d.normalWS=NormalizeNormalPerPixel(i.normalWS);d.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.positionWS);
    d.shadowCoord=TransformWorldToShadowCoord(i.positionWS);d.bakedGI=SampleSH(d.normalWS);d.shadowMask=half4(1,1,1,1);
    d.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);d.vertexLighting=VertexLighting(i.positionWS,d.normalWS);
    SurfaceData s=(SurfaceData)0;s.albedo=base;s.metallic=_Metallic;s.smoothness=_Smoothness;s.normalTS=half3(0,0,1);s.occlusion=1;s.alpha=1;
    half4 c=UniversalFragmentPBR(d,s);c.rgb=MixFog(c.rgb,i.fog);return c;
   }
   ENDHLSL
  }
  UsePass "Universal Render Pipeline/Lit/ShadowCaster"
  UsePass "Universal Render Pipeline/Lit/DepthOnly"
  UsePass "Universal Render Pipeline/Lit/DepthNormals"
 }
 FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
