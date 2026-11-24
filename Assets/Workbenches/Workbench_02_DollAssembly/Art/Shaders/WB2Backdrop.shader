Shader "Dollwright/Workbench2/SoftBackdrop"
{
 Properties{_BaseMap("Background",2D)="white"{} _BaseColor("Tint",Color)=(1,1,1,1) _Blur("Blur in texels",Range(0,12))=2}
 SubShader
 {
  Tags{"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Background"}
  Pass
  {
   Cull Off ZWrite Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
   CBUFFER_START(UnityPerMaterial)
   float4 _BaseMap_ST;float4 _BaseMap_TexelSize;half4 _BaseColor;float _Blur;
   CBUFFER_END
   struct A{float4 positionOS:POSITION;float2 uv:TEXCOORD0;};
   struct V{float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;};
   V vert(A a){V o;o.positionCS=TransformObjectToHClip(a.positionOS.xyz);o.uv=TRANSFORM_TEX(a.uv,_BaseMap);return o;}
   half4 frag(V i):SV_Target
   {
    float2 d=_BaseMap_TexelSize.xy*_Blur;half3 c=0;
    for(int x=-2;x<=2;x++)for(int y=-2;y<=2;y++)c+=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv+float2(x,y)*d).rgb;
    return half4(c/25*_BaseColor.rgb,1);
   }
   ENDHLSL
  }
 }
}
