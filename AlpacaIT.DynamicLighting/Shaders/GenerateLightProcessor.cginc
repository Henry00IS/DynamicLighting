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

// a simple dot product with the normal gives us diffusion.
float NdotL = max(dot(GENERATE_NORMAL, light_direction), 0);

// this also tells us whether the fragment is facing away from the light.
// as the fragment will then be black we can early out here.
// confirmed with NVIDIA Quadro K1000M improving the framerate.
if (NdotL == 0.0) return;

// when the light has a shadow cubemap we sample that for real-time shadows.
if (light.is_shadow_available())
{
    // magic bias function! it is amazing!
    float light_distance = sqrt(light_distanceSqr);
    float magic = 0.02 + 0.01 * (light_distanceSqr / light_distance);
    float autobias = magic * tan(acos(1.0 - NdotL));
    autobias = clamp(autobias, 0.0, magic);
    
    float shadow_mapping_distance = shadow_cubemaps.SampleLevel(sampler_shadow_cubemaps, float4(light_direction, light.shadowCubemapIndex), 0);
    
    // when the fragment is occluded we can early out here.
    if (light_distance - autobias > shadow_mapping_distance)
        return;
}

// if this renderer has a lightmap we use shadow bits otherwise it's a dynamic object.
// if this light is realtime we will skip this step.
float map = 1.0;
if (lightmap_resolution > 0 && light.is_dynamic())
{
    uint shadow_channel = light.get_shadow_channel();

#if DYNAMIC_LIGHTING_SHADOW_SOFT
    // retrieve the shadow bit at this position with bilinear filtering.
    map = lightmap_sample_bilinear(i.uv1, shadow_channel);
#else
    // retrieve the shadow bit at this position with 3x3 average sampling.
    map = lightmap_sample3x3(i.uv1, shadow_channel);
#endif

    // whenever the fragment is fully in shadow we can early out.
    // confirmed with NVIDIA Quadro K1000M improving the framerate.
    if (map == 0.0) return;
}

// spot lights determine whether we are in the light cone or outside.
if (light.is_spotlight())
{
    // anything outside of the spot light can and must be skipped.
    float2 spotlight = light.calculate_spotlight(light_direction);
    if (spotlight.x <= light.light_outerCutoff || spotlight.x == 0.0) // prevent division by zero in light cookies.
        return;
    map *= spotlight.y;
    
    // when the light has a cookie texture we sample that.
    if (light.is_cookie_available() && light.light_outerCutoff > 0.0)
    {
        float3x3 rot = look_at_matrix(-light.forward, light.up);
        float2 world_minus_light_position = mul(light_direction, rot).xy;
        map *= light_cookies.SampleLevel(sampler_light_cookies, float3(0.5 - light.gpFloat3 * world_minus_light_position * (1.0 / spotlight.x), light.cookieIndex), 0);
    }
}
else if (light.is_discoball())
{
    // anything outside of the spot lights can and must be skipped.
    float2 spotlight = light.calculate_discoball(light_direction);
    if (spotlight.x <= light.light_outerCutoff)
        return;
    map *= spotlight.y;
}
else if (light.is_wave())
{
    map *= light.calculate_wave(i.world);
}
else if (light.is_interference())
{
    map *= light.calculate_interference(light_position_minus_world);
}
else if (light.is_rotor())
{
    map *= light.calculate_rotor(light_position_minus_world);
}
else if (light.is_shock())
{
    map *= light.calculate_shock(i.world);
}
else if (light.is_disco())
{
    map *= light.calculate_disco(light_position_minus_world);
}

if (light.is_watershimmer())
{
    map *= light.calculate_watershimmer_bilinear(i.world);
}
else if (light.is_randomshimmer())
{
    map *= light.calculate_randomshimmer_bilinear(i.world);
}

// important attenuation that actually creates the point light with maximum radius.
float attenuation = pow(saturate(1.0 - light_distanceSqr / light.radiusSqr), 2.0) * light.intensity;

#undef GENERATE_NORMAL