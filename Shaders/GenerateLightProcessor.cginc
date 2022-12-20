// GENERATE_NORMAL: The variable containing the fragment normal.

// calculate the unnormalized direction between the light source and the fragment.
float3 light_direction = light.position - i.world;

// calculate the square distance between the light source and the fragment.
// distance(i.world, light.position); but squared to prevent a square root.
// confirmed with NVIDIA Quadro K1000M slightly improving the framerate.
float light_distanceSqr = dot(light_direction, light_direction);

// we can use the distance and guaranteed maximum light radius to early out.
// confirmed with NVIDIA Quadro K1000M doubling the framerate.
if (light_distanceSqr > light.radiusSqr) continue;

// properly normalize the direction between the light source and the fragment.
light_direction = normalize(light_direction);

// a simple dot product with the normal gives us diffusion.
float NdotL = max(dot(GENERATE_NORMAL, light_direction), 0);

// this also tells us whether the fragment is facing away from the light.
// as the fragment will then be black we can early out here.
// confirmed with NVIDIA Quadro K1000M improving the framerate.
if (NdotL == 0.0) continue;

// if this renderer has a lightmap we use shadow bits otherwise it's a dynamic object.
// if this light is realtime we will skip this step.
float map = 1.0;
if (lightmap_resolution > 0 && light_is_dynamic(light))
{
    uint shadow_channel = light_get_shadow_channel(light);

    // retrieve the shadow bit at this position with bilinear filtering.
    map = lightmap_sample_bilinear(i.uv1, shadow_channel);

    // whenever the fragment is fully in shadow we can early out.
    // confirmed with NVIDIA Quadro K1000M improving the framerate.
    if (map == 0.0) continue;
}

// spot lights determine whether we are in the light cone or outside.
if (light_is_spotlight(light))
{
    // anything outside of the spot light can and must be skipped.
    float2 spotlight = light_calculate_spotlight(light, light_direction);
    if (spotlight.x <= light_outerCutoff)
        continue;
    map *= spotlight.y;
}
else if (light_is_discoball(light))
{
    // anything outside of the spot lights can and must be skipped.
    float2 spotlight = light_calculate_discoball(light, light_direction);
    if (spotlight.x <= light_outerCutoff)
        continue;
    map *= spotlight.y;
}
else if (light_is_wave(light))
{
    map *= light_calculate_wave(light, i.world);
}

if (light_is_watershimmer(light))
{
    map *= light_calculate_watershimmer_bilinear(light, i.world);
}
else if (light_is_randomshimmer(light))
{
    map *= light_calculate_randomshimmer_bilinear(light, i.world);
}

// important attenuation that actually creates the point light with maximum radius.
float attenuation = saturate(1.0 - light_distanceSqr / light.radiusSqr) * light.intensity;

#undef GENERATE_NORMAL