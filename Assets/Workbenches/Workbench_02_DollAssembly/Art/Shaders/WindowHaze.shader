Shader "Dollwright/Workbench2/WindowHaze"
{
    Properties
    {
        _Tint("Scattered light", Color) = (1,.88,.65,1)
        _Density("Haze density", Range(0,.3)) = .09
        _Window("Window centre XYZ and throw in Z", Vector) = (-1.9,2.365,2.54,2.95)
        _Size("Opening width and height", Vector) = (2.5,2.31,0,0)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend One One
            Cull Front
            ZWrite Off
            ZTest Always
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
            half4 _Tint;
            float _Density;
            float4 _Window, _Size;
            CBUFFER_END

            struct Varyings { float4 clip : SV_POSITION; float3 world : TEXCOORD0; };
            Varyings vert(float3 position : POSITION)
            {
                Varyings o;
                o.world = TransformObjectToWorld(position);
                o.clip = TransformWorldToHClip(o.world);
                return o;
            }
            // Express a point by the window pane it receives light from and its distance inside.
            float3 windowSpace(float3 p, float2 slope)
            {
                float depth = _Window.z - p.z;
                return float3((p.xy - depth * slope - _Window.xy) / _Size.xy + .5, depth / _Window.w);
            }
            half4 frag(Varyings i) : SV_Target
            {
                Light sun = GetMainLight();
                if (sun.direction.z <= .01) return 0;
                float2 slope = -sun.direction.xy / sun.direction.z;
                float3 eye = GetCameraPositionWS();
                float3 ray = normalize(i.world - eye);
                float3 start = windowSpace(eye, slope);
                float3 dir = windowSpace(eye + ray, slope) - start;
                float3 invDir = lerp(-1.,1.,step(0.,dir)) / max(abs(dir), .00001);
                float3 a = -start * invDir, b = (1. - start) * invDir;
                float3 nearFaces = min(a,b), farFaces = max(a,b);
                float nearT = max(0.,max(nearFaces.x,max(nearFaces.y,nearFaces.z)));
                float farT = min(farFaces.x,min(farFaces.y,farFaces.z));
                float2 uv = GetNormalizedScreenSpaceUV(i.clip);
                float depth = SampleSceneDepth(uv);
                #if !UNITY_REVERSED_Z
                    depth = lerp(UNITY_NEAR_CLIP_VALUE,1.,depth);
                #endif
                float3 surface = ComputeWorldSpacePosition(uv,depth,UNITY_MATRIX_I_VP);
                farT = min(farT,distance(eye,surface));
                if (farT <= nearT) return 0;
                const int steps = 12;
                float stride = (farT-nearT)/steps, integral=0;
                [unroll] for(int k=0;k<steps;k++)
                {
                    float t = nearT + (k+.5)*stride;
                    float3 samplePos = eye + ray*t;
                    float3 q = start+dir*t;
                    float2 rim = min(q.xy,1.-q.xy);
                    float fade = smoothstep(0.,.11,min(rim.x,rim.y))*(1.-smoothstep(.26,1.,q.z));
                    float shadow = MainLightRealtimeShadow(TransformWorldToShadowCoord(samplePos));
                    integral += fade * shadow;
                }
                return half4(_Tint.rgb * sun.color * (1.-exp(-integral*stride*_Density)),0);
            }
            ENDHLSL
        }
    }
}
