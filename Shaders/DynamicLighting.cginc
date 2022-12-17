struct DynamicLight
{
    float3 position;
    float3 color;
    float  intensity;
    float  radiusSqr;
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
float lightmap_sample(uint2 uv, uint channel)
{
    return (lightmap[uv.y * lightmap_resolution + uv.x] & (1 << channel)) > 0;
}

// x x x
// x   x apply a simple 3x3 sampling with averaged results to the shadow bits.
// x x x
float lightmap_sample3x3(uint2 uv, uint channel)
{
    float map;

    map  = lightmap_sample(uv, channel);
    map += lightmap_sample(uv + uint2(-1, -1), channel);
    map += lightmap_sample(uv + uint2( 0, -1), channel);
    map += lightmap_sample(uv + uint2( 1, -1), channel);

    map += lightmap_sample(uv + uint2(-1,  0), channel);
    map += lightmap_sample(uv + uint2( 1,  0), channel);

    map += lightmap_sample(uv + uint2(-1,  1), channel);
    map += lightmap_sample(uv + uint2( 0,  1), channel);
    map += lightmap_sample(uv + uint2( 1,  1), channel);

    return map / 9.0;
}

// x x x
// x   x apply 4x 3x3 sampling with interpolation to get bilinear filtered shadow bits.
// x x x
float lightmap_sample_bilinear(float2 uv, uint channel)
{
    // huge shoutout to neu_graphic for their software bilinear filter shader.
    // https://www.shadertoy.com/view/4sBSRK

    float2 pos = uv - 0.5;
    float2 f = frac(pos);
    uint2 pos_top_left = floor(pos);

    // we are sample center, so it's the same as point sample.
    float tl = lightmap_sample3x3(pos_top_left, channel);
    float tr = lightmap_sample3x3(pos_top_left + uint2(1, 0), channel);
    float bl = lightmap_sample3x3(pos_top_left + uint2(0, 1), channel);
    float br = lightmap_sample3x3(pos_top_left + uint2(1, 1), channel);

    return lerp(lerp(tl, tr, f.x), lerp(bl, br, f.x), f.y);
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

// bit 8 determines whether the light is a discoball.
uint light_is_discoball(DynamicLight light)
{
    return light.channel & 128;
}

// bit 9 determines whether the light has water shimmer.
uint light_is_watershimmer(DynamicLight light)
{
    return light.channel & 256;
}

// calculates the spotlight effect.
//
// returns:
// x: the cutoff angle theta.
// y: the intensity for a smooth transition.
// 
// example:
// 
//    // anything outside of the spot light can and must be skipped.
//    float2 spotlight = light_calculate_spotlight(light, light_direction);
//    if (spotlight.x <= light.outerCutoff)
//        continue;
//    map *= spotlight.y;
//
float2 light_calculate_spotlight(DynamicLight light, float3 light_direction)
{
    float theta = dot(light_direction, -light.forward);
    float epsilon = light.cutoff - light.outerCutoff;
    float intensity = saturate((theta - light.outerCutoff) / epsilon);
    return float2(theta, intensity);
}

// Rotation with angle (in radians) and axis
float3x3 AngleAxis3x3(float angle, float3 axis)
{
    float c, s;
    sincos(angle, s, c);

    float t = 1 - c;
    float x = axis.x;
    float y = axis.y;
    float z = axis.z;

    return float3x3(
        t * x * x + c, t * x * y - s * z, t * x * z + s * y,
        t * x * y + s * z, t * y * y + c, t * y * z - s * x,
        t * x * z - s * y, t * y * z + s * x, t * z * z + c
        );
}

float4x4 axis_matrix(float3 right, float3 up, float3 forward)
{
    float3 xaxis = right;
    float3 yaxis = up;
    float3 zaxis = forward;
    return float4x4(
        xaxis.x, yaxis.x, zaxis.x, 0,
        xaxis.y, yaxis.y, zaxis.y, 0,
        xaxis.z, yaxis.z, zaxis.z, 0,
        0, 0, 0, 1
        );
}

float4x4 look_at_matrix(float3 forward, float3 up)
{
    float3 xaxis = normalize(cross(forward, up));
    float3 yaxis = up;
    float3 zaxis = forward;
    return axis_matrix(xaxis, yaxis, zaxis);
}

float4x4 look_at_matrix(float3 at, float3 eye, float3 up)
{
    float3 zaxis = normalize(at - eye);
    float3 xaxis = normalize(cross(up, zaxis));
    float3 yaxis = cross(zaxis, xaxis);
    return axis_matrix(xaxis, yaxis, zaxis);
}

// snaps the input direction to 45 degree increments.
float3 snap_direction(float3 input)
{
    float3 angle = acos(input);
    float3 rounded = round(angle / radians(45.0)) * radians(45.0);
    float3 cosine = cos(rounded);
    return normalize(cosine);
}

// calculates the discoball spotlights effect.
//
// returns:
// x: the cutoff angle theta.
// y: the intensity for a smooth transition.
// 
// example:
// 
//    // anything outside of the spot lights can and must be skipped.
//    float2 spotlight = light_calculate_spotlight(light, light_direction);
//    if (spotlight.x <= light.outerCutoff)
//        continue;
//    map *= spotlight.y;
//
float2 light_calculate_discoball(DynamicLight light, float3 light_direction)
{
    // FIXME: this is not accurate and flies all over the place!
    float3x3 rot = look_at_matrix(light.position - light.forward, light.position, float3(0, 1, 0));

    float theta = dot(snap_direction(mul(light_direction, rot)), mul(light_direction, rot));
    float epsilon = light.cutoff - light.outerCutoff;
    float intensity = saturate((theta - light.outerCutoff) / epsilon);
    return float2(theta, intensity);
}

// Gold Noise © 2015 dcerisano@standard3d.com
// - based on the Golden Ratio
// - uniform normalized distribution
// - fastest static noise generator function (also runs at low precision)
// - use with indicated fractional seeding method

const float PHI = 1.61803398874989484820459; // = Golden Ratio 

float gold_noise(in float2 xy, in float seed)
{
    return frac(tan(distance(xy * PHI, xy) * seed) * xy.x);
}

// calculates the water shimmer effect.
//
// returns: the multiplier for the shadow map.
//
float light_calculate_watershimmer(DynamicLight light, float3 world)
{
    // overlay the entire world with random blocks that never change between 0.0 and 1.0.
    float pixel_scale = 12.5;
    world = round(world * pixel_scale) / pixel_scale;

    // the random function cannot work when there is a zero component.
    if (world.x == 0.0) world.x = 1.0;
    if (world.y == 0.0) world.y = 1.0;
    if (world.z == 0.0) world.z = 1.0;

    float stablerng = gold_noise(world.xy, world.z);

    // use a sine wave to change the brightness of the stable random blocks.
    return lerp(0.8, 1, -abs(sin(stablerng * _Time.w)));
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
    denom = UNITY_PI * denom * denom;

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