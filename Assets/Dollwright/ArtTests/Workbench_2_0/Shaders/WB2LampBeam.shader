Shader "Dollwright/Workbench2/LampBeam"
{
 Properties
 {
  [HDR] _BeamColor("Warm scattering",Color)=(1,.76,.42,1)
  _Density("Beam visibility",Range(0,1))=.28
  _BeamOrigin("Aperture position",Vector)=(0,0,0,0)
  _BeamAxis("Direction and length",Vector)=(0,-1,0,1)
  _BeamRadii("Start and end radius",Vector)=(.10,.4,0,0)
 }
 SubShader
 {
  Tags{"RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent+10"}
  Pass
  {
   Name "SoftLampShaft"
   Blend One One
   ZWrite Off ZTest Always Cull Front
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma target 3.0
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
   CBUFFER_START(UnityPerMaterial)
   half4 _BeamColor;float _Density;float4 _BeamOrigin;float4 _BeamAxis;float4 _BeamRadii;
   CBUFFER_END
   struct A{float4 positionOS:POSITION;};
   struct V{float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;};
   V vert(A a){V o;o.positionCS=TransformObjectToHClip(a.positionOS.xyz);o.positionWS=TransformObjectToWorld(a.positionOS.xyz);return o;}
   half4 frag(V i):SV_Target
   {
    float3 ro=GetCameraPositionWS();float3 rd=normalize(i.positionWS-ro);
    float len=_BeamAxis.w;float3 axis=normalize(_BeamAxis.xyz);
    float3 centre=_BeamOrigin.xyz+axis*(len*.5);
    float bound=sqrt(len*len*.25+_BeamRadii.y*_BeamRadii.y);
    float3 oc=ro-centre;float b=dot(oc,rd);float disc=b*b-dot(oc,oc)+bound*bound;
    if(disc<=0)return 0;
    float nearT=max(0,-b-sqrt(disc));float farT=-b+sqrt(disc);
    float2 screenUV=GetNormalizedScreenSpaceUV(i.positionCS);
    float depth=SampleSceneDepth(screenUV);
    #if !UNITY_REVERSED_Z
    depth=lerp(UNITY_NEAR_CLIP_VALUE,1,depth);
    #endif
    float3 opaque=ComputeWorldSpacePosition(screenUV,depth,UNITY_MATRIX_I_VP);
    farT=min(farT,distance(ro,opaque));
    if(farT<=nearT)return 0;
    float stepSize=(farT-nearT)/32;float sum=0;
    [unroll]for(int k=0;k<32;k++)
    {
     float3 p=ro+rd*(nearT+(k+.5)*stepSize)-_BeamOrigin.xyz;
     float axial=dot(p,axis);float t=axial/len;
     float r=lerp(_BeamRadii.x,_BeamRadii.y,saturate(t));
     float radial=length(p-axis*axial)/r;
     float edge=1-smoothstep(.35,1,radial);
     float ends=smoothstep(0,.055,t)*(1-smoothstep(.82,1,t));
     sum+=edge*ends*stepSize;
    }
    return half4(_BeamColor.rgb*(1-exp(-sum*_Density)),0);
   }
   ENDHLSL
  }
 }
}
