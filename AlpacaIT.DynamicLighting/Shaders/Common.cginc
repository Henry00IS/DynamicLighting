// calculates a look-at orientation matrix.
float3x3 look_at_matrix(float3 forward, float3 up)
{
    float3 right = normalize(cross(forward, up));
    return float3x3(
        right.x, up.x, forward.x,
        right.y, up.y, forward.y,
        right.z, up.z, forward.z
    );
}

// returns the largest component of a float3.
float max3(float3 v)
{
    return max(max(v.x, v.y), v.z);
}

// https://forum.unity.com/threads/snap-round-3d-direction-vector-based-on-angle.905168/
// snaps the input direction to 45 degree increments.
// shoutouts to JoNax97 and Cannist!
float snap_direction_round(float f)
{
    if (abs(f) < tan(UNITY_PI / 8.0))
        return 0.0;
    return sign(f);
}
float3 snap_direction(float3 input)
{
    // scale vector to unit cube.
    float scaleDivisor = max3(abs(input));
    input /= scaleDivisor;

    float3 rounded = float3(snap_direction_round(input.x), snap_direction_round(input.y), snap_direction_round(input.z));
    return normalize(rounded);
}

// shoutouts to anastadunbar https://www.shadertoy.com/view/Xt23Ry
float rand(float co) { return frac(sin(co * (91.3458)) * 47453.5453); }
float rand(float2 co) { return frac(sin(dot(co.xy, float2(12.9898, 78.233))) * 43758.5453); }
float rand(float3 co) { return rand(co.xy + rand(co.z)); }

bool point_in_sphere(float3 pos, float3 center, float radius, float epsilon = 0.00001)
{
    float3 dist = center - pos;
    dist = dot(dist, dist);
    return dist < (radius * radius) + epsilon;
}

bool point_in_aabb(float3 pos, float3 min, float3 max)
{
    return all(pos >= min && pos <= max);
}

bool ray_box_intersection(float3 rayOrigin, float3 rayDir, float3 boxMin, float3 boxMax, out float tMin, out float tMax, float maxDepth)
{
    float3 invDir = 1.0 / rayDir;
    float3 t0 = (boxMin - rayOrigin) * invDir;
    float3 t1 = (boxMax - rayOrigin) * invDir;
    float3 tMinVec = min(t0, t1);
    float3 tMaxVec = max(t0, t1);

    tMin = max(0.0, max(tMinVec.x, max(tMinVec.y, tMinVec.z)));
    tMax = min(tMaxVec.x, min(tMaxVec.y, tMaxVec.z));

    // ensure that the intersection does not extend beyond the max depth.
    tMax = min(tMax, maxDepth);

    return tMax >= tMin;
}

bool ray_cone_intersection(float3 rayOrigin, float3 rayDir, float3 coneTip, float3 coneDir, float coneAngle, float coneDistance, out float tMin, out float tMax, float maxDepth)
{
    // Normalize directions
    float3 d = normalize(rayDir);
    float3 s = normalize(coneDir);
    float3 w = rayOrigin - coneTip;

    float cosTheta = cos(coneAngle);
    float m = cosTheta * cosTheta;

    float d_dot_s = dot(d, s);
    float w_dot_s = dot(w, s);
    float d_dot_w = dot(d, w);
    float w_dot_w = dot(w, w);

    float A = d_dot_s * d_dot_s - m;
    float B = 2.0 * (w_dot_s * d_dot_s - m * d_dot_w);
    float C = w_dot_s * w_dot_s - m * w_dot_w;

    float discriminant = B * B - 4.0 * A * C;

    tMin = 0.0;
    tMax = 0.0;

    int numHits = 0;
    float tValues[2];

    // Step 1: Check if the ray origin is inside the cone
    // To do this, we check the distance from the ray origin to the cone axis and compare it to the radius of the cone at the point where rayOrigin projects onto the cone's axis.
    float coneRadiusAtOrigin = tan(coneAngle) * w_dot_s;
    float distanceFromAxisSquared = dot(w, w) - w_dot_s * w_dot_s;

    bool isInsideCone = (w_dot_s >= 0.0 && w_dot_s <= coneDistance) && (distanceFromAxisSquared <= coneRadiusAtOrigin * coneRadiusAtOrigin);

    // Step 2: If the ray is inside the cone, set tMin = 0.0, but still compute tMax
    if (isInsideCone)
    {
        tMin = 0.0; // The ray starts inside the cone, so we consider the ray "already intersecting".
    }

    // Step 3: Compute potential intersections with the cone's body
    if (discriminant >= 0.0)
    {
        float sqrtDiscriminant = sqrt(discriminant);
        float inv2A = 0.5 / A; // Avoid division by zero if A is zero

        float t0 = (-B - sqrtDiscriminant) * inv2A;
        float t1 = (-B + sqrtDiscriminant) * inv2A;

        // Check t0
        if (t0 >= 0.0)
        {
            float3 hitPoint0 = rayOrigin + t0 * d;
            float h0 = dot(hitPoint0 - coneTip, s);

            if (h0 >= 0.0 && h0 <= coneDistance)
            {
                tValues[numHits++] = t0;
            }
        }

        // Check t1
        if (t1 >= 0.0)
        {
            float3 hitPoint1 = rayOrigin + t1 * d;
            float h1 = dot(hitPoint1 - coneTip, s);

            if (h1 >= 0.0 && h1 <= coneDistance)
            {
                tValues[numHits++] = t1;
            }
        }
    }

    // Step 4: Add ray-plane intersection test for the cone cap
    float3 coneCapCenter = coneTip + coneDir * coneDistance; // Cap is at coneDistance from tip along the direction
    float capRadius = coneDistance * tan(coneAngle);         // Radius of the cap is determined by cone angle and distance

    float denom = dot(d, s); // The denominator in ray-plane intersection
    if (abs(denom) > 0.0001) // Ensure the ray isn't parallel to the cap's plane
    {
        float tCap = dot(coneCapCenter - rayOrigin, s) / denom;
        if (tCap >= 0.0) // Only consider hits in front of the ray
        {
            float3 hitPointCap = rayOrigin + tCap * d;
            float3 capToHit = hitPointCap - coneCapCenter;
            if (dot(capToHit, capToHit) <= capRadius * capRadius)
            {
                tValues[numHits++] = tCap; // Valid hit on the cap
            }
        }
    }

    // Step 5: If there are no hits, return false
    if (numHits == 0)
    {
        return isInsideCone; // If ray started inside the cone, we consider it intersecting, otherwise return false.
    }

    // Step 6: Sort tMin and tMax based on intersection points
    if (numHits == 1)
    {
        if (isInsideCone)
        {
            tMax = tValues[0]; // The ray started inside, so tMin is 0, and tMax is the first valid intersection
        }
        else
        {
            tMin = tMax = tValues[0]; // Single intersection case
        }
        
        // ensure that the intersection does not extend beyond the max depth.
        tMax = min(tMax, maxDepth);
        
        return true;
    }
    else if (numHits == 2)
    {
        tMin = min(tValues[0], tValues[1]);
        tMax = max(tValues[0], tValues[1]);
        
        // If the ray starts inside, override tMin to be 0
        if (isInsideCone)
        {
            tMin = 0.0;
        }
        
        // ensure that the intersection does not extend beyond the max depth.
        tMax = min(tMax, maxDepth);
        
        return true;
    }

    return false;
}

// shoutouts to lordofduct (https://forum.unity.com/threads/how-do-i-find-the-closest-point-on-a-line.340058/)
float3 nearest_point_on_finite_line(float3 start, float3 end, float3 pnt)
{
    float3 linex = end - start;
    float len = length(linex);
    linex = normalize(linex);
    float3 v = pnt - start;
    float d = dot(v, linex);
    d = clamp(d, 0.0, len);
    return start + linex * d;
}

// looks at each channel's color information and multiplies the inverse of the blend and
// base colors. the result color is always a lighter color. screening with black leaves the
// color unchanged. screening with white produces white. the effect is similar to projecting
// multiple photographic slides on top of each other.
float4 color_screen(float4 self, float4 blend)
{
    return 1.0 - (1.0 - self) * (1.0 - blend);
}

#if DYNAMIC_LIGHTING_BOUNCE
float4 unpack_saturated_float4_from_uint(uint bytes)
{
    // extract the bytes and convert them to float [0.0, 255.0].
    float4 result = float4(
        (bytes >> 24) & 0xFF,
        (bytes >> 16) & 0xFF,
        (bytes >> 8) & 0xFF,
        bytes & 0xFF
    );
    
    // normalize to [0.0, 1.0]
    result *= 1.0 / 255.0;
    
    return result;
}
#endif

// packs a float into a byte so that -1.0 is 0 and +1.0 is 255.
uint normalized_float_to_byte(float value)
{
    return (1.0 + value) * 0.5 * 255.0;
}

float byte_to_normalized_float(uint byte)
{
    return -1.0 + byte * (1.0 / 255.0) * 2.0;
}

// packs a float into a byte so that 0.0 is 0 and +1.0 is 255.
uint saturated_float_to_byte(float value)
{
    return value * 255;
}

float byte_to_saturated_float(float value)
{
    return value * (1.0 / 255.0);
}

float pack_normalized_float4_into_float(float4 value)
{
    //value = normalize(value);
    uint x8 = normalized_float_to_byte(value.x);
    uint y8 = normalized_float_to_byte(value.y);
    uint z8 = normalized_float_to_byte(value.z);
    uint w8 = normalized_float_to_byte(value.w);
    uint combined = (x8 << 24) | (y8 << 16) | (z8 << 8) | w8;
    return asfloat(combined); // force the bit pattern into a float.
}

float4 unpack_normalized_float4_from_float(float value)
{
    uint bytes = asuint(value);
    float4 result;
    result.x = byte_to_normalized_float((bytes >> 24) & 0xFF);
    result.y = byte_to_normalized_float((bytes >> 16) & 0xFF);
    result.z = byte_to_normalized_float((bytes >> 8) & 0xFF);
    result.w = byte_to_normalized_float(bytes & 0xFF);
    return result;
}

float pack_saturated_float4_into_float(float4 value)
{
    value = saturate(value);
    uint x8 = saturated_float_to_byte(value.x);
    uint y8 = saturated_float_to_byte(value.y);
    uint z8 = saturated_float_to_byte(value.z);
    uint w8 = saturated_float_to_byte(value.w);
    uint combined = (x8 << 24) | (y8 << 16) | (z8 << 8) | w8;
    return asfloat(combined); // force the bit pattern into a float.
}

float4 unpack_saturated_float4_from_float(float value)
{
    uint bytes = asuint(value);
    float4 result;
    result.x = byte_to_saturated_float((bytes >> 24) & 0xFF);
    result.y = byte_to_saturated_float((bytes >> 16) & 0xFF);
    result.z = byte_to_saturated_float((bytes >> 8) & 0xFF);
    result.w = byte_to_saturated_float(bytes & 0xFF);
    return result;
}

// special thanks to https://learnopengl.com/PBR/Lighting

// normal distribution function: approximates the amount the surface's
// microfacets are aligned to the halfway vector, influenced by the roughness of
// the surface; this is the primary function approximating the microfacets.
float DistributionGGX(float3 N, float3 H, float3 roughness)
{
    float a = roughness * roughness;
    float a2 = a * a;
    float NdotH = max(dot(N, H), 0.0);
    float NdotH2 = NdotH * NdotH;

    float num = a2;
    float denom = (NdotH2 * (a2 - 1.0) + 1.0);
    denom = UNITY_PI * denom * denom;

    return num / denom;
}

// geometry function: describes the self-shadowing property of the microfacets.
// when a surface is relatively rough, the surface's microfacets can overshadow
// other microfacets reducing the light the surface reflects.
float GeometrySchlickGGX(float NdotV, float roughness)
{
    float r = (roughness + 1.0);
    float k = (r * r) / 8.0;

    float num = NdotV;
    float denom = NdotV * (1.0 - k) + k;

    return num / denom;
}

float GeometrySmith(float3 N, float3 V, float3 L, float roughness)
{
    float NdotV = max(dot(N, V), 0.0);
    float NdotL = max(dot(N, L), 0.0);
    float ggx2 = GeometrySchlickGGX(NdotV, roughness);
    float ggx1 = GeometrySchlickGGX(NdotL, roughness);

    return ggx1 * ggx2;
}

// fresnel equation: the fresnel equation describes the ratio of surface
// reflection at different surface angles.
float3 fresnelSchlick(float cosTheta, float3 F0)
{
    return F0 + (1.0 - F0) * pow(clamp(1.0 - cosTheta, 0.0, 1.0), 5.0);
}

float3 fresnelSchlickRoughness(float cosTheta, float3 F0, float roughness)
{
    return F0 + (max(1.0 - roughness, F0) - F0) * pow(clamp(1.0 - cosTheta, 0.0, 1.0), 5.0);
}