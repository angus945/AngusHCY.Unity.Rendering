#ifndef ANGUS_HCY_EDGE_LINE_INCLUDED
#define ANGUS_HCY_EDGE_LINE_INCLUDED

// 這裡假設外部已經 include：
// Core.hlsl
// DeclareDepthTexture.hlsl
// DeclareNormalsTexture.hlsl

// ------------------------------------------------------------
// Depth 相關
// ------------------------------------------------------------

inline float Angus_SampleDepth01(float2 uv)
{
    float rawDepth = SampleSceneDepth(uv);
    float depth01 = Linear01Depth(rawDepth, _ZBufferParams);
    return depth01;
}

inline float Angus_SampleMinDepthKernel(float2 uv, int radius)
{
    float minDepth = 1.0;
    float2 texelSize = 1.0 / _ScreenParams.xy;

    [loop]
    for (int x = -radius; x <= radius; x++)
    {
        [loop]
        for (int y = -radius; y <= radius; y++)
        {
            float2 offset = float2(x, y) * texelSize;
            float sampleDepthValue = Angus_SampleDepth01(uv + offset);
            minDepth = min(minDepth, sampleDepthValue);
        }
    }

    return minDepth;
}

inline float Angus_SampleDepthDifference(float2 uv, int radius)
{
    float centerDepth = Angus_SampleDepth01(uv);
    float minDepth = Angus_SampleMinDepthKernel(uv, radius);
    return centerDepth - minDepth;
}

// 你原本的 ddx/ddy 版本（可選）
inline float Angus_SampleDepthDDXDDY(float2 uv)
{
    float depth01 = Angus_SampleDepth01(uv);
    float depthDDX = ddx(depth01);
    float depthDDY = ddy(depth01);
    return sqrt(depthDDX * depthDDX + depthDDY * depthDDY);
}

// ------------------------------------------------------------
// Normal 相關
// ------------------------------------------------------------

inline float3 Angus_SampleNormalWS(float2 uv)
{
    float3 normalWS = SampleSceneNormals(uv);
    return normalWS;
}

inline float3 Angus_SampleMinNormalKernel(float2 uv, int radius)
{
    float3 minNormal = float3(1.0, 1.0, 1.0);
    float2 texelSize = 1.0 / _ScreenParams.xy;

    [loop]
    for (int x = -radius; x <= radius; x++)
    {
        [loop]
        for (int y = -radius; y <= radius; y++)
        {
            float2 offset = float2(x, y) * texelSize;
            float3 sampleNormalValue = Angus_SampleNormalWS(uv + offset);
            minNormal = min(minNormal, sampleNormalValue);
        }
    }

    return minNormal;
}

inline float Angus_SampleNormalDifference(float2 uv, int radius)
{
    float3 centerNormal = Angus_SampleNormalWS(uv);
    float3 minNormal = Angus_SampleMinNormalKernel(uv, radius);
    return length(centerNormal - minNormal);
}

// ------------------------------------------------------------
// Edge Detection 主函式（Shader Graph / Shader 共用）
//
// uv           : 螢幕 UV（通常 0~1）
// lineRadius   : 探測半徑（可直接對應你的 _LineThickness）
//
// return       : 邊緣強度 [0,1]
// ------------------------------------------------------------
inline float Angus_EdgeDetection(float2 uv, int lineRadius)
{
    float depthDiff = Angus_SampleDepthDifference(uv, lineRadius);
    float depthValue = pow(depthDiff * 50.0, 5.0);

    float normalDiff = Angus_SampleNormalDifference(uv, lineRadius);

    float edgeValue = max(depthValue, normalDiff);
    edgeValue = saturate(edgeValue);

    return edgeValue;
}

void EdgeDetection_float(float2 uv, int lineRadius, out float edgeValue)
{
    edgeValue = Angus_EdgeDetection(uv, lineRadius);
}

#endif // ANGUS_HCY_EDGE_LINE_INCLUDED