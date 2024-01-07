#include "Common.cginc"

// macros to name the general purpose variables.
#define light_cutoff gpFloat1
#define light_outerCutoff gpFloat2
#define light_waveSpeed gpFloat1
#define light_waveFrequency gpFloat2
#define light_rotorCenter gpFloat3
#define light_discoVerticalSpeed gpFloat3

struct DynamicLight
{
    float3 position;
    float  radiusSqr;
    // -- 16 byte boundary --
    uint   channel;
    float  intensity;
    float  gpFloat1;
    float  gpFloat2;
    // -- 16 byte boundary --
    float3 color;
    float  gpFloat3;
    // -- 16 byte boundary --
    float3 up;
    float  shimmerScale;
    // -- 16 byte boundary --
    float3 forward;
    float  shimmerModifier;
    // -- 16 byte boundary --
    float  volumetricRadiusSqr;
    float  volumetricIntensity;
    float  volumetricThickness;
    float  volumetricVisibility;
    // -- 16 byte boundary --
    
    // the first 5 bits contain a valid channel index so mask by 31.
    uint get_shadow_channel()
    {
        return channel & 31;
    }

    // bit 6 determines whether the light is realtime and does not have shadows.
    uint is_realtime()
    {
        return channel & 32;
    }

    // bit 6 determines whether the light is realtime and does not have shadows.
    uint is_dynamic()
    {
        return (channel & 32) == 0;
    }

    // bit 7 determines whether the light is a spotlight.
    uint is_spotlight()
    {
        return channel & 64;
    }

    // bit 8 determines whether the light is a discoball.
    uint is_discoball()
    {
        return channel & 128;
    }

    // bit 9 determines whether the light has water shimmer.
    uint is_watershimmer()
    {
        return channel & 256;
    }

    // bit 10 determines whether the light has random shimmer.
    uint is_randomshimmer()
    {
        return channel & 512;
    }

    // bit 11 determines whether the light is a wave.
    uint is_wave()
    {
        return channel & 1024;
    }

    // bit 12 determines whether the light is interference.
    uint is_interference()
    {
        return channel & 2048;
    }

    // bit 13 determines whether the light is a rotor.
    uint is_rotor()
    {
        return channel & 4096;
    }

    // bit 14 determines whether the light is a shockwave.
    uint is_shock()
    {
        return channel & 8192;
    }

    // bit 15 determines whether the light is a disco.
    uint is_disco()
    {
        return channel & 16384;
    }
    
    // bit 16 determines whether the light is volumetric.
    uint is_volumetric()
    {
        return channel & 32768;
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
    float2 calculate_spotlight(float3 light_direction)
    {
        float theta = dot(light_direction, -forward);
        float epsilon = light_cutoff - light_outerCutoff;
        float intensity = saturate((theta - light_outerCutoff) / epsilon);
        return float2(theta, intensity);
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
    float2 calculate_discoball(float3 light_direction)
    {
        float3x3 rot = look_at_matrix(forward, up);

        float3 rotated_direction = mul(light_direction, rot);
        float theta = dot(snap_direction(rotated_direction), rotated_direction);
        float epsilon = light_cutoff - light_outerCutoff;
        float intensity = saturate((theta - light_outerCutoff) / epsilon);
        return float2(theta, intensity);
    }
    
    // calculates the wave effect.
    float calculate_wave(float3 world)
    {
        return 0.7 + 0.3 * sin((distance(position, world) - _Time.y * light_waveSpeed) * UNITY_PI * 2 * light_waveFrequency);
    }
    
    // calculates the interference effect.
    float calculate_interference(float3 light_position_minus_world)
    {
        float3x3 rot = look_at_matrix(forward, up);
        float3 world = mul(light_position_minus_world, rot);

        float angle = atan2(sqrt((world.x * world.x) + (world.z * world.z)), world.y) * UNITY_PI * light_waveFrequency;
        float scale = 0.5 + 0.5 * cos(angle - _Time.y * light_waveSpeed * UNITY_PI * 2.0);
        return scale;
    }
    
    // calculates the rotor effect.
    float calculate_rotor(float3 light_position_minus_world)
    {
        float3x3 rot = look_at_matrix(forward, up);
        float3 world = mul(light_position_minus_world, rot);

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
        float absRotorCenter = radiusSqr * abs(light_rotorCenter);
        float distSquared = dot(world.xz, world.xz);
        if (light_rotorCenter < 0.0)
        {
            if (distSquared < absRotorCenter)
                scale *= pow(distSquared / absRotorCenter, UNITY_PI);
        }
        else
        {
            distSquared *= 1.0 / absRotorCenter;
            if (distSquared < 1.0)
                scale = 1.0 - distSquared + scale * distSquared;
        }
        
        // clueless why but this makes light and shadow blades equal size.
        return pow(scale, UNITY_HALF_PI);
    }
    
    // calculates the shock effect.
    float calculate_shock(float3 world)
    {
	    float dist = light_waveFrequency * distance(position, world);
	    float brightness = 0.9 + 0.1 * sin((dist * 2.0 - _Time.y * light_waveSpeed) * UNITY_PI * 2.0);
	    brightness      *= 0.9 + 0.1 * cos((dist + _Time.y * light_waveSpeed) * UNITY_PI * 2.0);
	    brightness      *= 0.9 + 0.1 * sin((dist / 2.0 - _Time.y * light_waveSpeed) * UNITY_PI * 2.0);
        return brightness;
    }
    
    // calculates the disco effect.
    float calculate_disco(float3 light_position_minus_world)
    {
        float3x3 rot = look_at_matrix(forward, up);
        float3 world = mul(light_position_minus_world, rot);

	    float horizontal = light_waveFrequency * atan2(world.x, world.z);
	    float vertical = light_waveFrequency * atan2(sqrt(world.x * world.x + world.z * world.z), world.y);

	    float scale1 = 0.5 + 0.5 * cos(horizontal + _Time.y * light_waveSpeed * UNITY_PI * 2.0);
	    float scale2 = 0.5 + 0.5 * cos(vertical - _Time.y * light_discoVerticalSpeed * UNITY_PI * 2.0);

	    float scale  = scale1 + scale2 - scale1 * scale2;

	    float dist = 0.5 * (world.x * world.x + world.z * world.z);
	    if (dist < 1.0) scale *= dist;

        return 1.0 - scale;
    }
    
    // calculates the water shimmer effect.
    //
    // returns: the multiplier for the shadow map.
    //
    float calculate_watershimmer(float3 world, float modifier)
    {
        // overlay the entire world with random blocks that never change between 0.0 and 1.0.
        float stablerng = rand(world);

        // use a sine wave to change the brightness of the stable random blocks.
        return lerp(modifier, 1.0, -abs(sin(stablerng * _Time.w + _Time.x)));
    }

    #define GENERATE_FUNCTION_NAME calculate_watershimmer_bilinear
    #define GENERATE_FUNCTION_CALL calculate_watershimmer
    #include "GenerateBilinearFilter3D.cginc"
    
    // calculates the random shimmer effect.
    //
    // returns: the multiplier for the shadow map.
    //
    float calculate_randomshimmer(float3 world, float modifier)
    {
        // overlay the entire world with random blocks that change at 30FPS between 0.0 and 1.0.
        float stablerng = rand(world + frac(floor(_Time.y * 30) * 0.001));

        // clamp the range down to change the intensity.
        return modifier + (1.0 - modifier) * stablerng;
    }

    #define GENERATE_FUNCTION_NAME calculate_randomshimmer_bilinear
    #define GENERATE_FUNCTION_CALL calculate_randomshimmer
    #include "GenerateBilinearFilter3D.cginc"
};

StructuredBuffer<DynamicLight> dynamic_lights;
uint dynamic_lights_count;
uint realtime_lights_count;

StructuredBuffer<uint> lightmap;
uint lightmap_resolution;

float3 dynamic_ambient_color;

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

    // we are sample center, so it's the same as point sample.
    float2 pos = uv - 0.5;
    float2 f = frac(pos);
    uint2 pos_top_left = floor(pos);

    // we wish to do the following but with as few instructions as possible:
    //
    //float tl = lightmap_sample3x3(pos_top_left, channel);
    //float tr = lightmap_sample3x3(pos_top_left + uint2(1, 0), channel);
    //float bl = lightmap_sample3x3(pos_top_left + uint2(0, 1), channel);
    //float br = lightmap_sample3x3(pos_top_left + uint2(1, 1), channel);

    // read all of the lightmap samples we need in advance.
    float4x4 map;
    map[0][0] = lightmap_sample(pos_top_left + uint2(-1, -1), channel);
    map[0][1] = lightmap_sample(pos_top_left + uint2( 0, -1), channel);
    map[0][2] = lightmap_sample(pos_top_left + uint2( 1, -1), channel);
    map[0][3] = lightmap_sample(pos_top_left + uint2( 2, -1), channel);
    map[1][0] = lightmap_sample(pos_top_left + uint2(-1,  0), channel);
    map[1][1] = lightmap_sample(pos_top_left + uint2( 0,  0), channel);
    map[1][2] = lightmap_sample(pos_top_left + uint2( 1,  0), channel);
    map[1][3] = lightmap_sample(pos_top_left + uint2( 2,  0), channel);
    map[2][0] = lightmap_sample(pos_top_left + uint2(-1,  1), channel);
    map[2][1] = lightmap_sample(pos_top_left + uint2( 0,  1), channel);
    map[2][2] = lightmap_sample(pos_top_left + uint2( 1,  1), channel);
    map[2][3] = lightmap_sample(pos_top_left + uint2( 2,  1), channel);
    map[3][0] = lightmap_sample(pos_top_left + uint2(-1,  2), channel);
    map[3][1] = lightmap_sample(pos_top_left + uint2( 0,  2), channel);
    map[3][2] = lightmap_sample(pos_top_left + uint2( 1,  2), channel);
    map[3][3] = lightmap_sample(pos_top_left + uint2( 2,  2), channel);

    // there are several common overlapping 3x3 samples (marked as X).
    //
    // ----
    // -XX-
    // -XX-
    // ----
    // 
    // m00 m01 m02 m03
    // m10 m11 m12 m13
    // m20 m21 m22 m23
    // m30 m31 m32 m33
    //
    float common = map[1][1] + map[1][2] + map[2][1] + map[2][2];

    // for the top 3x3 samples there are more overlapping samples:
    //
    // -XX-
    // -XX-
    // -XX-
    // ----
    //
    float tcommon = common + map[0][1] + map[0][2];

    float tl = (tcommon + map[0][0] + map[1][0] + map[2][0]) / 9.0;
    float tr = (tcommon + map[0][3] + map[1][3] + map[2][3]) / 9.0;

    // for the bottom 3x3 samples there are more overlapping samples:
    //
    // ----
    // -XX-
    // -XX-
    // -XX-
    //
    float bcommon = common + map[3][1] + map[3][2];

    float bl = (bcommon + map[1][0] + map[2][0] + map[3][0]) / 9.0;
    float br = (bcommon + map[1][3] + map[2][3] + map[3][3]) / 9.0;

    // bilinear interpolation.
    return lerp(lerp(tl, tr, f.x), lerp(bl, br, f.x), f.y);
}

// [dynamic triangles technique]
//
// instead of iterating over all light sources in the scene per fragment,
// a lookup table is used to associate dynamic light indices per triangle.
// 
// +---------------+     +-----------------+
// |SV_PrimitiveID |--+->|Light Data Offset|
// |(TriangleIndex)|  |  +-----------------+
// +---------------+  +->|Light Data Offset|
//                    |  +-----------------+
//                    +->|...              |
//                       +--------+--------+
//                                |
//                                v
//                       +------------------+
// Light Data Offset --> |Light Count       |
//                       +------------------+
//                       |Light Index 1     | --> dynamic_lights[Light Index 1]
//                       +------------------+
//                       |Light Index 2     | --> dynamic_lights[Light Index 2]
//                       +------------------+
//                       |Light Index ...   | --> dynamic_lights[Light Index ...]
//                       +------------------+
//
StructuredBuffer<uint> dynamic_triangles;

// for a triangle gets the light count affecting it.
uint dynamic_triangles_light_count(uint triangle_index)
{
    // if we are dealing with a dynamic object then we iterate over all light indices.
    if (lightmap_resolution == 0)
        return dynamic_lights_count;
    
    return dynamic_triangles[dynamic_triangles[triangle_index]];
}

// for a triangle gets a light index affecting it.
uint dynamic_triangles_light_index(uint triangle_index, uint triangle_light_count, uint light_index)
{
    // if we are dealing with a dynamic object then we iterate over all light indices.
    if (lightmap_resolution == 0)
        return light_index;
    
    // light indices within the triangle light count return the associated light indices.
    if (light_index < triangle_light_count)
        return dynamic_triangles[dynamic_triangles[triangle_index] + 1 + light_index];
    
    // light indices beyond the triangle light count are used for realtime light sources.
    return dynamic_lights_count + light_index - triangle_light_count;
}