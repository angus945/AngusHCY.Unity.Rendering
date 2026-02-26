#ifndef ANGUS_HCY_NOISE_INCLUDED
#define ANGUS_HCY_NOISE_INCLUDED

// 3D hash → [0,1)
inline float Angus_Hash31(float3 p)
{
    p = frac(p * 0.1031);
    p += dot(p, p.yzx + 33.33);
    return frac((p.x + p.y) * p.z);
}

// Smoothstep-like 補間
inline float Angus_Smooth01(float t)
{
    return t * t * (3.0 - 2.0 * t);
}

// 3D value noise（輸出 [0,1]）
inline float Angus_Noise3D(float3 p)
{
    float3 i = floor(p);
    float3 f = frac(p);

    f = float3(
        Angus_Smooth01(f.x),
        Angus_Smooth01(f.y),
        Angus_Smooth01(f.z)
    );

    float n000 = Angus_Hash31(i + float3(0, 0, 0));
    float n100 = Angus_Hash31(i + float3(1, 0, 0));
    float n010 = Angus_Hash31(i + float3(0, 1, 0));
    float n110 = Angus_Hash31(i + float3(1, 1, 0));
    float n001 = Angus_Hash31(i + float3(0, 0, 1));
    float n101 = Angus_Hash31(i + float3(1, 0, 1));
    float n011 = Angus_Hash31(i + float3(0, 1, 1));
    float n111 = Angus_Hash31(i + float3(1, 1, 1));

    float n00 = lerp(n000, n100, f.x);
    float n10 = lerp(n010, n110, f.x);
    float n01 = lerp(n001, n101, f.x);
    float n11 = lerp(n011, n111, f.x);

    float n0 = lerp(n00, n10, f.y);
    float n1 = lerp(n01, n11, f.y);

    return lerp(n0, n1, f.z);
}

// ------------------------------------------------------------
// Utility：用世界座標 + time 做邊緣 jitter，用來偏移 UV
// 這個版本特別為 Shader Graph / Custom Function 設計
//
// uv              : 原始螢幕 UV (0~1)
// worldPos        : 該像素的世界座標
// noiseScale      : 控制 noise 空間頻率
// noiseIntensity  : 原本的 _NoiseIntensity（會內部乘 0.001）
// animationSpeed  : _AnimationSpeed（控制跳動速度）
// timeValue       : 你可以直接傳 _Time.y（SG 用 Time node 的 y 分量）
//
// return : 偏移後 UV
// ------------------------------------------------------------
inline float2 Angus_ApplyWorldPosNoise(
    float2 uv,
    float3 worldPos,
    float noiseScale,
    float noiseIntensity,
    float animationSpeed,
    float timeValue)
{
    float noiseTime = floor(timeValue * animationSpeed);
    float noise = Angus_Noise3D(worldPos * noiseScale + noiseTime);

    // [0,1] → [-1,1]
    noise = noise * 2.0 - 1.0;

    // 與原 shader 相同：乘 0.001
    noise *= noiseIntensity * 0.001f;

    return uv + noise;
}

void WorldPosUVNoise_float(
    float2 uv,
    float3 worldPos,
    float noiseScale,
    float noiseIntensity,
    float animationSpeed,
    float timeValue,
    out float2 noisyUV)
{
    noisyUV = Angus_ApplyWorldPosNoise(uv, worldPos, noiseScale, noiseIntensity, animationSpeed, timeValue);
}

#endif // ANGUS_HCY_NOISE_INCLUDED