struct DynamicLight
{
    float3 position;
    float3 color;
    float  intensity;
    float  radiusSqr;
    uint   channel;

    float3 up;
    float3 forward;
    float  gpFloat1;
    float  gpFloat2;
    float  gpFloat3;
    float  shimmerScale;
    float  shimmerModifier;
};

StructuredBuffer<DynamicLight> dynamic_lights;
uint dynamic_lights_count;

StructuredBuffer<uint> lightmap;
uint lightmap_resolution;

float3 dynamic_ambient_color;

struct DynamicShape
{
    float3 position;
    float3 size;
    uint   flags;
    
    bool isBox()
    {
        return flags & 1;
    }
    
    bool isSphere()
    {
        return flags & 2;
    }
};

StructuredBuffer<DynamicShape> dynamic_shapes;
uint dynamic_shapes_count;

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

// bit 10 determines whether the light has random shimmer.
uint light_is_randomshimmer(DynamicLight light)
{
    return light.channel & 512;
}

// bit 11 determines whether the light is a wave.
uint light_is_wave(DynamicLight light)
{
    return light.channel & 1024;
}

// bit 12 determines whether the light is interference.
uint light_is_interference(DynamicLight light)
{
    return light.channel & 2048;
}

// bit 13 determines whether the light is a rotor.
uint light_is_rotor(DynamicLight light)
{
    return light.channel & 4096;
}

// bit 14 determines whether the light is a shockwave.
uint light_is_shock(DynamicLight light)
{
    return light.channel & 8192;
}

// bit 15 determines whether the light is a disco.
uint light_is_disco(DynamicLight light)
{
    return light.channel & 16384;
}

// macros to name the general purpose variables.
#define light_cutoff light.gpFloat1
#define light_outerCutoff light.gpFloat2
#define light_waveSpeed light.gpFloat1
#define light_waveFrequency light.gpFloat2
#define light_rotorCenter light.gpFloat3
#define light_discoVerticalSpeed light.gpFloat3

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
    float epsilon = light_cutoff - light_outerCutoff;
    float intensity = saturate((theta - light_outerCutoff) / epsilon);
    return float2(theta, intensity);
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

// returns the largest component of a float3.
float max3(float3 v)
{
    return max(max(v.x, v.y), v.z);
}

// https://forum.unity.com/threads/snap-round-3d-direction-vector-based-on-angle.905168/
// snaps the input direction to 45 degree increments.
// shoutouts to JoNax97 and Cannist!
float snap_direction_round(float f)
{
    if (abs(f) < tan(UNITY_PI / 8.0))
        return 0.0;
    return sign(f);
}
float3 snap_direction(float3 input)
{
    // scale vector to unit cube.
    float scaleDivisor = max3(abs(input));
    input /= scaleDivisor;

    float3 rounded = float3(snap_direction_round(input.x), snap_direction_round(input.y), snap_direction_round(input.z));
    return normalize(rounded);
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
    float3x3 rot = look_at_matrix(light.forward, light.up);

    float3 rotated_direction = mul(light_direction, rot);
    float theta = dot(snap_direction(rotated_direction), rotated_direction);
    float epsilon = light_cutoff - light_outerCutoff;
    float intensity = saturate((theta - light_outerCutoff) / epsilon);
    return float2(theta, intensity);
}

// calculates the wave effect.
float light_calculate_wave(DynamicLight light, float3 world)
{
    return 0.7 + 0.3 * sin((distance(light.position, world) - _Time.y * light_waveSpeed) * UNITY_PI * 2 * light_waveFrequency);
}

// calculates the interference effect.
float light_calculate_interference(DynamicLight light, float3 world)
{
    float3x3 rot = look_at_matrix(light.forward, light.up);
    world = mul(world - light.position, rot);

    float angle = atan2(sqrt((world.x * world.x) + (world.z * world.z)), world.y) * UNITY_PI * light_waveFrequency;
    float scale = 0.5 + 0.5 * cos(angle + _Time.y * light_waveSpeed * UNITY_PI * 2.0);
    return scale;
}

// calculates the rotor effect.
float light_calculate_rotor(DynamicLight light, float3 world)
{
    float signRotorCenter = sign(light_rotorCenter);

    float3x3 rot = look_at_matrix(light.forward, light.up);
    world = mul(world - light.position, rot);

    // world.xz are zero at the light position and move outwards. atan2 then calculates the angle
    // from the zero point towards the world coordinate yielding positive pi to zero to negative pi.
    // the angle changes less when it's further away creating a realistic light cone effect.
    float angle = light_waveFrequency * atan2(world.x, world.z);

    // we now calculate the cosine of the angle so that it does one complete oscillation. the angle
    // has been multiplied against the desired wave frequency creating multiple rotor blades as it
    // completes multiple oscillations in one circle. this angle is then offset by the current time
    // to create a rotation.
    float scale = 0.5 + 0.5 * cos(angle + _Time.y * light_waveSpeed * UNITY_PI * 2.0);

    // near world.xz zero the light starts from a sharp center point. that doesn't look very nice so
    // add a blob of light or shadow in the center of the rotor to hide it.
    float dist1 = distance(float2(0, 0), world.xz); // helpme reader: optimize the square roots away.
    float dist2 = sqrt(light.radiusSqr) * abs(light_rotorCenter);
    if (dist1 < dist2)
    {
        // the light blob uses an exponent of 2 and shadows use 4.
        float exponent = max(signRotorCenter * 4, 2);
        scale *= pow(dist1 / dist2, exponent);
    }

    // this if statement does not cause a branch in the shader.
    if (light_rotorCenter < 0)
        return 1.0 - scale;
    return scale;
}

// calculates the shock effect.
float light_calculate_shock(DynamicLight light, float3 world)
{
	float dist = light_waveFrequency * distance(light.position, world);
	float brightness = 0.9 + 0.1 * sin((dist * 2.0 - _Time.y * light_waveSpeed) * UNITY_PI * 2.0);
	brightness      *= 0.9 + 0.1 * cos((dist + _Time.y * light_waveSpeed) * UNITY_PI * 2.0);
	brightness      *= 0.9 + 0.1 * sin((dist / 2.0 - _Time.y * light_waveSpeed) * UNITY_PI * 2.0);
    return brightness;
}

// calculates the disco effect.
float light_calculate_disco(DynamicLight light, float3 world)
{
    float3x3 rot = look_at_matrix(light.forward, light.up);
    world = mul(world - light.position, rot);

	float horizontal = light_waveFrequency * atan2(world.x, world.z);
	float vertical = light_waveFrequency * atan2(sqrt(world.x * world.x + world.z * world.z), world.y);

	float scale1 = 0.5 + 0.5 * cos(horizontal + _Time.y * light_waveSpeed * UNITY_PI * 2.0);
	float scale2 = 0.5 + 0.5 * cos(vertical + _Time.y * light_discoVerticalSpeed * UNITY_PI * 2.0);

	float scale  = scale1 + scale2 - scale1 * scale2;

	float dist = 0.5 * (world.x * world.x + world.z * world.z);
	if (dist < 1.0) scale *= dist;

    return 1.0 - scale;
}

// shoutouts to anastadunbar https://www.shadertoy.com/view/Xt23Ry
float rand(float co) { return frac(sin(co * (91.3458)) * 47453.5453); }
float rand(float2 co) { return frac(sin(dot(co.xy, float2(12.9898, 78.233))) * 43758.5453); }
float rand(float3 co) { return rand(co.xy + rand(co.z)); }

// calculates the water shimmer effect.
//
// returns: the multiplier for the shadow map.
//
float light_calculate_watershimmer(float3 world, float modifier)
{
    // overlay the entire world with random blocks that never change between 0.0 and 1.0.
    float stablerng = rand(world);

    // use a sine wave to change the brightness of the stable random blocks.
    return lerp(modifier, 1.0, -abs(sin(stablerng * _Time.w + _Time.x)));
}

#define GENERATE_FUNCTION_NAME light_calculate_watershimmer_bilinear
#define GENERATE_FUNCTION_CALL light_calculate_watershimmer
#include "GenerateBilinearFilter3D.cginc"

// calculates the random shimmer effect.
//
// returns: the multiplier for the shadow map.
//
float light_calculate_randomshimmer(float3 world, float modifier)
{
    // overlay the entire world with random blocks that change at 30FPS between 0.0 and 1.0.
    float stablerng = rand(world + frac(floor(_Time.y * 30) * 0.001));

    // clamp the range down to change the intensity.
    return modifier + (1.0 - modifier) * stablerng;
}

#define GENERATE_FUNCTION_NAME light_calculate_randomshimmer_bilinear
#define GENERATE_FUNCTION_CALL light_calculate_randomshimmer
#include "GenerateBilinearFilter3D.cginc"

/*
float raycast_box(float3 origin, float3 target, float3 boxcenter, float3 boxsize, int maxsteps)
{
    float3 pos = origin;
    
    for (int i = 0; i < maxsteps; i++)
    {
        if (abs(pos.x - boxcenter.x) <= boxsize.x && abs(pos.y - boxcenter.y) <= boxsize.y && abs(pos.z - boxcenter.z) <= boxsize.z)
        {
            return 1.0;
        }
        
        pos = lerp(origin, target, i / float(maxsteps));
    }
    
    return 0.0;
}*/

float raycast_box(float3 origin, float3 target, float3 boxcenter, float3 boxsize)
{
    #define EPSILON 0.00001;
    
    float3 p0 = origin;
    float3 p1 = target;
    
    float3 b_min = boxcenter - boxsize;
    float3 b_max = boxcenter + boxsize;
    
    float3 c = (b_min + b_max) * 0.5f; // Box center-point
    float3 e = b_max - c; // Box halflength extents
    float3 m = (p0 + p1) * 0.5f; // Segment midpoint
    float3 d = p1 - m; // Segment halflength vector
    m = m - c; // Translate box and segment to origin
    // Try world coordinate axes as separating axes
    float adx = abs(d.x);
    if (abs(m.x) > e.x + adx) return 0;
    float ady = abs(d.y);
    if (abs(m.y) > e.y + ady) return 0;
    float adz = abs(d.z);
    if (abs(m.z) > e.z + adz) return 0;
    // Add in an epsilon term to counteract arithmetic errors when segment is
    // (near) parallel to a coordinate axis (see text for detail)
    adx += EPSILON; ady += EPSILON; adz += EPSILON;
    // Try cross products of segment direction vector with coordinate axes
    if (abs(m.y * d.z - m.z * d.y) > e.y * adz + e.z * ady) return 0;
    if (abs(m.z * d.x - m.x * d.z) > e.x * adz + e.z * adx) return 0;
    if (abs(m.x * d.y - m.y * d.x) > e.x * ady + e.y * adx) return 0;
    // No separating axis found; segment must be overlapping AABB
    return 1;
    
    #undef EPSILON
}

float raycast_sphere(float3 origin, float3 target, float3 spherecenter, float radius)
{
    /*float3 d = normalize(target - origin);
    float3 m = origin - spherecenter;
    float c = dot(m, m) - radius * radius;
    // if there is definitely at least one real root, there must be an intersection.
    if (c <= 0.0) return 1.0;
    float b = dot(m, d);
    // early exit if ray origin outside sphere and ray pointing away from sphere.
    if (b > 0.0) return 0.0;
    float disc = b*b - c;
    // a negative discriminant corresponds to ray missing sphere.
    if (disc < 0.0) return 0.0;
    // now ray must hit sphere.
    return 1.0;*/
    
    float3 p = origin;
    float3 d = normalize(target - origin);
    
    float3 m = p - spherecenter; 
    float b = dot(m, d); 
    float c = dot(m, m) - radius * radius; 

    // exit if r’s origin outside s (c > 0) and r pointing away from s (b > 0).
    if (c > 0.0 && b > 0.0) return 0.0;
    float discr = b*b - c;

    // a negative discriminant corresponds to ray missing sphere.
    if (discr < 0.0) return 0.0; 

    // ray now found to intersect sphere, compute smallest t value of intersection.
    float t = -b - sqrt(discr); 

    // if t is negative, ray started inside sphere so clamp t to zero.
    if (t < 0.0) t = 0.0;
    float q = p + t * d;
    
    // ensure that t does not exceed the ray origin and target.
    return t <= length(abs(target - origin));
}
// special thanks to https://learnopengl.com/PBR/Lighting

// normal distribution function: approximates the amount the surface's
// microfacets are aligned to the halfway vector, influenced by the roughness of
// the surface; this is the primary function approximating the microfacets.
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

// geometry function: describes the self-shadowing property of the microfacets.
// when a surface is relatively rough, the surface's microfacets can overshadow
// other microfacets reducing the light the surface reflects.
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

// fresnel equation: the fresnel equation describes the ratio of surface
// reflection at different surface angles.
float3 fresnelSchlick(float cosTheta, float3 F0)
{
    return F0 + (1.0 - F0) * pow(clamp(1.0 - cosTheta, 0.0, 1.0), 5.0);
}

float3 fresnelSchlickRoughness(float cosTheta, float3 F0, float roughness)
{
    return F0 + (max(1.0 - roughness, F0) - F0) * pow(clamp(1.0 - cosTheta, 0.0, 1.0), 5.0);
}