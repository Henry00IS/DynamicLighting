// GENERATE_FUNCTION_NAME: The name of the function that will be generated.
// GENERATE_FUNCTION_CALL: The function to call to get samples to be filtered.

// shoutouts to https://chat.openai.com/ for actually figuring out bilinear filtering in 3 dimensions.
float GENERATE_FUNCTION_NAME(float3 world)
{
    world *= shimmerScale;

    // calculate the weights for the trilinear interpolation.
    float3 weight = frac(world);

    // calculate the integer part of the texture coordinates.
    world = floor(world);

    // sample the texture at the neighboring cells.
    float topLeftFront = GENERATE_FUNCTION_CALL(world);
    float topRightFront = GENERATE_FUNCTION_CALL(world + float3(1, 0, 0));
    float bottomLeftFront = GENERATE_FUNCTION_CALL(world + float3(0, 1, 0));
    float bottomRightFront = GENERATE_FUNCTION_CALL(world + float3(1, 1, 0));
    float topLeftBack = GENERATE_FUNCTION_CALL(world + float3(0, 0, 1));
    float topRightBack = GENERATE_FUNCTION_CALL(world + float3(1, 0, 1));
    float bottomLeftBack = GENERATE_FUNCTION_CALL(world + float3(0, 1, 1));
    float bottomRightBack = GENERATE_FUNCTION_CALL(world + float3(1, 1, 1));

    // perform bilinear interpolation in the z direction.
    float4 dz = lerp(float4(topLeftFront, topRightFront, bottomLeftFront, bottomRightFront),
                     float4(topLeftBack , topRightBack , bottomLeftBack , bottomRightBack),
                     weight.z);
    
    // perform bilinear interpolation in the y direction.
    float2 dy = lerp(dz.xy, dz.zw, weight.y);

    // perform bilinear interpolation in the x direction.
    return lerp(dy.x, dy.y, weight.x);
}

#undef GENERATE_FUNCTION_NAME
#undef GENERATE_FUNCTION_CALL