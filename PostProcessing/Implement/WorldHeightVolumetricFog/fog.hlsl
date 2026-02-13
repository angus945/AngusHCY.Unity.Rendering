
float GetRayInUndenseFog(float3 origin, float3 rayDir, float rayLength, float fogBottom, float fogTop, out float3 startPoint, out float3 endPoint)
{
    startPoint = float3(0, 0, 0);
    endPoint = float3(0, 0, 0);

    float epsilon = 1e-5f;

    // 射線與 y 平面幾乎平行
    if (abs(rayDir.y) < epsilon)
    {
        bool inside = origin.y > fogBottom && origin.y < fogTop;
        if (inside)
        {
            startPoint = origin;
            endPoint = origin + rayDir * rayLength;
            return rayLength;
        }
        return 0;
    }

    // 計算與 bottom 與 top 平面的 t
    float tBottom = (fogBottom - origin.y) / rayDir.y;
    float tTop = (fogTop - origin.y) / rayDir.y;

    // 將 t 由小排到大（射線沿自身參數方向的區段）
    float tMin = min(tBottom, tTop);
    float tMax = max(tBottom, tTop);

    // 射線有效區間是 [0, rayLength]
    float segStart = max(0, tMin);
    float segEnd = min(rayLength, tMax);

    // segStart < segEnd 才表示有有效區段
    if (segEnd <= segStart)
    {
        return 0;
    }

    startPoint = origin + segStart * rayDir;
    endPoint = origin + segEnd * rayDir;

    return segEnd - segStart;
}

float GetRayInDenseFog(float3 origin, float3 rayDir, float rayLength, float fogBottom)
{
    float epsilon = 1e-5f;
    bool startsInFog = origin.y <= fogBottom;

    // 射線幾乎平行於平面
    if (abs(rayDir.y) < epsilon)
    {
        if (startsInFog)
        {
            // 起點就在霧中，而且不會離開平面 => 全程在霧裡
            return rayLength;
        }

        // 起點不在霧中，而且不會碰到平面 => 完全沒在霧裡
        return 0;
    }

    // t：射線與 y = fogBottom 的交點參數
    float t = (fogBottom - origin.y) / rayDir.y;

    // 沒有在 [0, rayLength] 之內穿過平面
    if (t <= 0 || t >= rayLength)
    {
        if (startsInFog)
        {
            // 一開始在霧裡，但這一段長度內不會穿出霧 => 全程在霧裡
            return rayLength;
        }

        // 一開始不在霧裡，也不會進入霧 => 沒有濃霧路徑
        return 0;
    }

    // 有在 [0, rayLength] 之內穿過平面一次：
    if (startsInFog)
    {
        // 從霧裡往上走，到平面就離開濃霧
        return t;
    }
    else
    {
        // 從上方往下走，碰到平面才進入濃霧
        return rayLength - t;
    }

    return 0;
}

float HeightWeightNonLinear(float y, float fogBottom, float fogTop, float heightExponent)
{
    float denom = fogTop - fogBottom;
    denom = (abs(denom) < 1e-4) ? 1e-4 : denom;

    // y 在 fogBottom ~ fogTop 之間：越高越淡
    float h = (fogTop - y) / denom;
    h = saturate(h);

    // 用 pow 做非線性：exponent > 1 -> 貼地濃，向上迅速變淡
    //                    exponent < 1 -> 整體偏均勻
    float result = pow(h, heightExponent);
    return result;
}

float FogValueInUndense(
    float3 cameraPosWS,
    float3 pixelPosWS,
    float3 rayDirWS,
    float distance,
    float fogBottom,
    float fogTop,
    float density,
    float heightExponent)
{
    float3 startPoint, endPoint;
    float distanceInUndenseFog = GetRayInUndenseFog(cameraPosWS, rayDirWS, distance, fogBottom, fogTop, startPoint, endPoint);
    float h0 = HeightWeightNonLinear(startPoint.y, fogBottom, fogTop, heightExponent);
    float h1 = HeightWeightNonLinear(endPoint.y, fogBottom, fogTop, heightExponent);
    float hAvg = 0.5 * (h0 + h1);
    float sigma = density * hAvg * distanceInUndenseFog;
    sigma = max(sigma, 0.0);
    return sigma;
}
float FogValueInDense(
    float3 cameraPosWS,
    float3 pixelPosWS,
    float3 rayDirWS,
    float distance,
    float fogBottom,
    float fogTop,
    float density,
    float heightExponent)
{
    float distanceInDenseFog = GetRayInDenseFog(cameraPosWS, rayDirWS, distance, fogBottom);
    float h0 = HeightWeightNonLinear(cameraPosWS.y, fogBottom, fogTop, heightExponent);
    float h1 = HeightWeightNonLinear(pixelPosWS.y, fogBottom, fogTop, heightExponent);
    float hAvg = 0.5 * (h0 + h1);
    float sigma = density * hAvg * distanceInDenseFog;
    sigma = max(sigma, 0.0);
    return sigma;
}

void SimpleHeightFogNonLinear_float
(
    float3 cameraPosWS,
    float3 pixelPosWS,
    float fogBottom,
    float fogTop,
    float density,
    float heightExponent,
    out float fogFactor
)
{
    float3 rayDirWS = normalize(pixelPosWS - cameraPosWS);
    float distance = length(pixelPosWS - cameraPosWS);
    float fogInUndense = FogValueInUndense(cameraPosWS, pixelPosWS, rayDirWS, distance, fogBottom, fogTop, density, heightExponent);
    float fogInDense = FogValueInDense(cameraPosWS, pixelPosWS, rayDirWS, distance, fogBottom, fogTop, density, heightExponent);
    float sigma = fogInUndense + fogInDense;
    fogFactor = 1.0 - exp(-sigma);
}