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
    float calculate_interference(float3 world, float3 light_position_minus_world)
    {
        float3x3 rot = look_at_matrix(forward, up);
        world = mul(light_position_minus_world, rot);

        float angle = atan2(sqrt((world.x * world.x) + (world.z * world.z)), world.y) * UNITY_PI * light_waveFrequency;
        float scale = 0.5 + 0.5 * cos(angle - _Time.y * light_waveSpeed * UNITY_PI * 2.0);
        return scale;
    }
    
    // calculates the rotor effect.
    float calculate_rotor(float3 world, float3 light_position_minus_world)
    {
        float signRotorCenter = sign(light_rotorCenter);

        float3x3 rot = look_at_matrix(forward, up);
        world = mul(light_position_minus_world, rot);

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
        float dist2 = sqrt(radiusSqr) * abs(light_rotorCenter);
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
    float calculate_shock(float3 world)
    {
	    float dist = light_waveFrequency * distance(position, world);
	    float brightness = 0.9 + 0.1 * sin((dist * 2.0 - _Time.y * light_waveSpeed) * UNITY_PI * 2.0);
	    brightness      *= 0.9 + 0.1 * cos((dist + _Time.y * light_waveSpeed) * UNITY_PI * 2.0);
	    brightness      *= 0.9 + 0.1 * sin((dist / 2.0 - _Time.y * light_waveSpeed) * UNITY_PI * 2.0);
        return brightness;
    }
    
    // calculates the disco effect.
    float calculate_disco(float3 world, float3 light_position_minus_world)
    {
        float3x3 rot = look_at_matrix(forward, up);
        world = mul(light_position_minus_world, rot);

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

// axis aligned box centered at 0,0,0 with given size.
bool raycast_box(float3 origin, float3 size, float light_distanceSqr, float3 light_direction)
{
    float3 m = 1.0/light_direction;
    float3 n = m*origin;
    float3 k = abs(m)*size;
    float3 t1 = -n - k;
    float3 t2 = -n + k;
    float tN = max( max( t1.x, t1.y ), t1.z );
    float tF = min( min( t2.x, t2.y ), t2.z );
    if(tN>tF || tF<0.0)
        return false; // no intersection
    return light_distanceSqr >= tN * tN;
}

bool raycast_obb(float3 origin, float3 boxcenter, float3 boxsize, float3x3 rotation, float light_distanceSqr, float3 light_direction)
{
    origin = mul(origin - boxcenter, rotation);
    light_direction = mul(light_direction, rotation);
    
    return raycast_box(origin, boxsize, light_distanceSqr, light_direction);
}

float point_in_box(float3 pos, float3 center, float3 boxsize, float epsilon = 0.00001)
{
    return (abs(pos.x - center.x) <= boxsize.x + epsilon && abs(pos.y - center.y) <= boxsize.y + epsilon && abs(pos.z - center.z) <= boxsize.z + epsilon);
}

float point_in_obb(float3 pos, float3 center, float3 boxsize, float3x3 rotation, float epsilon = 0.00001)
{
    pos = abs(mul(pos - center, rotation));
    return (pos.x <= boxsize.x + epsilon && pos.y <= boxsize.y + epsilon && pos.z <= boxsize.z + epsilon);
}

float point_in_sphere(float3 pos, float3 center, float radius, float epsilon = 0.00001)
{
    float3 dist = center - pos;
    dist = dot(dist, dist);
    return dist < (radius * radius) + epsilon;
}

// special thanks to https://iquilezles.org/articles/intersectors/
bool raycast_sphere(float3 origin, float3 center, float radius, float light_distanceSqr, float3 light_direction)
{
    float3 oc = origin - center;
    float b = dot( oc, light_direction );
    float c = dot( oc, oc ) - radius*radius;
    float h = b*b - c;
    if(h<0.0)
        return false; // no intersection
    h = sqrt(h);
    float tN = -b-h;
    return tN > 0.0 && light_distanceSqr >= tN * tN;
}

// special thanks to https://iquilezles.org/articles/intersectors/
float4 raycast_cylinder(float3 origin, float3 target, float3 center, float height, float radius, float3x3 rotation)
{
    origin = mul(origin, rotation);
    target = mul(target, rotation);
    center = mul(center, rotation);
    
    float3 p1 = center - float3(0.0, height, 0.0);
    float3 p2 = center + float3(0.0, height, 0.0);
    
    float3 rd = normalize(target - origin);
    
    float3  ba = p2 - p1;
    float3  oc = origin - p1;
    float baba = dot(ba,ba);
    float bard = dot(ba,rd);
    float baoc = dot(ba,oc);
    float k2 = baba            - bard*bard;
    float k1 = baba*dot(oc,rd) - baoc*bard;
    float k0 = baba*dot(oc,oc) - baoc*baoc - radius*radius*baba;
    float h = k1*k1 - k2*k0;
    if( h<0.0 ) return float4(-1.0, -1.0, -1.0, -1.0);//no intersection
    h = sqrt(h);
    float t = (-k1-h)/k2;
    // body
    float y = baoc + t*bard;
    if( y>0.0 && y<baba ) return float4( t, (oc+t*rd - ba*y/baba)/radius );
    // caps
    t = ( ((y<0.0) ? 0.0 : baba) - baoc)/bard;
    if( abs(k1+k2*t)<h )
    {
        return float4( t, ba*sign(y)/sqrt(baba) );
    }
    return float4(-1.0, -1.0, -1.0, -1.0);//no intersection
}

// special thanks to https://iquilezles.org/articles/intersectors/
float raycast_capsule(float3 origin, float3 target, float3 center, float height, float radius, float3x3 rotation)
{
    origin = mul(origin, rotation);
    target = mul(target, rotation);
    center = mul(center, rotation);
    
    float3 p1 = center - float3(0.0, height, 0.0);
    float3 p2 = center + float3(0.0, height, 0.0);
    
    float3 rd = normalize(target - origin);
    
    float3  ba = p2 - p1;
    float3  oa = origin - p1;
    float baba = dot(ba,ba);
    float bard = dot(ba,rd);
    float baoa = dot(ba,oa);
    float rdoa = dot(rd,oa);
    float oaoa = dot(oa,oa);
    float a = baba      - bard*bard;
    float b = baba*rdoa - baoa*bard;
    float c = baba*oaoa - baoa*baoa - radius*radius*baba;
    float h = b*b - a*c;
    if( h >= 0.0 )
    {
        float t = (-b-sqrt(h))/a;
        float y = baoa + t*bard;
        // body
        if( y>0.0 && y<baba ) return t;
        // caps
        float3 oc = (y <= 0.0) ? oa : origin - p2;
        b = dot(rd,oc);
        c = dot(oc,oc) - radius*radius;
        h = b*b - c;
        if( h>0.0 ) return -b - sqrt(h);
    }
    return -1.0;
}

struct DynamicShape
{
    float3   position;
    uint     flags;
    // -- 16 byte boundary --
    float3   size;
    float3x3 rotation;
    
    bool is_box()
    {
        return flags & 1;
    }
    
    bool is_sphere()
    {
        return flags & 2;
    }
    
    bool is_cylinder()
    {
        return flags & 4;
    }
    
    bool is_capsule()
    {
        return flags & 8;
    }
    
    bool is_skipping_inner_self_shadows()
    {
        return flags & 16;
    }
    
    bool contains_point(float3 pos)
    {
        if (is_box())
        {
            return point_in_obb(pos, position, size, rotation);    
        }
        else if (is_sphere())
        {
            return point_in_sphere(pos, position, size.x);
        }
        return false;
    }
    
    bool raycast(float3 origin, float3 target, float light_distanceSqr, float3 light_direction)
    {
        if (is_box())
        {
            return raycast_obb(origin, position, size, rotation, light_distanceSqr, light_direction);
        }
        else if (is_sphere())
        {
            return raycast_sphere(origin, position, size.x, light_distanceSqr, light_direction);
        }
        else if (is_cylinder())
        {
            // ensure that t does not exceed the ray origin and target.
            float t = raycast_cylinder(origin, target, position, size.y, size.x, rotation).x;
            if(t > 0.0 && t <= length(abs(target - origin)))
                return true;
        }
        else if (is_capsule())
        {
            // ensure that t does not exceed the ray origin and target.
            float t = raycast_capsule(origin, target, position, size.y * 0.5, size.x, rotation).x;
            if(t > 0.0 && t <= length(abs(target - origin)))
                return true;
        }
        return false;
    }
};

StructuredBuffer<DynamicShape> dynamic_shapes;
uint dynamic_shapes_count;