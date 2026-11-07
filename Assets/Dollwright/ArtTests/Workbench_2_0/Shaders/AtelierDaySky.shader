Shader "Dollwright/Workbench2/DaySky" {
 SubShader { Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
  Pass { HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   struct V {float4 clip:SV_POSITION;float3 world:TEXCOORD0;};
   V vert(float3 p:POSITION){V o;o.world=TransformObjectToWorld(p);o.clip=TransformWorldToHClip(o.world);return o;}
   half4 frag(V i):SV_Target{
    float t=smoothstep(1.20,3.30,i.world.y);
    float3 sky=lerp(float3(.53,.52,.31),float3(.22,.40,.42),t);
    float2 d=(i.world.xy-float2(-2.55,2.28))/float2(.93,.67);
    return half4(sky+float3(.40,.29,.12)*exp(-dot(d,d)),1);
   }
   ENDHLSL
  }
 }
}
