// HaloSync Windows — Shaders/ZoneSampler.hlsl
// High-performance GPU compute shader that samples LED color zones from captured desktop textures in VRAM.
// Mirrors the ZoneSampler.metal algorithm 1:1. Runs completely on the GPU with zero CPU memory transfer of the 4K frame.

struct LEDColorOutput {
    float r;
    float g;
    float b;
    float padding;
};

cbuffer ZoneSamplerParams : register(b0) {
    uint LedCount;
    uint SamplingDepth;
    uint SamplesPerZone;
    float BlackBarThreshold;
    float Gamma;
    float SaturationBoost;
    uint TextureWidth;
    uint TextureHeight;
    float3 WallCompensation;
    float _pad;
};

Texture2D<float4> InTexture : register(t0);
SamplerState LinearSampler : register(s0);
StructuredBuffer<float2> LedCoordinates : register(t1);
RWStructuredBuffer<LEDColorOutput> OutBuffer : register(u0);

inline float Linearize(float v, float gamma) {
    return pow(max(v, 0.0f), gamma);
}

inline float Delinearize(float v, float gamma) {
    return pow(max(v, 0.0f), 1.0f / max(gamma, 0.001f));
}

inline float GetLuminance(float3 c) {
    return dot(c, float3(0.2126f, 0.7152f, 0.0722f));
}

[numthreads(64, 1, 1)]
void CSMain(uint3 dispatchThreadId : SV_DispatchThreadID) {
    uint gid = dispatchThreadId.x;
    if (gid >= LedCount) return;

    const float W = float(TextureWidth);
    const float H = float(TextureHeight);
    const float depth = float(max(SamplingDepth, 1u));

    float3 colorAccum = float3(0.0f, 0.0f, 0.0f);
    float weightAccum = 0.0f;

    float2 centerUV = LedCoordinates[gid];
    float2 centerPx = centerUV * float2(W, H);

    float2 boxExtents = float2(depth, depth);
    float2 zoneMin = clamp(centerPx - boxExtents, float2(0.0f, 0.0f), float2(W, H));
    float2 zoneMax = clamp(centerPx + boxExtents, float2(0.0f, 0.0f), float2(W, H));

    const uint sqrtSamples = max((uint)sqrt((float)SamplesPerZone), 1u);
    const float2 zoneSize = max(zoneMax - zoneMin, float2(1.0f, 1.0f));

    for (uint sy = 0; sy < sqrtSamples; ++sy) {
        for (uint sx = 0; sx < sqrtSamples; ++sx) {
            float2 offset = float2(
                ((float)sx + 0.5f) / (float)sqrtSamples,
                ((float)sy + 0.5f) / (float)sqrtSamples
            );
            float2 px = zoneMin + offset * zoneSize;
            float2 uv = px / float2(W, H);

            float4 sample = InTexture.SampleLevel(LinearSampler, uv, 0);

            // Desktop capture formats on Windows can be DXGI_FORMAT_B8G8R8A8_UNORM
            // Shader sampling automatically handles component layout
            float3 linearColor = float3(
                Linearize(sample.r, Gamma),
                Linearize(sample.g, Gamma),
                Linearize(sample.b, Gamma)
            );

            // Wall color compensation (subtractive reflection correction)
            linearColor *= WallCompensation;

            float lum = GetLuminance(linearColor);

            // Smart black bar suppression
            if (lum < BlackBarThreshold) {
                continue;
            }

            // Weight calculation: pixels closer to the screen perimeter count more
            float distFromCenter = length(px - centerPx) / max(depth, 1.0f);
            float weight = saturate(1.0f - distFromCenter * 0.5f);

            colorAccum += linearColor * weight;
            weightAccum += weight;
        }
    }

    float3 finalLinear = (weightAccum > 0.0f) ? (colorAccum / weightAccum) : float3(0.0f, 0.0f, 0.0f);

    // Saturation boost
    if (SaturationBoost > 0.0f) {
        float l = GetLuminance(finalLinear);
        finalLinear = lerp(float3(l, l, l), finalLinear, 1.0f + SaturationBoost * 0.5f);
        finalLinear = max(finalLinear, float3(0.0f, 0.0f, 0.0f));
    }

    // Delinearize back to sRGB space
    LEDColorOutput output;
    output.r = saturate(Delinearize(finalLinear.r, Gamma));
    output.g = saturate(Delinearize(finalLinear.g, Gamma));
    output.b = saturate(Delinearize(finalLinear.b, Gamma));
    output.padding = 0.0f;

    OutBuffer[gid] = output;
}

