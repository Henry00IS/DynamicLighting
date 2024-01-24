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
    float  volumetricRadius;
    float  volumetricIntensity;
    float  volumetricThickness;
    float  volumetricVisibility;
    // -- 16 byte boundary --
    
    // the first 5 bits contain a valid channel index so mask by 31.
    uint get_shadow_channel()
    {
        return channel & 31u;
    }

    // bit 6 determines whether the light is realtime and does not have shadows.
    bool is_realtime()
    {
        return channel & 32u;
    }

    // bit 6 determines whether the light is realtime and does not have shadows.
    bool is_dynamic()
    {
        return !is_realtime();
    }

    // bit 7 determines whether the light is a spotlight.
    bool is_spotlight()
    {
        return channel & 64u;
    }

    // bit 8 determines whether the light is a discoball.
    bool is_discoball()
    {
        return channel & 128u;
    }

    // bit 9 determines whether the light has water shimmer.
    bool is_watershimmer()
    {
        return channel & 256u;
    }

    // bit 10 determines whether the light has random shimmer.
    bool is_randomshimmer()
    {
        return channel & 512u;
    }

    // bit 11 determines whether the light is a wave.
    bool is_wave()
    {
        return channel & 1024u;
    }

    // bit 12 determines whether the light is interference.
    bool is_interference()
    {
        return channel & 2048u;
    }

    // bit 13 determines whether the light is a rotor.
    bool is_rotor()
    {
        return channel & 4096u;
    }

    // bit 14 determines whether the light is a shockwave.
    bool is_shock()
    {
        return channel & 8192u;
    }

    // bit 15 determines whether the light is a disco.
    bool is_disco()
    {
        return channel & 16384u;
    }
    
    // bit 16 determines whether the light is volumetric.
    bool is_volumetric()
    {
        return channel & 32768u;
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

// fetches a shadow bit at the specified uv coordinates from the lightmap data.
bool lightmap_sample(uint2 uv, uint channel)
{
    return lightmap[uv.y * lightmap_resolution + uv.x] & (1u << channel);
}

// x x x
// x   x apply a simple 3x3 sampling with averaged results to the shadow bits.
// x x x
float lightmap_sample3x3(uint2 uv, uint channel)
{
    float map;

    map  = lightmap_sample(uint2(uv.x - 1u, uv.y - 1u), channel);
    map += lightmap_sample(uint2(uv.x     , uv.y - 1u), channel);
    map += lightmap_sample(uint2(uv.x + 1u, uv.y - 1u), channel);

    map += lightmap_sample(uint2(uv.x - 1u, uv.y     ), channel);
    map += lightmap_sample(uv                         , channel);
    map += lightmap_sample(uint2(uv.x + 1u, uv.y     ), channel);

    map += lightmap_sample(uint2(uv.x - 1u, uv.y + 1u), channel);
    map += lightmap_sample(uint2(uv.x     , uv.y + 1u), channel);
    map += lightmap_sample(uint2(uv.x + 1u, uv.y + 1u), channel);

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
    map[0][0] = lightmap_sample(uint2(pos_top_left.x - 1, pos_top_left.y - 1), channel);
    map[0][1] = lightmap_sample(uint2(pos_top_left.x    , pos_top_left.y - 1), channel);
    map[0][2] = lightmap_sample(uint2(pos_top_left.x + 1, pos_top_left.y - 1), channel);
    map[0][3] = lightmap_sample(uint2(pos_top_left.x + 2, pos_top_left.y - 1), channel);
    map[1][0] = lightmap_sample(uint2(pos_top_left.x - 1, pos_top_left.y    ), channel);
    map[1][1] = lightmap_sample(pos_top_left                                 , channel);
    map[1][2] = lightmap_sample(uint2(pos_top_left.x + 1, pos_top_left.y    ), channel);
    map[1][3] = lightmap_sample(uint2(pos_top_left.x + 2, pos_top_left.y    ), channel);
    map[2][0] = lightmap_sample(uint2(pos_top_left.x - 1, pos_top_left.y + 1), channel);
    map[2][1] = lightmap_sample(uint2(pos_top_left.x    , pos_top_left.y + 1), channel);
    map[2][2] = lightmap_sample(uint2(pos_top_left.x + 1, pos_top_left.y + 1), channel);
    map[2][3] = lightmap_sample(uint2(pos_top_left.x + 2, pos_top_left.y + 1), channel);
    map[3][0] = lightmap_sample(uint2(pos_top_left.x - 1, pos_top_left.y + 2), channel);
    map[3][1] = lightmap_sample(uint2(pos_top_left.x    , pos_top_left.y + 2), channel);
    map[3][2] = lightmap_sample(uint2(pos_top_left.x + 1, pos_top_left.y + 2), channel);
    map[3][3] = lightmap_sample(uint2(pos_top_left.x + 2, pos_top_left.y + 2), channel);

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
    return dynamic_triangles[dynamic_triangles[triangle_index]];
}

// for a triangle gets a light index affecting it.
uint dynamic_triangles_light_index(uint triangle_index, uint triangle_light_count, uint light_index)
{
    // light indices within the triangle light count return the associated light indices.
    if (light_index < triangle_light_count)
        return dynamic_triangles[dynamic_triangles[triangle_index] + 1 + light_index];
    
    // light indices beyond the triangle light count are used for realtime light sources.
    return dynamic_lights_count + light_index - triangle_light_count;
}

// [dynamic mesh acceleration]
//
// instead of iterating over all light sources in the scene per fragment,
// we use a bounding volume hierarchy acceleration structure.

struct DynamicLightBvhNode
{
    float3 aabbMin;
    uint   leftFirst;
    // -- 16 byte boundary --
    float3 aabbMax;
    uint   count;
    // -- 16 byte boundary --
    
    // gets whether this node is a leaf containing light source indices.
    bool is_leaf()
    {
        return count > 0;
    }
    
    // when is_leaf() then contains the first light index.
    uint get_first_light_index()
    {
        return leftFirst;
    }
    
    // when not is_leaf() then contains the left node index.
    uint get_left_node_index()
    {
        return leftFirst;
    }
    
    // when not is_leaf() then contains the right node index.
    uint get_right_node_index()
    {
        return leftFirst + 1;
    }
};

StructuredBuffer<DynamicLightBvhNode> dynamic_lights_bvh;

// next we prepare macro statements that shaders use to implement their fragment functions.

#define DYNLIT_FRAGMENT_FUNCTION \
void dynlit_frag_light(v2f i, uint triangle_index:SV_PrimitiveID, inout DynamicLight light, DYNLIT_FRAGMENT_LIGHT_OUT_PARAMETERS);\
\
fixed4 frag (v2f i, uint triangle_index:SV_PrimitiveID) : SV_Target

#if DYNAMIC_LIGHTING_BVH

    #define DYNLIT_FRAGMENT_INTERNAL \
    if (lightmap_resolution == 0)\
    {\
        \
        /* we traverse the bounding volume hierarchy starting at the root node: */ \
        DynamicLightBvhNode stack[32];\
        uint stackPointer = 0;\
        DynamicLightBvhNode node = dynamic_lights_bvh[0];\
        \
        /* instead of 'true' we can cheaply prevent an infinite loop and stack overflow. */ \
        while (stackPointer <= 30)\
        {\
            /* if the current node is a leaf (has light indices): */ \
            if (node.is_leaf())\
            {\
                /* process the light indices: */ \
                for (uint k = node.get_first_light_index(); k < node.get_first_light_index() + node.count; k++)\
                {\
                    DynamicLight light = dynamic_lights[k];\
                    \
                    dynlit_frag_light(i, triangle_index, light, DYNLIT_FRAGMENT_LIGHT_IN_PARAMETERS); \
                }\
                \
                /* check whether we are done traversing the bvh: */ \
                if (stackPointer == 0) break; else node = stack[--stackPointer]; \
                continue; \
            }\
            \
            /* find the left and right child node. */ \
            DynamicLightBvhNode left = dynamic_lights_bvh[node.get_left_node_index()];\
            DynamicLightBvhNode right = dynamic_lights_bvh[node.get_right_node_index()];\
            \
            if (point_in_aabb(i.world, left.aabbMin, left.aabbMax)) \
                stack[stackPointer++] = left; \
            \
            if (point_in_aabb(i.world, right.aabbMin, right.aabbMax)) \
                stack[stackPointer++] = right; \
            \
            if (stackPointer == 0) break; else node = stack[--stackPointer]; \
        }\
        \
        /* iterate over every realtime light in the scene: */ \
        for (uint k = 0; k < realtime_lights_count; k++)\
        {\
            /* get the current light from memory. */ \
            DynamicLight light = dynamic_lights[dynamic_lights_count + k];\
            \
            dynlit_frag_light(i, triangle_index, light, DYNLIT_FRAGMENT_LIGHT_IN_PARAMETERS);\
        }\
    }\
    else\
    {\
        /* iterate over every dynamic light affecting this triangle: */ \
        uint triangle_light_count = dynamic_triangles_light_count(triangle_index);\
        for (uint k = 0; k < triangle_light_count + realtime_lights_count; k++)\
        {\
            /* get the current light from memory. */ \
            DynamicLight light = dynamic_lights[dynamic_triangles_light_index(triangle_index, triangle_light_count, k)];\
            \
            dynlit_frag_light(i, triangle_index, light, DYNLIT_FRAGMENT_LIGHT_IN_PARAMETERS);\
        }\
    }

#else

    #define DYNLIT_FRAGMENT_INTERNAL \
    if (lightmap_resolution == 0)\
    {\
        /* iterate over every dynamic light in the scene (slow without bvh): */ \
        for (uint k = 0; k < dynamic_lights_count + realtime_lights_count; k++)\
        {\
            /* get the current light from memory. */ \
            DynamicLight light = dynamic_lights[k];\
            \
            dynlit_frag_light(i, triangle_index, light, DYNLIT_FRAGMENT_LIGHT_IN_PARAMETERS);\
        }\
    }\
    else\
    {\
        /* iterate over every dynamic light affecting this triangle: */ \
        uint triangle_light_count = dynamic_triangles_light_count(triangle_index);\
        for (uint k = 0; k < triangle_light_count + realtime_lights_count; k++)\
        {\
            /* get the current light from memory. */ \
            DynamicLight light = dynamic_lights[dynamic_triangles_light_index(triangle_index, triangle_light_count, k)];\
            \
            dynlit_frag_light(i, triangle_index, light, DYNLIT_FRAGMENT_LIGHT_IN_PARAMETERS);\
        }\
    }

#endif

#define DYNLIT_FRAGMENT_LIGHT void dynlit_frag_light(v2f i, uint triangle_index:SV_PrimitiveID, inout DynamicLight light, DYNLIT_FRAGMENT_LIGHT_OUT_PARAMETERS)

#define DYNLIT_FRAGMENT_UNLIT \
fixed4 frag (v2f i) : SV_Target\
{\
    return tex2D(_MainTex, i.uv0);\
}