#include "Common.cginc"

// macros to name the general purpose variables.
#define light_cutoff gpFloat1
#define light_outerCutoff gpFloat2
#define light_waveTime gpFloat1
#define light_waveFrequency gpFloat2
#define light_rotorCenter gpFloat3
#define light_discoVerticalTime gpFloat3

#define light_type_point 0
#define light_type_spot 1 << 6
#define light_type_discoball 2 << 6
#define light_type_wave 3 << 6
#define light_type_interference 4 << 6
#define light_type_rotor 5 << 6
#define light_type_shock 6 << 6
#define light_type_disco 7 << 6

#define light_shimmer_none 0
#define light_shimmer_water 1 << 10
#define light_shimmer_random 2 << 10

// define an additional medium quality keyword without using a global keyword.
//
// DYNAMIC_LIGHTING_INTEGRATED_GRAPHICS
// DYNAMIC_LIGHTING_QUALITY_LOW
// DYNAMIC_LIGHTING_QUALITY_MEDIUM
// DYNAMIC_LIGHTING_QUALITY_HIGH
//
#if !defined(DYNAMIC_LIGHTING_INTEGRATED_GRAPHICS) && !defined(DYNAMIC_LIGHTING_QUALITY_LOW) && !defined(DYNAMIC_LIGHTING_QUALITY_HIGH)
    #define DYNAMIC_LIGHTING_QUALITY_MEDIUM
#endif

// if you want to skip all dynamic geometry lighting work, you can define the
// disabled keyword instead of the bvh keyword and implement your own system.
//
#ifndef DYNAMIC_LIGHTING_DYNAMIC_GEOMETRY_DISTANCE_CUBES
    // #define DYNAMIC_LIGHTING_DYNAMIC_GEOMETRY_DISABLED
#endif

// switch to guassian shadow sampling when in high quality mode.
//
#if defined(DYNAMIC_LIGHTING_QUALITY_HIGH) && !defined(DYNAMIC_LIGHTING_SHADOW_SAMPLER)
    #define DYNAMIC_LIGHTING_SHADOW_SAMPLER shadow_sample_gaussian5
#endif

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
    float  volumetricIntensity;
    float  volumetricVisibility;
    uint   cookieIndex;
    uint   shadowCubemapIndex;
    // -- 16 byte boundary --
    float  falloff;
    float3 bounceColor;
    // -- 16 byte boundary --
    
    // the first 5 bits are unused (used to be channel index).

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
    
    // bit 7-10 determines the light type (see the light_type_ defines above).
    uint get_type()
    {
        return channel & 960u;
    }
    
    // bit 11-12 determines the light shimmering (see the light_shimmer_ defines above).
    uint get_shimmer()
    {
        return channel & 3072u;
    }
    
    // bit 16 determines whether the light has a shadow cubemap.
    bool is_shadow_available()
    {
        return channel & 32768u;
    }
    
    // bit 17 determines whether the light has a cookie texture.
    bool is_cookie_available()
    {
        return channel & 65536u;
    }
    
    // bit 18-21 determine the bounce light compression level (1-8).
    uint bounce_compression_level()
    {
        return 1 + ((channel & 917504u) >> 17);
    }
    
    // calculates the spotlight effect.
    //
    // returns:
    // x: the cutoff angle theta.
    // y: the intensity for a smooth transition.
    // 
    // example:
    // 
    //    // anything outside of the spot light can be skipped.
    //    float2 spotlight = light_calculate_spotlight(light, light_direction);
    //    if (spotlight.x <= light.outerCutoff)
    //        continue;
    //    map *= spotlight.y;
    //
    float2 calculate_spotlight(float3 light_direction)
    {
        float theta = dot(light_direction, forward);
        float epsilon = light_cutoff - light_outerCutoff;
        float intensity = saturate((theta - light_outerCutoff) / epsilon);
        return float2(theta, intensity);
    }
    
    // calculates the spotlight effect when bounce lighting is available.
    //
    // returns:
    // x: the direct lighting intensity for a smooth transition.
    // y: the bounce lighting intensity for a smooth transition.
    //
    float3 calculate_spotlight_bounce(float3 light_direction)
    {
        float theta = dot(light_direction, forward);
        float2 epsilon = float2(light_cutoff - light_outerCutoff, light_cutoff + 1.0);
        float2 intensity = saturate(float2(theta - light_outerCutoff, theta + 1.0) / epsilon);
        // compute the bounce size factor based on the cutoff angle.
        float spot_size_factor = min(1.0, 1.0 - light_outerCutoff);
        return float3(theta, intensity.x, intensity.y * spot_size_factor);
    }
    
    // calculates the discoball spotlights effect.
    //
    // returns:
    // x: the cutoff angle theta.
    // y: the intensity for a smooth transition.
    // 
    // example:
    // 
    //    // anything outside of the spot lights can be skipped.
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
    
    // calculates the discoball spotlights effect.
    //
    // returns:
    // x: the direct lighting intensity for a smooth transition.
    // y: the bounce lighting intensity for a smooth transition.
    //
    float2 calculate_discoball_bounce(float3 light_direction)
    {
        float3x3 rot = look_at_matrix(forward, up);

        float3 rotated_direction = mul(light_direction, rot);
        float theta = dot(snap_direction(rotated_direction), rotated_direction);
        float2 epsilon = float2(light_cutoff - light_outerCutoff, light_cutoff - 0.75);
        float2 intensity = saturate(float2(theta - light_outerCutoff, theta - 0.75) / epsilon);
        // compute the bounce size factor based on the cutoff angle.
        float spot_size_factor = min(1.0, (1.0 - light_outerCutoff) * 10.0);
        return float2(intensity.x, intensity.y * spot_size_factor);
    }
    
    // calculates the wave effect.
    float calculate_wave(float3 world)
    {
        // [CPU] light_waveFrequency *= UNITY_PI * 2.0
        return 0.7 + 0.3 * sin((distance(position, world) - light_waveTime) * light_waveFrequency);
    }
    
    // calculates the interference effect.
    float calculate_interference(float3 light_position_minus_world)
    {
        // [CPU] light_waveFrequency *= UNITY_PI
        // [CPU] light_waveTime *= UNITY_PI * 2.0
        float3x3 rot = look_at_matrix(forward, up);
        float3 world = mul(light_position_minus_world, rot);
        
        float angle = atan2(sqrt((world.x * world.x) + (world.z * world.z)), world.y) * light_waveFrequency;
        float scale = 0.5 + 0.5 * cos(angle - light_waveTime);
        return scale;
    }
    
    // calculates the rotor effect.
    float calculate_rotor(float3 light_position_minus_world)
    {
        // [CPU] light_waveTime *= UNITY_PI * 2.0
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
        float scale = 0.5 + 0.5 * cos(angle + light_waveTime);
        
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
	    float brightness = 0.9 + 0.1 * sin((dist * 2.0 - light_waveTime) * UNITY_PI * 2.0);
	    brightness      *= 0.9 + 0.1 * cos((dist + light_waveTime) * UNITY_PI * 2.0);
	    brightness      *= 0.9 + 0.1 * sin((dist / 2.0 - light_waveTime) * UNITY_PI * 2.0);
        return brightness;
    }
    
    // calculates the disco effect.
    float calculate_disco(float3 light_position_minus_world)
    {
        // [CPU] light_waveTime *= UNITY_PI * 2.0
        // [CPU] light_discoVerticalTime *= UNITY_PI * 2.0
        float3x3 rot = look_at_matrix(forward, up);
        float3 world = mul(light_position_minus_world, rot);

	    float horizontal = light_waveFrequency * atan2(world.x, world.z);
	    float vertical = light_waveFrequency * atan2(sqrt(world.x * world.x + world.z * world.z), world.y);

	    float scale1 = 0.5 + 0.5 * cos(horizontal + light_waveTime);
	    float scale2 = 0.5 + 0.5 * cos(vertical - light_discoVerticalTime);

	    float scale  = scale1 + scale2 - scale1 * scale2;

	    float dist = 0.5 * (world.x * world.x + world.z * world.z);
	    if (dist < 1.0) scale *= dist;

        return 1.0 - scale;
    }
    
    // calculates the water shimmer effect.
    //
    // returns: the multiplier for the shadow map.
    //
    float calculate_watershimmer(float3 world)
    {
        // overlay the entire world with random blocks that never change between 0.0 and 1.0.
        float stablerng = rand(world);

        // use a sine wave to change the brightness of the stable random blocks.
        return 1.0 - abs(sin(stablerng * _Time.w + _Time.x)) * (1.0 - shimmerModifier);
    }

    #define GENERATE_FUNCTION_NAME calculate_watershimmer_bilinear
    #define GENERATE_FUNCTION_CALL calculate_watershimmer
    #include "GenerateBilinearFilter3D.cginc"
    
    // calculates the random shimmer effect.
    //
    // returns: the multiplier for the shadow map.
    //
    float calculate_randomshimmer(float3 world)
    {
        // overlay the entire world with random blocks that change at 30FPS between 0.0 and 1.0.
        float stablerng = rand(world + frac(floor(_Time.y * 30) * 0.001));

        // clamp the range down to change the intensity.
        return shimmerModifier + (1.0 - shimmerModifier) * stablerng;
    }
    
    // special thanks to Nikita Lisitsa https://lisyarus.github.io/blog/posts/point-light-attenuation.html
    // calculates attenuation for the light source.
    float calculate_attenuation(float distanceSqr)
    {
        float s = saturate(distanceSqr / radiusSqr);
        return intensity * pow(1.0 - s, 2.0) / (1.0 + falloff * s);
    }

    #define GENERATE_FUNCTION_NAME calculate_randomshimmer_bilinear
    #define GENERATE_FUNCTION_CALL calculate_randomshimmer
    #include "GenerateBilinearFilter3D.cginc"
};

StructuredBuffer<DynamicLight> dynamic_lights;
uint dynamic_lights_count;
uint realtime_lights_count;

int triangle_index_submesh_offset;
uint lightmap_resolution;

TextureCubeArray shadow_cubemaps;
sampler sampler_shadow_cubemaps;

Texture2DArray light_cookies;
sampler sampler_light_cookies;

float3 dynamic_ambient_color;

// [distance cubes technique]
//
// there is only shadow occlusion data for raycasted static geometry,
// light will leak through walls onto dynamic meshes so we need a fix.
//
// stale cubemaps with distances are used to compute occlusion like
// realtime shadows, except that they rarely/never update their data.
//
// +-----------+        +-------------+
// |Light Index|-----+->|Index*32*32*6|
// +-----------+     |  +-------------+
//                   +->|Index*32*32*6|
//                      +-------------+
//                      |...          |
//                      +-------------+
//                             |
//                             v
//                      +---------------+
// Cube Data Offset --> |Distance Floats|
//                      +---------------+
//
StructuredBuffer<uint> dynamic_lights_distance_cubes;

TextureCube dynamic_lights_distance_cubes_lookup32;
sampler sampler_dynamic_lights_distance_cubes_lookup32;

float sample_distance_cube(uint cubeDataOffset, float3 dir)
{
    // sample the cubemap lookup texture of array indices to avoid doing complex math.
    uint index = dynamic_lights_distance_cubes_lookup32.SampleLevel(sampler_dynamic_lights_distance_cubes_lookup32, dir, 0);
    return asfloat(dynamic_lights_distance_cubes[cubeDataOffset + index]);
}

float sample_distance_cube_tiny(uint cubeDataOffset, float3 world, float3 lightPos, float3 normal)
{
    float3 light_direction = lightPos - world;
    float light_distanceSqr = dot(light_direction, light_direction);
    float light_distance = sqrt(light_distanceSqr);
    
    float shadow_distance = sample_distance_cube(cubeDataOffset, light_direction);
    
    light_direction = normalize(light_direction);
    float NdotL = max(dot(normal, light_direction), 0);
    
    // magic bias function! it is amazing!
    float magic = 0.02 + 0.01 * light_distance;
    float autobias = magic * tan(acos(1.0 - NdotL));
    autobias = clamp(autobias, 0.0, magic);
    
    // check whether the fragment is occluded.
    return (light_distance - autobias <= shadow_distance);
}

float sample_distance_cube_bilinear(uint dynamicLightIndex, float light_distanceSqr, float3 light_direction, float3 world, float3 normal, float penumbraScale = 1.0)
{
    #define DYNLIT_ANGULAR_RADIUS 0.05
    static const float2 poisson_disk[9] =
    {
        DYNLIT_ANGULAR_RADIUS * float2(-0.326, -0.406),
        DYNLIT_ANGULAR_RADIUS * float2(-0.840, -0.074),
        DYNLIT_ANGULAR_RADIUS * float2(-0.696, 0.457),
        DYNLIT_ANGULAR_RADIUS * float2(-0.203, 0.621),
        DYNLIT_ANGULAR_RADIUS * float2(0.962, -0.195),
        DYNLIT_ANGULAR_RADIUS * float2(0.473, -0.480),
        DYNLIT_ANGULAR_RADIUS * float2(0.519, 0.767),
        DYNLIT_ANGULAR_RADIUS * float2(0.185, -0.893),
        DYNLIT_ANGULAR_RADIUS * float2(0.507, 0.064)
    };
    #undef DYNLIT_ANGULAR_RADIUS
    
    // calculate the cube data offset in memory.
    uint cubeDataOffset = dynamicLightIndex * 64 * 64 * 6;

    float light_distance = sqrt(light_distanceSqr);
    
    // build tangent basis (orthonormal frame for disk sampling).
    float3 dot_normal_light_direction = dot(normal, light_direction);
    float3 up = abs(dot_normal_light_direction) > 0.999 ? float3(0, 1, 0) : normal; // fallback to avoid zero cross.
    float3 tangent = normalize(cross(light_direction, up));
    float3 bitangent = cross(light_direction, tangent);

    float NdotL = max(dot_normal_light_direction, 0);
        
    // magic bias function! it is amazing!
    float magic = 0.04 + 0.02 * light_distance;
    float autobias = magic * tan(acos(1.0 - NdotL));
    autobias = clamp(autobias, 0.0, magic);
    
    // pre-computer the light distance minus bias.
    light_distance -= autobias;
    
    float num_samples = 9.0;
    float accumulated = 0.0;

    [unroll]
    for (int i = 0; i < 9; ++i)
    {
        float rng = (-0.5 + rand((i + 1) * world));
        float2 disk_offset = poisson_disk[i] * rng;

        float3 offset_dir = light_direction + tangent * disk_offset.x + bitangent * disk_offset.y;
        float sample_d = sample_distance_cube(cubeDataOffset, offset_dir);

        accumulated += (light_distance <= sample_d + rng * 0.5);
    }

    return accumulated / num_samples;
}

float sample_distance_cube_bilinear_old(uint dynamicLightIndex, float3 world, float3 lightPos, float3 normal)
{
    // calculate the cube data offset in memory.
    uint cubeDataOffset = dynamicLightIndex * 32 * 32 * 6;
    
    float gridScale = 0.25; // blurry approximation.
    
    // convert world position to grid coordinates based on the grid scale.
    float3 gridCoord = world / gridScale;
    
    // calculate the weights for the bilinear interpolation.
    float3 weight = frac(gridCoord);
    
    // calculate the integer part of the grid coordinates.
    float3 gridCoordInt = floor(gridCoord);
    
    // convert grid coordinates back to world positions for sampling.
    float3 baseWorldPos = gridCoordInt * gridScale;
    
    // sample the texture at the neighboring cells
    float topLeftFront     = sample_distance_cube_tiny(cubeDataOffset, baseWorldPos, lightPos, normal); 
    float topRightFront    = sample_distance_cube_tiny(cubeDataOffset, baseWorldPos + float3(gridScale, 0, 0), lightPos, normal); 
    float bottomLeftFront  = sample_distance_cube_tiny(cubeDataOffset, baseWorldPos + float3(0, gridScale, 0), lightPos, normal); 
    float bottomRightFront = sample_distance_cube_tiny(cubeDataOffset, baseWorldPos + float3(gridScale, gridScale, 0), lightPos, normal); 
    float topLeftBack      = sample_distance_cube_tiny(cubeDataOffset, baseWorldPos + float3(0, 0, gridScale), lightPos, normal); 
    float topRightBack     = sample_distance_cube_tiny(cubeDataOffset, baseWorldPos + float3(gridScale, 0, gridScale), lightPos, normal); 
    float bottomLeftBack   = sample_distance_cube_tiny(cubeDataOffset, baseWorldPos + float3(0, gridScale, gridScale), lightPos, normal); 
    float bottomRightBack  = sample_distance_cube_tiny(cubeDataOffset, baseWorldPos + float3(gridScale, gridScale, gridScale), lightPos, normal); 
    
    // perform bilinear interpolation in the x direction.
    float4 dx = lerp(float4(topLeftFront , bottomLeftFront , topLeftBack , bottomLeftBack),
                     float4(topRightFront, bottomRightFront, topRightBack, bottomRightBack),
                     weight.x);
    
    // perform bilinear interpolation in the y direction.
    float2 dy = lerp(float2(dx.x, dx.z),
                     float2(dx.y, dx.w),
                     weight.y);

    // perform bilinear interpolation in the z direction.
    return lerp(dy.x, dy.y, weight.z);
}

// [dynamic triangles technique]
//
// instead of iterating over all light sources in the scene per fragment,
// a lookup table is used to associate dynamic light indices per triangle.
// 
// +---------------+     +------------------+
// |SV_PrimitiveID |--+->|Light Data Offset |
// |(TriangleIndex)|  |  +------------------+
// +---------------+  |  |Triangle Bounds X |
//                    |  +------------------+
//                    |  |Triangle Bounds Y |
//                    |  +------------------+
//                    |  |Triangle Bounds W |
//                    |  +------------------+
//                    +->|Light Data Offset |
//                       +------------------+
//                       |...               |
//                       +------------------+
//                                |
//                                v
//                       +--------------------+
// Light Data Offset --> |Light Count         |
//                       +--------------------+
//                       |Light Index 1       | --> dynamic_lights[Light Index 1]
//                       +--------------------+
//                       |Shadow Data Offset 1| --> dynamic_lights[+1]
//                       +--------------------+
//                       |Bounce Data Offset 1| --> dynamic_lights[+2]              ONLY IF DYNAMIC_LIGHTING_BOUNCE ENABLED
//                       +--------------------+
//                       |Light Index 2       | --> dynamic_lights[Light Index 2]
//                       +--------------------+
//                       |Shadow Data Offset 2| --> dynamic_lights[+1]
//                       +--------------------+
//                       |Bounce Data Offset 2| --> dynamic_lights[+2]              ONLY IF DYNAMIC_LIGHTING_BOUNCE ENABLED
//                       +--------------------+
//                       |...                 |
//                       +--------------------+
//
StructuredBuffer<uint> dynamic_triangles;

struct DynamicTriangle
{
    // the bounds of the triangle.
    uint3 bounds;
    // offset into dynamic_triangles[] for the light data.
    uint lightDataOffset;
    // the amount of light sources affecting the triangle.
    uint lightCount;
    
    // offset into dynamic_lights[] for the active light.
    uint activeLightDynamicLightsIndex;
    // offset into dynamic_triangles[] for the shadow data.
    uint activeLightShadowDataOffset;
#if defined(DYNAMIC_LIGHTING_BOUNCE) && !defined(DYNAMIC_LIGHTING_INTEGRATED_GRAPHICS)
    // offset into dynamic_triangles[] for the bounce data.
    uint activeLightBounceDataOffset;
#endif
    
    void initialize()
    {
        bounds = uint3(0, 0, 0);
        lightDataOffset = 0;
        lightCount = 0;
        activeLightDynamicLightsIndex = 0;
        activeLightShadowDataOffset = 0;
#if defined(DYNAMIC_LIGHTING_BOUNCE) && !defined(DYNAMIC_LIGHTING_INTEGRATED_GRAPHICS)
        activeLightBounceDataOffset = 0;
#endif
    }
    
    // loads this struct for a triangle from the dynamic triangles data.
    void load(uint triangle_index)
    {
        // read the dynamic triangles header.
        uint offset = (triangle_index + triangle_index_submesh_offset) * 4; // struct size.
        
        // read the offset into dynamic_triangles[] for the light data.
        lightDataOffset = dynamic_triangles[offset++];
        
        // read the bounds of the triangle as floats.
        bounds.x = dynamic_triangles[offset++];
        bounds.y = dynamic_triangles[offset++];
        bounds.z = dynamic_triangles[offset];
        
        // we increase the light data offset by one to skip the light count field.
        lightCount = dynamic_triangles[lightDataOffset++];
        
        // we can crash the gpu driver when we read a garbage light count.
        // 32 overlapping lights limit doesn't restrict a triangle from being hit by more.
        if (lightCount > 256) lightCount = 256;
    }
    
    // sets the active triangle light index for light related queries.
    void set_active_light_index(uint light_index)
    {
        // light indices within the triangle light count return the associated light indices.
        if (light_index < lightCount)
        {
#if DYNAMIC_LIGHTING_BOUNCE
            uint offset = lightDataOffset + light_index * 3; // struct size.
#else
            uint offset = lightDataOffset + light_index * 2; // struct size.
#endif      
            // read the dynamic light index to be used.
            activeLightDynamicLightsIndex = dynamic_triangles[offset++];
            
            // read the shadow data offset.
            activeLightShadowDataOffset = dynamic_triangles[offset++];
            
#if defined(DYNAMIC_LIGHTING_BOUNCE) && !defined(DYNAMIC_LIGHTING_INTEGRATED_GRAPHICS)
            // read the bounce data offset.
            activeLightBounceDataOffset = dynamic_triangles[offset];
#endif      
            return;
        }
        
        // light indices beyond the triangle light count are used for realtime light sources.
        activeLightDynamicLightsIndex = dynamic_lights_count + light_index - lightCount;
    }
    
    // for a triangle gets the dynamic light source affecting it.
    uint get_dynamic_light_index()
    {
        return activeLightDynamicLightsIndex;
    }
    
    // fetches a shadow bit at the specified index from the shadow data.
    // note: requires 'uv -= bounds.xy' to be calculated up front.
    bool shadow_sample_index(uint index)
    {
        return dynamic_triangles[activeLightShadowDataOffset + index / 32] & (1 << index % 32);
    }
    
    // fetches a shadow bit at the specified uv coordinates from the shadow data.
    // note: requires 'uv -= bounds.xy' to be calculated up front.
    bool shadow_sample(uint2 uv)
    {
        uint index = uv.y * bounds.z + uv.x;
        return dynamic_triangles[activeLightShadowDataOffset + index / 32] & (1 << index % 32);
    }
    
    // x x x
    // x   x apply a simple 3x3 gaussian blur to the shadow bits.
    // x x x
    // note: requires 'uv -= bounds.xy' to be calculated up front.
    float shadow_sample_gaussian3(uint2 uv)
    {
        const float weights[3][3] = {
            {1, 2, 1},
            {2, 4, 2},
            {1, 2, 1}
        };
        
        float map = 0.0;
        
        for (int i = -1; i <= 1; ++i) {
            for (int j = -1; j <= 1; ++j) {
                map += weights[i + 1][j + 1] * shadow_sample(uv + uint2(i, j));
            }
        }

        return map / 16.0; // normalize by the sum of the weights.
    }
    
    // x x x x x
    // x x x x x apply a 5x5 Gaussian blur to the shadow bits.
    // x x   x x
    // x x x x x
    // x x x x x
    // note: requires 'uv -= bounds.xy' to be calculated up front.
    float shadow_sample_gaussian5(uint2 uv)
    {
        const float weights[5][5] =
        {
            { 1,  4,  6,  4, 1 },
            { 4, 16, 24, 16, 4 },
            { 6, 24, 36, 24, 6 },
            { 4, 16, 24, 16, 4 },
            { 1,  4,  6,  4, 1 }
        };
        
        float map = 0.0;
        
        for (int i = -2; i <= 2; ++i)
        {
            for (int j = -2; j <= 2; ++j)
            {
                map += weights[i + 2][j + 2] * shadow_sample(uv + uint2(i, j));
            }
        }
        
        return map / 256.0; // normalize by the sum of the weights.
    }
    
    #ifndef DYNAMIC_LIGHTING_SHADOW_SAMPLER
    #define DYNAMIC_LIGHTING_SHADOW_SAMPLER_INDEX
    #define DYNAMIC_LIGHTING_SHADOW_SAMPLER shadow_sample_index
    #endif
    
    // x x x
    // x   x apply a simple 3x3 sampling with averaged results to the shadow bits.
    // x x x
    float shadow_sample3x3(uint2 uv)
    {
        float map = 0.0;
        
        // offset the lightmap triangle uv to the top-left corner to read near zero, zero.
        uv -= bounds.xy;

        [unroll]
        for (int y = -1; y <= 1; y++)
        {
            uint index = (uv.y + y) * bounds.z + uv.x;
            [unroll]
            for (int x = -1; x <= 1; x++)
                map += shadow_sample_index(index + x);
        }

        return map / 9.0;
    }
    
    // x x x
    // x   x apply 4x 3x3 sampling with interpolation to get bilinear filtered shadow bits.
    // x x x
    float shadow_sample_bilinear(float2 uv)
    {
        // huge shoutout to neu_graphic for their software bilinear filter shader.
        // https://www.shadertoy.com/view/4sBSRK

        // we are sample center, so it's the same as point sample.
        float2 pos = uv - 0.5;
        float2 f = frac(pos);
        uint2 pos_top_left = floor(pos);

        // we wish to do the following but with as few instructions as possible:
        //
        //float tl = lightmap_sample3x3(pos_top_left);
        //float tr = lightmap_sample3x3(pos_top_left + uint2(1, 0));
        //float bl = lightmap_sample3x3(pos_top_left + uint2(0, 1));
        //float br = lightmap_sample3x3(pos_top_left + uint2(1, 1));
        
        // offset the lightmap triangle uv to the top-left corner to read near zero, zero.
        pos_top_left -= bounds.xy;

        // read all of the lightmap samples we need in advance.
        float map[4][4];
        [unroll]
        for (int y = -1; y <= 2; y++)
        {
#ifdef DYNAMIC_LIGHTING_SHADOW_SAMPLER_INDEX
            uint index = (pos_top_left.y + y) * bounds.z + pos_top_left.x;
            [unroll]
            for (int x = -1; x <= 2; x++)
                map[y + 1][x + 1] = DYNAMIC_LIGHTING_SHADOW_SAMPLER(index + x);
#else
            [unroll]
            for (int x = -1; x <= 2; x++)
                map[y + 1][x + 1] = DYNAMIC_LIGHTING_SHADOW_SAMPLER(uint2(pos_top_left.x + x, pos_top_left.y + y));
#endif
        }
        
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

        float tl = (tcommon + map[0][0] + map[1][0] + map[2][0]);
        float tr = (tcommon + map[0][3] + map[1][3] + map[2][3]);

        // for the bottom 3x3 samples there are more overlapping samples:
        //
        // ----
        // -XX-
        // -XX-
        // -XX-
        //
        float bcommon = common + map[3][1] + map[3][2];

        float bl = (bcommon + map[1][0] + map[2][0] + map[3][0]);
        float br = (bcommon + map[1][3] + map[2][3] + map[3][3]);
        
        // bilinear interpolation (simd).
        float2 c = lerp(float2(tl, bl), float2(tr, br), f.x);
        return lerp(c.x, c.y, f.y) / 9.0;
    }
    
    // x x
    // x x apply 4x sampling with interpolation to get bilinear filtered shadow bits.
    //
    float shadow_sample_integrated(float2 uv)
    {
        // huge shoutout to neu_graphic for their software bilinear filter shader.
        // https://www.shadertoy.com/view/4sBSRK

        // we are sample center, so it's the same as point sample.
        float2 pos = uv - 0.5;
        float2 f = frac(pos);
        uint2 pos_top_left = floor(pos);

        // offset the lightmap triangle uv to the top-left corner to read near zero, zero.
        pos_top_left -= bounds.xy;
        
        float tl = shadow_sample(pos_top_left);
        float tr = shadow_sample(pos_top_left + uint2(1, 0));
        float bl = shadow_sample(pos_top_left + uint2(0, 1));
        float br = shadow_sample(pos_top_left + uint2(1, 1));
        
        // bilinear interpolation (simd).
        float2 c = lerp(float2(tl, bl), float2(tr, br), f.x);
        return lerp(c.x, c.y, f.y);
    }
    
#if defined(DYNAMIC_LIGHTING_BOUNCE) && !defined(DYNAMIC_LIGHTING_INTEGRATED_GRAPHICS)
    // fetches a bounce pixel at the specified uv coordinates from the bounce texture data.
    // note: requires 'uv -= bounds.xy' to be calculated up front.
    float bounce_sample(uint index, uint bouncePixels, uint bounceBpp, uint bounceMask)
    {
        // the bounce texture data is compressed with multiple pixels in one uint.
        uint uintIndex = index / bouncePixels; // determine the index of the uint in the buffer.
        uint byteIndex = index % bouncePixels; // find the byte position within the uint.
        
        // read the uint from memory.
        uint value = dynamic_triangles[activeLightBounceDataOffset + uintIndex];
        
        uint shift = byteIndex * bounceBpp;         // calculate the shift amount for the correct byte.
        float byte = (value >> shift) & bounceMask; // extract the desired byte.
        return byte / bounceMask;                   // convert the byte to a float in [0, 1].
    }
    
    // fetches a bilinearly filtered bounce pixel at the specified uv coordinates from the bounce texture data.
    float bounce_sample_bilinear(DynamicLight light, float2 uv)
    {
        // huge shoutout to neu_graphic for their software bilinear filter shader.
        // https://www.shadertoy.com/view/4sBSRK
        
        // we are sample center, so it's the same as point sample.
        float2 pos = uv - 0.5;
        float2 f = frac(pos);
        uint2 pos_top_left = floor(pos);
        
        // offset the lightmap triangle uv to the top-left corner to read near zero, zero.
        pos_top_left -= bounds.xy;
        
        // the bounce texture data is compressed with multiple pixels in one uint.
        uint bounceBpp = light.bounce_compression_level();
        uint bouncePixels = 32 / bounceBpp;
        uint bounceMask = (1 << bounceBpp) - 1; // the bitmask for the bits per pixel (e.g. 5 = 31).
        
        uint index = pos_top_left.y * bounds.z + pos_top_left.x;
        float tl   = bounce_sample(index    , bouncePixels, bounceBpp, bounceMask);
        float tr   = bounce_sample(index + 1, bouncePixels, bounceBpp, bounceMask);
        
        index      = (pos_top_left.y + 1) * bounds.z + pos_top_left.x;
        float bl   = bounce_sample(index    , bouncePixels, bounceBpp, bounceMask);
        float br   = bounce_sample(index + 1, bouncePixels, bounceBpp, bounceMask);
        
        // bilinear interpolation (simd).
        float2 c = lerp(float2(tl, bl), float2(tr, br), f.x);
        float pixel = lerp(c.x, c.y, f.y);
        
        // restore linear color by squaring to recover detail in darker shades.
        return pixel * pixel;
    }
    
    // returns whether occlusion (1bpp shadow bitmask) data is available for this polygon.
    bool is_occlusion_available()
    {
        return activeLightShadowDataOffset > 0;
    }
    
    // returns whether bounce texture data is available for this polygon.
    bool is_bounce_available()
    {
        return activeLightBounceDataOffset > 0;
    }
#endif
};

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
float4 dynamic_lighting_unity_LightmapST;

// next we prepare macro statements that shaders use to implement their fragment functions.

#ifdef DYNAMIC_LIGHTING_DYNAMIC_GEOMETRY_DISTANCE_CUBES
    #define DYNLIT_FRAGMENT_FUNCTION \
    void dynlit_frag_light(v2f i, uint triangle_index:SV_PrimitiveID, int bvhLightIndex, inout DynamicLight light, inout DynamicTriangle dynamic_triangle, DYNLIT_FRAGMENT_LIGHT_OUT_PARAMETERS);\
    \
    fixed4 frag (v2f i, uint triangle_index:SV_PrimitiveID) : SV_Target
    
    #define DYNLIT_FRAG_LIGHT_CALL_BVH dynlit_frag_light(i, triangle_index, k, light, dynamic_triangle, DYNLIT_FRAGMENT_LIGHT_IN_PARAMETERS);
    #define DYNLIT_FRAG_LIGHT_CALL dynlit_frag_light(i, triangle_index, -1, light, dynamic_triangle, DYNLIT_FRAGMENT_LIGHT_IN_PARAMETERS);
#else
    #define DYNLIT_FRAGMENT_FUNCTION \
    void dynlit_frag_light(v2f i, uint triangle_index:SV_PrimitiveID, inout DynamicLight light, inout DynamicTriangle dynamic_triangle, DYNLIT_FRAGMENT_LIGHT_OUT_PARAMETERS);\
    \
    fixed4 frag (v2f i, uint triangle_index:SV_PrimitiveID) : SV_Target
    
    #define DYNLIT_FRAG_LIGHT_CALL_BVH dynlit_frag_light(i, triangle_index, light, dynamic_triangle, DYNLIT_FRAGMENT_LIGHT_IN_PARAMETERS);
    #define DYNLIT_FRAG_LIGHT_CALL dynlit_frag_light(i, triangle_index, light, dynamic_triangle, DYNLIT_FRAGMENT_LIGHT_IN_PARAMETERS);
#endif

#ifdef DYNAMIC_LIGHTING_DYNAMIC_GEOMETRY_DISABLED
    
    #define DYNLIT_FRAGMENT_INTERNAL \
    DynamicTriangle dynamic_triangle;\
    if (lightmap_resolution > 0)\
    {\
        /* use the dynamic triangles acceleration structure. */ \
        dynamic_triangle.load(triangle_index); \
        \
        /* iterate over every dynamic light affecting this triangle: */ \
        for (uint k = 0; k < dynamic_triangle.lightCount + realtime_lights_count; k++)\
        {\
            /* get the current light from memory. */ \
            dynamic_triangle.set_active_light_index(k);\
            DynamicLight light = dynamic_lights[dynamic_triangle.get_dynamic_light_index()];\
            \
            dynlit_frag_light(i, triangle_index, light, dynamic_triangle, DYNLIT_FRAGMENT_LIGHT_IN_PARAMETERS);\
        }\
    }
    
#else

#ifdef DYNAMIC_LIGHTING_BVH

    #define DYNLIT_FRAGMENT_INTERNAL \
    DynamicTriangle dynamic_triangle;\
    dynamic_triangle.initialize();\
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
                    DYNLIT_FRAG_LIGHT_CALL_BVH\
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
            DYNLIT_FRAG_LIGHT_CALL\
        }\
    }\
    else\
    {\
        /* use the dynamic triangles acceleration structure. */ \
        dynamic_triangle.load(triangle_index); \
        \
        /* iterate over every dynamic light affecting this triangle: */ \
        for (uint k = 0; k < dynamic_triangle.lightCount + realtime_lights_count; k++)\
        {\
            /* get the current light from memory. */ \
            dynamic_triangle.set_active_light_index(k);\
            DynamicLight light = dynamic_lights[dynamic_triangle.get_dynamic_light_index()];\
            \
            DYNLIT_FRAG_LIGHT_CALL\
        }\
    }

#else

    #define DYNLIT_FRAGMENT_INTERNAL \
    DynamicTriangle dynamic_triangle;\
    dynamic_triangle.initialize();\
    if (lightmap_resolution == 0)\
    {\
        /* iterate over every dynamic light in the scene (slow without bvh): */ \
        for (uint k = 0; k < dynamic_lights_count + realtime_lights_count; k++)\
        {\
            /* get the current light from memory. */ \
            DynamicLight light = dynamic_lights[k];\
            \
            DYNLIT_FRAG_LIGHT_CALL\
        }\
    }\
    else\
    {\
        /* use the dynamic triangles acceleration structure. */ \
        dynamic_triangle.load(triangle_index); \
        \
        /* iterate over every dynamic light affecting this triangle: */ \
        for (uint k = 0; k < dynamic_triangle.lightCount + realtime_lights_count; k++)\
        {\
            /* get the current light from memory. */ \
            dynamic_triangle.set_active_light_index(k);\
            DynamicLight light = dynamic_lights[dynamic_triangle.get_dynamic_light_index()];\
            \
            DYNLIT_FRAG_LIGHT_CALL\
        }\
    }

#endif

#endif

#ifdef DYNAMIC_LIGHTING_DYNAMIC_GEOMETRY_DISTANCE_CUBES
    #define DYNLIT_FRAGMENT_LIGHT void dynlit_frag_light(v2f i, uint triangle_index:SV_PrimitiveID, int bvhLightIndex, inout DynamicLight light, inout DynamicTriangle dynamic_triangle, DYNLIT_FRAGMENT_LIGHT_OUT_PARAMETERS)
#else
    #define DYNLIT_FRAGMENT_LIGHT void dynlit_frag_light(v2f i, uint triangle_index:SV_PrimitiveID, inout DynamicLight light, inout DynamicTriangle dynamic_triangle, DYNLIT_FRAGMENT_LIGHT_OUT_PARAMETERS)
#endif

#define DYNLIT_FRAGMENT_UNLIT \
fixed4 frag (v2f i) : SV_Target\
{\
    return tex2D(_MainTex, i.uv0);\
}