#ifndef URP_COLOR_HSV_INCLUDED
#define URP_COLOR_HSV_INCLUDED

// ------------------------------------------------------------
// RGB (Linear, 可 HDR) → HSV
// H: [0,1)   S: [0,1]   V: [0,∞)
// 演算法可在 HDR 下保持亮度比例
// ------------------------------------------------------------
float3 RGBToHSV(float3 rgb)
{
    float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    float4 p = rgb.g < rgb.b ? float4(rgb.bg, K.wz) : float4(rgb.gb, K.xy);
    float4 q = rgb.r < p.x ? float4(p.xyw, rgb.r) : float4(rgb.r, p.yzx);

    float d = q.x - min(q.w, q.y);
    float e = 1e-10;

    float3 hsv;
    hsv.x = abs(q.z + (q.w - q.y) / (6.0 * d + e)); // Hue
    hsv.y = d / (q.x + e);                           // Saturation
    hsv.z = q.x;                                     // Value (可 > 1.0，支援 HDR)

    return hsv;
}

// ------------------------------------------------------------
// HSV → RGB (Linear, 可 HDR)
// 期待輸入：H ∈ [0,1)、S ∈ [0,1]、V ∈ [0,∞)
// ------------------------------------------------------------
float3 HSVToRGB(float3 hsv)
{
    float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    float3 p = abs(frac(hsv.xxx + K.xyz) * 6.0 - K.www);
    float3 rgb = hsv.z * lerp(K.xxx, saturate(p - K.xxx), hsv.y);

    return rgb;
}

// ------------------------------------------------------------
// 使用 HSV 進行顏色調整（保持 HDR 亮度）
// hueOffset      : 以 1.0 為一整圈（+0.1 = 轉 36°）
// saturationMul  : 飽和度倍率（典型 0.0~2.0）
// valueMul       : 亮度倍率（0.0~∞，>1.0 會更亮，保留 HDR）
// ------------------------------------------------------------
float3 AdjustRGBWithHSV(
    float3 rgb,
    float hueOffset,
    float saturationMul,
    float valueMul
)
{
    float3 hsv = RGBToHSV(rgb);

    // Hue shift，維持在 [0,1)
    hsv.x = frac(hsv.x + hueOffset);

    // 飽和度乘法，並限制在 [0,1]
    hsv.y = saturate(hsv.y * saturationMul);

    // Value 乘法，不做 saturate，保留 HDR 亮度
    hsv.z = hsv.z * valueMul;

    return HSVToRGB(hsv);
}

// ------------------------------------------------------------
// 針對帶 alpha 的顏色進行調整：
// - 對 rgb 做 HSV 操作
// - alpha 原樣保留
// ------------------------------------------------------------
float4 AdjustRGBAWithHSV(
    float4 rgba,
    float hueOffset,
    float saturationMul,
    float valueMul
)
{
    float3 adjustedRGB = AdjustRGBWithHSV(rgba.rgb, hueOffset, saturationMul, valueMul);
    return float4(adjustedRGB, rgba.a);
}

// Mapping to Unity ShaderGraph Custom Function

void AdjustRGBWithHSV_float(
    float3 rgb,
    float hueOffset,
    float saturationMul,
    float valueMul,
    out float3 result
)
{
    result = AdjustRGBWithHSV(rgb, hueOffset, saturationMul, valueMul);
}

#endif // URP_COLOR_HSV_INCLUDED