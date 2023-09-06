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
    float modifier = shimmerModifier;
    float topLeftFront = GENERATE_FUNCTION_CALL(world, modifier);
    float topRightFront = GENERATE_FUNCTION_CALL(world + float3(1, 0, 0), modifier);
    float bottomLeftFront = GENERATE_FUNCTION_CALL(world + float3(0, 1, 0), modifier);
    float bottomRightFront = GENERATE_FUNCTION_CALL(world + float3(1, 1, 0), modifier);
    float topLeftBack = GENERATE_FUNCTION_CALL(world + float3(0, 0, 1), modifier);
    float topRightBack = GENERATE_FUNCTION_CALL(world + float3(1, 0, 1), modifier);
    float bottomLeftBack = GENERATE_FUNCTION_CALL(world + float3(0, 1, 1), modifier);
    float bottomRightBack = GENERATE_FUNCTION_CALL(world + float3(1, 1, 1), modifier);

    // perform bilinear interpolation in the x direction.
    float topFront = lerp(topLeftFront, topRightFront, weight.x);
    float bottomFront = lerp(bottomLeftFront, bottomRightFront, weight.x);
    float topBack = lerp(topLeftBack, topRightBack, weight.x);
    float bottomBack = lerp(bottomLeftBack, bottomRightBack, weight.x);

    // perform bilinear interpolation in the y direction.
    float front = lerp(topFront, bottomFront, weight.y);
    float back = lerp(topBack, bottomBack, weight.y);

    // perform bilinear interpolation in the z direction.
    float result = lerp(front, back, weight.z);

    // return the final interpolated value.
    return result;
}

#undef GENERATE_FUNCTION_NAME
#undef GENERATE_FUNCTION_CALL