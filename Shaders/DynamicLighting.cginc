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

float point_in_box(float3 pos, float3 center, float3 boxsize, float epsilon = 0.00001)
{
    return (abs(pos.x - center.x) <= boxsize.x + epsilon && abs(pos.y - center.y) <= boxsize.y + epsilon && abs(pos.z - center.z) <= boxsize.z + epsilon);
}

float point_in_obb(float3 pos, float3 center, float3 boxsize, float3x3 rotation, float epsilon = 0.00001)
{
    pos = mul(pos, rotation);
    center = mul(center, rotation);
    return point_in_box(pos, center, boxsize, epsilon);
}

float point_in_sphere(float3 pos, float3 center, float radius, float epsilon = 0.00001)
{
    float3 dist = center - pos;
    dist = dot(dist, dist);
    return dist < (radius * radius) + epsilon;
}

float raycast_box(float3 origin, float3 target, float3 boxcenter, float3 boxsize)
{
    #define EPSILON 0.00001;
    
    float3 m = (origin + target) * 0.5; // Segment midpoint
    float3 d = target - m; // Segment halflength vector
    m = m - boxcenter; // Translate box and segment to origin
    // Try world coordinate axes as separating axes
    float adx = abs(d.x);
    if (abs(m.x) > boxsize.x + adx) return 0.0;
    float ady = abs(d.y);
    if (abs(m.y) > boxsize.y + ady) return 0.0;
    float adz = abs(d.z);
    if (abs(m.z) > boxsize.z + adz) return 0.0;
    // Add in an epsilon term to counteract arithmetic errors when segment is
    // (near) parallel to a coordinate axis (see text for detail)
    adx += EPSILON; ady += EPSILON; adz += EPSILON;
    // Try cross products of segment direction vector with coordinate axes
    if (abs(m.y * d.z - m.z * d.y) > boxsize.y * adz + boxsize.z * ady) return 0.0;
    if (abs(m.z * d.x - m.x * d.z) > boxsize.x * adz + boxsize.z * adx) return 0.0;
    if (abs(m.x * d.y - m.y * d.x) > boxsize.x * ady + boxsize.y * adx) return 0.0;
    // No separating axis found; segment must be overlapping AABB
    return 1.0;
    
    #undef EPSILON
}

float raycast_obb(float3 origin, float3 target, float3 boxcenter, float3 boxsize, float3x3 rotation)
{
    origin = mul(origin, rotation);
    target = mul(target, rotation);
    boxcenter = mul(boxcenter, rotation);
    
    return raycast_box(origin, target, boxcenter, boxsize);
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
    
    float3 d = normalize(target - origin);
    
    float3 m = origin - spherecenter; 
    float b = dot(m, d);
    float c = dot(m, m) - radius * radius;

    // exit if r�s origin outside s (c > 0) and r pointing away from s (b > 0).
    if (c > 0.0 && b > 0.0) return 0.0;
    float discr = b*b - c;

    // a negative discriminant corresponds to ray missing sphere.
    if (discr < 0.0) return 0.0;

    // ray now found to intersect sphere, compute smallest t value of intersection.
    float t = -b - sqrt(discr);

    // if t is negative, ray started inside sphere so clamp t to zero.
    if (t < 0.0) t = 0.0;
    float q = origin + t * d;
    
    // ensure that t does not exceed the ray origin and target.
    return t <= length(abs(target - origin));
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

struct DynamicShape
{
    float3   position;
    float3   size;
    float3x3 rotation;
    uint     type;
    
    bool is_box()
    {
        return type == 0;
    }
    
    bool is_sphere()
    {
        return type == 1;
    }
    
    bool is_cylinder()
    {
        return type == 2;
    }
    
    bool is_capsule()
    {
        return type == 3;
    }
    
    bool is_plane()
    {
        return type == 4;
    }
    
    bool contains_point(float3 pos)
    {
        if (is_box() && point_in_obb(pos, position, size, rotation))
            return true;
        else if (is_sphere() && point_in_sphere(pos, position, size.x))
            return true;
        return false;
    }
    
    bool raycast(float3 origin, float3 target)
    {
        if (is_box() && raycast_obb(origin, target, position, size, rotation))
            return true;
        else if (is_sphere() && raycast_sphere(origin, target, position, size.x))
            return true;
        else if (is_cylinder())
        {
            // ensure that t does not exceed the ray origin and target.
            float t = raycast_cylinder(origin, target, position, size.y, size.x, rotation).x;
            if(t > 0.0 && t <= length(abs(target - origin)))
                return true;
        }
        return false;
    }
};

StructuredBuffer<DynamicShape> dynamic_shapes;
uint dynamic_shapes_count;