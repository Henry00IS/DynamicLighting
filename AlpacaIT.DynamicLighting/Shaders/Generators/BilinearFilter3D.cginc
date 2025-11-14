// GENERATE_FUNCTION_NAME: The name of the function that will be generated.
// GENERATE_FUNCTION_CALL: The function to call to get samples to be filtered.

// shoutouts to https://chat.openai.com/ for actually figuring out bilinear filtering in 3 dimensions.
float GENERATE_FUNCTION_NAME(float3 world)
{
    world *= shimmerScale;

    // calculate the weights for the bilinear interpolation.
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

#undef GENERATE_FUNCTION_NAME
#undef GENERATE_FUNCTION_CALL