struct DynamicLight
{
    float3 position;
    float3 color;
    float  intensity;
    float  radius;
    uint   channel;

    float3 forward;
    float  cutoff;
    float  outerCutoff;
};

StructuredBuffer<DynamicLight> dynamic_lights;
uint dynamic_lights_count;

StructuredBuffer<uint> lightmap;
uint lightmap_resolution;

// fetches a shadow bit as the specified uv coordinates from the lightmap data.
float lightmap_pixel(uint2 uv, uint channel)
{
    return (lightmap[uv.y * lightmap_resolution + uv.x] & (1 << channel)) > 0;
}

// x x x
// x   x apply a simple 3x3 sampling with averaged results to the shadow bits.
// x x x
float lightmap_sample3x3(uint2 uv, uint channel)
{
    float map;

    map  = lightmap_pixel(uv, channel);
    map += lightmap_pixel(uv + uint2(-1, -1), channel);
    map += lightmap_pixel(uv + uint2( 0, -1), channel);
    map += lightmap_pixel(uv + uint2( 1, -1), channel);

    map += lightmap_pixel(uv + uint2(-1,  0), channel);
    map += lightmap_pixel(uv + uint2( 1,  0), channel);

    map += lightmap_pixel(uv + uint2(-1,  1), channel);
    map += lightmap_pixel(uv + uint2( 0,  1), channel);
    map += lightmap_pixel(uv + uint2 (1,  1), channel);

    return map / 9.0;
}

// x x x
// x   x apply a simple 3x3 sampling with averaged results to the shadow bits.
// x x x
// 
// this function skips reading the center pixel, pass that into the map parameter.
float lightmap_sample3x3(uint2 uv, uint channel, float map)
{
    map += lightmap_pixel(uv + uint2(-1, -1), channel);
    map += lightmap_pixel(uv + uint2(0, -1), channel);
    map += lightmap_pixel(uv + uint2(1, -1), channel);

    map += lightmap_pixel(uv + uint2(-1, 0), channel);
    map += lightmap_pixel(uv + uint2(1, 0), channel);

    map += lightmap_pixel(uv + uint2(-1, 1), channel);
    map += lightmap_pixel(uv + uint2(0, 1), channel);
    map += lightmap_pixel(uv + uint2 (1, 1), channel);

    return map / 9.0;
}

// the first 5 bits contain a valid channel index so mask by 31.
uint light_get_shadow_channel(DynamicLight light)
{
    return light.channel & 31;
}

// bit 6 determines whether the light is realtime and does not have shadows.
uint light_is_realtime(DynamicLight light)
{
    return light.channel & 32;
}

// bit 6 determines whether the light is realtime and does not have shadows.
uint light_is_dynamic(DynamicLight light)
{
    return (light.channel & 32) == 0;
}

// bit 7 determines whether the light is a spotlight.
uint light_is_spotlight(DynamicLight light)
{
    return light.channel & 64;
}

// special thanks to https://learnopengl.com/PBR/Lighting

float DistributionGGX(float3 N, float3 H, float3 roughness)
{
    float a = roughness * roughness;
    float a2 = a * a;
    float NdotH = max(dot(N, H), 0.0);
    float NdotH2 = NdotH * NdotH;

    float num = a2;
    float denom = (NdotH2 * (a2 - 1.0) + 1.0);
    denom = 3.14159265359 * denom * denom;

    return num / denom;
}

float GeometrySchlickGGX(float NdotV, float roughness)
{
    float r = (roughness + 1.0);
    float k = (r * r) / 8.0;

    float num = NdotV;
    float denom = NdotV * (1.0 - k) + k;

    return num / denom;
}

float GeometrySmith(float3 N, float3 V, float3 L, float roughness)
{
    float NdotV = max(dot(N, V), 0.0);
    float NdotL = max(dot(N, L), 0.0);
    float ggx2 = GeometrySchlickGGX(NdotV, roughness);
    float ggx1 = GeometrySchlickGGX(NdotL, roughness);

    return ggx1 * ggx2;
}

float3 fresnelSchlick(float cosTheta, float3 F0)
{
    return F0 + (1.0 - F0) * pow(clamp(1.0 - cosTheta, 0.0, 1.0), 5.0);
}