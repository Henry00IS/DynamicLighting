// GENERATE_NORMAL: The variable containing the fragment normal.

// calculate the unnormalized direction between the light source and the fragment.
float3 light_direction = light.position - i.world;

// calculate the square distance between the light source and the fragment.
// distance(i.world, light.position); but squared to prevent a square root.
// confirmed with NVIDIA Quadro K1000M slightly improving the framerate.
float light_distanceSqr = dot(light_direction, light_direction);

// we can use the distance and guaranteed maximum light radius to early out.
// confirmed with NVIDIA Quadro K1000M doubling the framerate.
if (light_distanceSqr > light.radiusSqr) return;

// many effects require the (light.position - i.world) that we already calculated.
float3 light_position_minus_world = light_direction;

// properly normalize the direction between the light source and the fragment.
light_direction = normalize(light_direction);

// depending on the culling mode we flip the normal if we are rendering backsides.
#if defined(DYNAMIC_LIGHTING_CULL_OFF)
    float NdotL = dot(is_front_face ? GENERATE_NORMAL : -GENERATE_NORMAL, light_direction);
#elif defined(DYNAMIC_LIGHTING_CULL_FRONT)
    float NdotL = dot(-GENERATE_NORMAL, light_direction);
#else
    float NdotL = dot(GENERATE_NORMAL, light_direction);
#endif

#ifndef DYNAMIC_LIGHTING_DYNAMIC_GEOMETRY_DISABLED
if (lightmap_resolution > 0)
{
    // static geometry:
    // a simple dot product with the normal gives us diffusion.
    NdotL = max(NdotL, 0);
}
else
{
    // dynamic geometry:
    // pull the dot product all the way around for half lambert diffusion.
    NdotL = pow(0.5 + NdotL * 0.5, 2.0);
}
#else
    // static geometry:
    // a simple dot product with the normal gives us diffusion.
    NdotL = max(NdotL, 0);
#endif

#if defined(DYNAMIC_LIGHTING_BOUNCE) && !defined(DYNAMIC_LIGHTING_INTEGRATED_GRAPHICS)
// check whether bounce texture data is available on this triangle.
bool is_bounce_available = dynamic_triangle.is_bounce_available();
#endif

// this also tells us whether the fragment is facing away from the light.
// as the fragment will then be black we can early out here.
// confirmed with NVIDIA Quadro K1000M improving the framerate.
#if defined(DYNAMIC_LIGHTING_BOUNCE) && !defined(DYNAMIC_LIGHTING_INTEGRATED_GRAPHICS)
if (!is_bounce_available && NdotL == 0.0) return;
#else
if (NdotL == 0.0) return;
#endif

// if this renderer has a lightmap we use shadow bits otherwise it's a dynamic object.
// if this light is realtime we will skip this step.
float map = 1.0;
#if defined(DYNAMIC_LIGHTING_BOUNCE) && !defined(DYNAMIC_LIGHTING_INTEGRATED_GRAPHICS)
float bounce = 0.0;
#endif
#ifndef DYNAMIC_LIGHTING_DYNAMIC_GEOMETRY_DISABLED
if (lightmap_resolution > 0 && light.is_dynamic())
#else
if (light.is_dynamic())
#endif
{
#if defined(DYNAMIC_LIGHTING_INTEGRATED_GRAPHICS)
    // retrieve the shadow bit at this position with basic sampling.
    map = dynamic_triangle.shadow_sample_integrated(i.uv1);
#elif defined(DYNAMIC_LIGHTING_QUALITY_LOW)
    // retrieve the shadow bit at this position with 3x3 average sampling.
    map = dynamic_triangle.shadow_sample3x3(i.uv1);
#elif defined(DYNAMIC_LIGHTING_QUALITY_MEDIUM) || defined(DYNAMIC_LIGHTING_QUALITY_HIGH)
    // retrieve the shadow bit at this position with bilinear filtering.
    map = dynamic_triangle.shadow_sample_bilinear(i.uv1);
#endif

#if defined(DYNAMIC_LIGHTING_BOUNCE) && !defined(DYNAMIC_LIGHTING_INTEGRATED_GRAPHICS)
    // retrieve the bounce lighting sample.
    if (is_bounce_available)
    {
        // bounce is almost never 0.0 and checking for it to early out is more expensive.
        // confirmed with NVIDIA Quadro K1000M.
        bounce = dynamic_triangle.bounce_sample_bilinear(light, i.uv1);
    }
    else
    {
        // whenever the fragment is fully in shadow we can skip work.
        // confirmed with NVIDIA Quadro K1000M improving the framerate.
        if (map == 0.0) return;
    }
#else
    // whenever the fragment is fully in shadow we can skip work.
    // confirmed with NVIDIA Quadro K1000M improving the framerate.
    if (map == 0.0) return;
#endif
}
#ifdef DYNAMIC_LIGHTING_DYNAMIC_GEOMETRY_DISTANCE_CUBES
// dynamic geometry drawn using the bounding volume hierarchy sample distance cubemaps.
else if (bvhLightIndex != -1)
{
    map = sample_distance_cube_trilinear(bvhLightIndex, i.world, light.position);
}
#endif

#ifndef DYNAMIC_LIGHTING_INTEGRATED_GRAPHICS
// when the light has a shadow cubemap we sample that for real-time shadows.
if (light.is_shadow_available())
{
    float fragDepth = sqrt(light_distanceSqr / light.radiusSqr);
    float2 moments = shadow_cubemaps.SampleLevel(sampler_shadow_cubemaps, float4(light_direction, light.shadowCubemapIndex), 0);
    float E_x2 = moments.y;
    float Ex_2 = moments.x * moments.x;
    float variance = E_x2 - Ex_2;
    float mD = moments.x - fragDepth;
    float mD_2 = mD * mD;
    float p = variance / (variance + mD_2);
    float res = map = max(p, fragDepth - (0.0075 * fragDepth) <= moments.x);
    map = ReduceLightBleeding(res, max(max(0.3, 1.0 - NdotL), fragDepth));
}
#endif

#if defined(DYNAMIC_LIGHTING_BOUNCE) && !defined(DYNAMIC_LIGHTING_INTEGRATED_GRAPHICS)
// whenever the fragment is fully in shadow we can skip work.
// confirmed with NVIDIA Quadro K1000M improving the framerate.
if (map != 0.0 || bounce != 0.0)
{
#endif
    uint light_type = light.get_type();
    if (light_type == light_type_point)
    {
        // early out for this default type, big speed improvement.
    }
    else
    // spot lights determine whether we are in the light cone or outside.
    if (light_type == light_type_spot)
    {
#if !defined(DYNAMIC_LIGHTING_BOUNCE) || defined(DYNAMIC_LIGHTING_INTEGRATED_GRAPHICS)
        // anything outside of the spot light can be skipped.
        float2 spotlight = light.calculate_spotlight(light_direction);
        if (spotlight.x <= light.light_outerCutoff)
            return;
        map *= spotlight.y;
#else
        float3 spotlight = light.calculate_spotlight_bounce(light_direction);
        map *= spotlight.y;
        bounce *= spotlight.z;
#endif
        // when the light has a cookie texture we sample that.
        if (light.is_cookie_available())
        {
            float3x3 rot = look_at_matrix(light.forward, light.up);
            float2 world_minus_light_position = mul(light_direction, rot).xy;
            map *= light_cookies.SampleLevel(sampler_light_cookies, float3(0.5 - light.gpFloat3 * world_minus_light_position * (1.0 / spotlight.x), light.cookieIndex), 0);
        }
    }
    else if (light_type == light_type_discoball)
    {
#if !defined(DYNAMIC_LIGHTING_BOUNCE) || defined(DYNAMIC_LIGHTING_INTEGRATED_GRAPHICS)
        // anything outside of the spot lights can be skipped.
        float2 spotlight = light.calculate_discoball(light_direction);
        if (spotlight.x <= light.light_outerCutoff)
            return;
        map *= spotlight.y;
#else
        float2 spotlight = light.calculate_discoball_bounce(light_direction);
        map *= spotlight.x;
        bounce *= spotlight.y;
#endif
    }
#if defined(DYNAMIC_LIGHTING_BOUNCE) && !defined(DYNAMIC_LIGHTING_INTEGRATED_GRAPHICS)
}
if (map != 0.0)
{
    uint light_type = light.get_type();
#else
    else
#endif
    if (light_type == light_type_wave)
    {
        map *= light.calculate_wave(i.world);
    }
    else if (light_type == light_type_interference)
    {
        map *= light.calculate_interference(light_position_minus_world);
    }
    else if (light_type == light_type_rotor)
    {
        map *= light.calculate_rotor(light_position_minus_world);
    }
    else if (light_type == light_type_shock)
    {
        map *= light.calculate_shock(i.world);
    }
    else if (light_type == light_type_disco)
    {
        map *= light.calculate_disco(light_position_minus_world);
    }

    uint light_shimmer = light.get_shimmer();
    if (light_shimmer == light_shimmer_none)
    {
        // early out for this default type, big speed improvement.
    }
    else if (light_shimmer == light_shimmer_water)
    {
        map *= light.calculate_watershimmer_trilinear(i.world);
    }
    else if (light_shimmer == light_shimmer_random)
    {
        map *= light.calculate_randomshimmer_trilinear(i.world);
    }
#if defined(DYNAMIC_LIGHTING_BOUNCE) && !defined(DYNAMIC_LIGHTING_INTEGRATED_GRAPHICS)
}
#endif

// important attenuation that actually creates the point light with maximum radius.
float attenuation = light.calculate_attenuation(light_distanceSqr);

#undef GENERATE_NORMAL