// Code provided by Null Software - https://nullsoftware.net
// Please note that shader preview in Inspector can be buggy!!!

Shader "AngusHCY/PostProcess/EdgeLine"
{
    Properties
    {
        _LineThickness ("Line Thickness", Int) = 1
        _LineColor ("Line Color", Color) = (0, 0, 0, 1)
        _NoiseScale ("Noise Scale", Float) = 0.1
        _NoiseIntensity ("Noise Intensity", Float) = 0 // default multiplied by 0.001 in shader
        _AnimationSpeed ("Animation Speed", Float) = 0
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


            float _LineThickness;
            float4 _LineColor;
            float _NoiseScale, _NoiseIntensity, _AnimationSpeed;

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

            // Sample depth from the camera depth texture
            float sampleDepth(float2 uv)
            {
                float rawDepth = SampleSceneDepth(uv);
                float depth01 = Linear01Depth(rawDepth, _ZBufferParams);
                return depth01;
            }
            float sampleMinDepthKernel(float2 uv, int radius)
            {
                float minDepth = 1.0;
                float2 texelSize = 1.0 / _ScreenParams.xy;
                for (int x = -radius; x <= radius; x++)
                {
                    for (int y = -radius; y <= radius; y++)
                    {
                        float2 offset = float2(x, y) * texelSize;
                        float sampleDepthValue = sampleDepth(uv + offset);
                        minDepth = min(minDepth, sampleDepthValue);
                    }
                }
                return minDepth;
            }
            float sampleDepthDifference(float2 uv, int radius)
            {
                float centerDepth = sampleDepth(uv);
                float minDepth = sampleMinDepthKernel(uv, radius);
                return centerDepth - minDepth;
            }

            // Sample depth from ddx/ddy
            float sampleDepthDDXDDY(float2 uv)
            {
                float rawDepth = SampleSceneDepth(uv);
                float depth01 = Linear01Depth(rawDepth, _ZBufferParams);
                float depthDDX = ddx(depth01);
                float depthDDY = ddy(depth01);
                return sqrt(depthDDX * depthDDX + depthDDY * depthDDY);
            }
            
            // Sample normal from the camera normal texture
            float3 sampleNormal(float2 uv)
            {
                float3 normalWS = SampleSceneNormals(uv);
                return normalWS;
            }
            float3 sampleMinNormalKernel(float2 uv, int radius)
            {
                float3 minNormal = float3(1.0, 1.0, 1.0);
                float2 texelSize = 1.0 / _ScreenParams.xy;
                for (int x = -radius; x <= radius; x++)
                {
                    for (int y = -radius; y <= radius; y++)
                    {
                        float2 offset = float2(x, y) * texelSize;
                        float3 sampleNormalValue = sampleNormal(uv + offset);
                        minNormal = min(minNormal, sampleNormalValue);
                    }
                }
                return minNormal;
            }
            float sampleNormalDifference(float2 uv, int radius)
            {
                float3 centerNormal = sampleNormal(uv);
                float3 minNormal = sampleMinNormalKernel(uv, radius);
                return length(centerNormal - minNormal);
            }

            //
            float edgeDetection(float2 uv, int radius)
            {
                float depthDiff = sampleDepthDifference(uv, _LineThickness);
                float depthValue = pow(depthDiff * 50, 5);

                float normalDiff = sampleNormalDifference(uv, _LineThickness);

                float edgeValue = max(depthValue, normalDiff);
                edgeValue = saturate(edgeValue);
                return edgeValue;
            }

            // Noise
            float Hash31(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }
            float Smooth(float t)
            {
                return t * t * (3.0 - 2.0 * t);
            }
            float Noise3D(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);

                f = float3(Smooth(f.x), Smooth(f.y), Smooth(f.z));

                float n000 = Hash31(i + float3(0, 0, 0));
                float n100 = Hash31(i + float3(1, 0, 0));
                float n010 = Hash31(i + float3(0, 1, 0));
                float n110 = Hash31(i + float3(1, 1, 0));
                float n001 = Hash31(i + float3(0, 0, 1));
                float n101 = Hash31(i + float3(1, 0, 1));
                float n011 = Hash31(i + float3(0, 1, 1));
                float n111 = Hash31(i + float3(1, 1, 1));

                float n00 = lerp(n000, n100, f.x);
                float n10 = lerp(n010, n110, f.x);
                float n01 = lerp(n001, n101, f.x);
                float n11 = lerp(n011, n111, f.x);

                float n0 = lerp(n00, n10, f.y);
                float n1 = lerp(n01, n11, f.y);

                return lerp(n0, n1, f.z);
            }

            //
            float4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                uv.y = 1.0 - uv.y; // flip Y for UV

                float3 worldPos = ReconstructWorldPosition(uv, SampleSceneDepth(uv));
                float noiseTime = floor(_Time.y * _AnimationSpeed);
                float noise = Noise3D(worldPos * _NoiseScale + noiseTime);
                noise = noise * 2.0 - 1.0; // remap to -1 ~ 1
                noise *= _NoiseIntensity * 0.001f;
                float2 noisedUV = uv + noise;

                float4 screenColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uv);
                float edgeValue = edgeDetection(noisedUV, _LineThickness);
                
                float4 color = lerp(screenColor, _LineColor, edgeValue * _LineColor.a);


                return color ;
            }
            ENDHLSL
        }
    }
}