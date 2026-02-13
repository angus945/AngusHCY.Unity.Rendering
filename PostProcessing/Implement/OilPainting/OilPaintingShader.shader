// Code provided by Null Software - https://nullsoftware.net
// Please note that shader preview in Inspector can be buggy!!!

Shader "AngusHCY/PostProcess/OilPaintingShader"
{
    Properties
    {
        _KernelSize ("Kernel Size (N)", Int) = 7
    }
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalRenderPipeline" "RenderType" = "Transparent" "Queue" = "Transparent" }
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            // Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            // example of BlitTexture declaration
            TEXTURE2D_X(_BlitTexture);
            SAMPLER(sampler_BlitTexture);
            float4 Unity_Universal_SampleBuffer_BlitSource(float2 uv)
            {
                uint2 pixelCoords = uint2(uv * _ScreenSize.xy);
                return LOAD_TEXTURE2D_X_LOD(_BlitTexture, pixelCoords, 0);
            }
            float3 ReconstructWorldPosition(float2 uv, float rawDepth)
            {
                // UV (0~1) → NDC (-1~1)
                float4 clipPos;
                clipPos.xy = uv * 2.0 - 1.0;
                clipPos.z = rawDepth;
                clipPos.w = 1.0;

                // NDC → View Space
                float4 viewPos = mul(UNITY_MATRIX_I_P, clipPos);
                viewPos.xyz /= viewPos.w;

                // View Space → World Space
                float4 worldPos = mul(UNITY_MATRIX_I_V, float4(viewPos.xyz, 1.0));

                return worldPos.xyz;
            }

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(uint vertexID : SV_VertexID)
            {
                Varyings output;
                
                // hardcoded vertices for a screen-filling triangle (ID 0, 1, 2).
                // this covers the entire viewport and is the most reliable method when URP functions are unavailable.
                float4 positions[3] = {
                    float4(-1.0, -1.0, 0.0, 1.0), // Bottom-Left
                    float4(-1.0, 3.0, 0.0, 1.0), // Stretched Top
                    float4(3.0, -1.0, 0.0, 1.0)  // Stretched Right

                };
                
                // corresponding UVs for the three vertices (covers the 0-1 UV space)
                float2 uvs[3] = {
                    float2(0.0, 0.0),
                    float2(0.0, 2.0),
                    float2(2.0, 0.0)
                };

                output.positionHCS = positions[vertexID];
                output.uv = uvs[vertexID];

                return output;
            }

            int _KernelSize;
            struct region
            {
                float3 mean;
                float variance;
            };

            region calcRegion(int2 lower, int2 upper, int samples, float2 uv)
            {
                region r;

                float3 sum = 0.0;
                float3 squareSum = 0.0;
                float2 texelSize = 1.0 / _ScreenSize.xy;

                for (int x = lower.x; x <= upper.x; ++x)
                {
                    for (int y = lower.y; y <= upper.y; ++y)
                    {
                        float2 offset = float2(texelSize.x * x, texelSize.y * y);
                        float3 tex = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uv + offset);

                        sum += tex;
                        squareSum += tex * tex;
                    }
                }

                r.mean = sum / samples;

                float3 variance = abs((squareSum / samples) - (r.mean * r.mean));
                r.variance = length(variance);

                return r;
            }
            

            float4 frag(Varyings input) : SV_Target
            {
                float2 uv = float2(input.uv.x, 1.0 - input.uv.y); // flip Y for UV
                float3 tex = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uv);

                int upper = (_KernelSize - 1) / 2;
                int lower = -upper;

                int samples = (upper + 1) * (upper + 1);

                region regionA = calcRegion(int2(lower, lower), int2(0, 0), samples, uv);
                region regionB = calcRegion(int2(0, lower), int2(upper, 0), samples, uv);
                region regionC = calcRegion(int2(lower, 0), int2(0, upper), samples, uv);
                region regionD = calcRegion(int2(0, 0), int2(upper, upper), samples, uv);

                float3 col = regionA.mean;
                float minVar = regionA.variance;

                float testVal;

                // Test region B.
                testVal = step(regionB.variance, minVar);
                col = lerp(col, regionB.mean, testVal);
                minVar = lerp(minVar, regionB.variance, testVal);

                // Test region C.
                testVal = step(regionC.variance, minVar);
                col = lerp(col, regionC.mean, testVal);
                minVar = lerp(minVar, regionC.variance, testVal);

                // Text region D.
                testVal = step(regionD.variance, minVar);
                col = lerp(col, regionD.mean, testVal);

                return float4(col, 1.0);

                
                // return float4(tex, 1.0);

            }
            ENDHLSL
        }
    }
}