// Code provided by Null Software - https://nullsoftware.net
// Please note that shader preview in Inspector can be buggy!!!

Shader "AngusHCY/PostProcess/FullscreenShaderTemplate_DoNothing"
{
    Properties { }
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


            float4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                uv.y = 1.0 - uv.y; // flip Y for UV

                // 取螢幕顏色
                float4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, uv);
                return color;

                // 取 depth（0~1 非線性，轉成線性 0~1）
                float rawDepth = SampleSceneDepth(uv);
                float depth01 = Linear01Depth(rawDepth, _ZBufferParams);

                // 取 world normal
                float3 normalWS = SampleSceneNormals(uv);
                float3 absNormalWS = abs(normalWS);

                // 依需求可轉到 view space 等其他空間
                // float3 normalVS = TransformWorldToViewDir(normalWS);

                // Debug：把法線壓到 0~1 範圍顯示

                // 可以順便把 depth 也混進去玩，例如做霧、暗角、描邊等
                // 這裡先只示範：在原畫面與法線可視化之間插值
                // float3 result = lerp(color.rgb, debugNormal, _Intensity);

                return float4(absNormalWS, 1.0);
            }
            ENDHLSL
        }
    }
}