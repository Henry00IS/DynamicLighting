// calculates a look-at orientation matrix.
// warning: 'forward' and 'up' must be normalized.
float3x3 look_at_matrix(float3 forward, float3 up)
{
    float3 right = cross(forward, up);
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

// convert a direction to face index and uv [0..1].
// standard cubemap mapping: +x, -x, +y, -y, +z, -z.
void cubemap_get_face_and_uv(float3 dir, out uint face, out float2 uv)
{
    float3 absDir = abs(dir);
    float ma;
    float2 sc; // sc = (sc.x, sc.y) = (xc, yc) usually.

    if (absDir.x > absDir.y && absDir.x > absDir.z) {
        ma = absDir.x;
        face = dir.x > 0 ? 0 : 1;
        sc = (dir.x > 0) ? float2(-dir.z, -dir.y) : float2(dir.z, -dir.y);
    } else if (absDir.y > absDir.z) {
        ma = absDir.y;
        face = dir.y > 0 ? 2 : 3;
        sc = (dir.y > 0) ? float2(dir.x, dir.z) : float2(dir.x, -dir.z);
    } else {
        ma = absDir.z;
        face = dir.z > 0 ? 4 : 5;
        sc = (dir.z > 0) ? float2(dir.x, -dir.y) : float2(-dir.x, -dir.y);
    }
    
    // convert range [-ma, ma] to [0, 1].
    uv = (sc / ma) * 0.5 + 0.5;
}

// convert face index and uv [0..1] back to a direction.
// this allows us to find the exact direction of a neighbor texel center.
float3 cubemap_get_dir(uint face, float2 uv)
{
    static const float3 FaceOrigins[6] = {
        float3(1,1,1),   // +X
        float3(-1,1,-1), // -X
        float3(-1,1,-1), // +Y
        float3(-1,-1,1), // -Y
        float3(-1,1,1),  // +Z
        float3(1,1,-1)   // -Z
    };

    static const float3 FaceRights[6] = {
        float3(0,0,-2), float3(0,0,2),  float3(2,0,0), 
        float3(2,0,0),  float3(2,0,0),  float3(-2,0,0)
    };

    static const float3 FaceUps[6] = {
        float3(0,-2,0), float3(0,-2,0), float3(0,0,2), 
        float3(0,0,-2), float3(0,-2,0), float3(0,-2,0)
    };
    
    float3 dir = FaceOrigins[face] + (uv.x * FaceRights[face]) + (uv.y * FaceUps[face]);
    return normalize(dir);
}

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
    // precompute frequently used values.
    float3 w = rayOrigin - coneTip;

    float cosTheta = cos(coneAngle);
    float cosTheta2 = cosTheta * cosTheta;
    float sinTheta2 = 1.0 - cosTheta2;
    float invCosTheta2 = 1.0 / cosTheta2;

    float d_dot_s = dot(rayDir, coneDir);
    float w_dot_s = dot(w, coneDir);
    float d_dot_w = dot(rayDir, w);
    float w_dot_w = dot(w, w);

    float A = d_dot_s * d_dot_s - cosTheta2;
    float B = 2.0 * (d_dot_s * w_dot_s - cosTheta2 * d_dot_w);
    float C = w_dot_s * w_dot_s - cosTheta2 * w_dot_w;

    float discriminant = B * B - 4.0 * A * C;

    tMin = 0.0;
    tMax = 0.0;

    int numHits = 0;
    float tValues[2];

    // check if the ray origin is inside the cone.
    float k = sinTheta2 * invCosTheta2;
    float coneRadius2 = w_dot_s * w_dot_s * k;
    float distance2 = w_dot_w - w_dot_s * w_dot_s;

    bool isInsideCone = (w_dot_s >= 0.0 && w_dot_s <= coneDistance) && (distance2 <= coneRadius2);

    if (isInsideCone)
    {
        tMin = 0.0;
    }

    // compute potential intersections with the cone's body.
    if (discriminant >= 0.0)
    {
        float sqrtDiscriminant = sqrt(discriminant);
        float invA = 0.5 / A;

        float t0 = (-B - sqrtDiscriminant) * invA;
        float t1 = (-B + sqrtDiscriminant) * invA;

        // simplify hit height calculations.
        float h0 = w_dot_s + t0 * d_dot_s;
        float h1 = w_dot_s + t1 * d_dot_s;

        // check t0.
        if (t0 >= 0.0 && h0 >= 0.0 && h0 <= coneDistance)
            tValues[numHits++] = t0;

        // check t1.
        if (t1 >= 0.0 && h1 >= 0.0 && h1 <= coneDistance)
            tValues[numHits++] = t1;
    }

    // ray-plane intersection test for the cone cap.
    float denom = dot(rayDir, coneDir);
    if (abs(denom) > 0.0001)
    {
        float tCap = (coneDistance - w_dot_s) / denom;
        if (tCap >= 0.0)
        {
            float3 hitPoint = w + tCap * rayDir;
            float hitPointDist2 = dot(hitPoint, hitPoint) - (w_dot_s + tCap * d_dot_s) * (w_dot_s + tCap * d_dot_s);
            float capRadius2 = coneDistance * coneDistance * k;
            if (hitPointDist2 <= capRadius2)
                tValues[numHits++] = tCap;
        }
    }

    if (numHits == 0)
        return isInsideCone;

    // sort tMin and tMax.
    if (numHits == 1)
    {
        if (isInsideCone)
            tMax = tValues[0];
        else
            tMin = tMax = tValues[0];
        tMax = min(tMax, maxDepth);
        return true;
    }
    else
    {
        tMin = min(tValues[0], tValues[1]);
        tMax = max(tValues[0], tValues[1]);
        if (isInsideCone)
            tMin = 0.0;
        tMax = min(tMax, maxDepth);
        return true;
    }
}

// shoutouts to lordofduct (https://forum.unity.com/threads/how-do-i-find-the-closest-point-on-a-line.340058/)
float3 nearest_point_on_finite_line(float3 start, float3 end, float3 pnt)
{
    float3 lineDir = end - start;
    float lenSq = dot(lineDir, lineDir);
    float3 v = pnt - start;
    float d = dot(v, lineDir);
    d = saturate(d / lenSq);
    return start + lineDir * d;
}

// looks at each channel's color information and multiplies the inverse of the blend and
// base colors. the result color is always a lighter color. screening with black leaves the
// color unchanged. screening with white produces white. the effect is similar to projecting
// multiple photographic slides on top of each other.
float4 color_screen(float4 self, float4 blend)
{
    return 1.0 - (1.0 - self) * (1.0 - blend);
}

uint float8(float f)
{
    f = clamp(f, -1.0, 1.0);
    return (f + 1.0) * 127.0;
}

uint minivector3(float3 v)
{
    return (float8(v.z) << 16) | (float8(v.y) << 8) | float8(v.x);
}

float texture_alpha_sample_gaussian5(sampler2D tex, float2 texelsize, float2 uv)
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
            map += weights[i + 2][j + 2] * tex2D(tex, uv + float2(i, j) * texelsize).a;
        }
    }
        
    return map / 256.0; // normalize by the sum of the weights.
}

float linstep(float min, float max, float v)
{
    return clamp((v - min) / (max - min), 0, 1);
}
float ReduceLightBleeding(float p_max, float amount)
{
    // Remove the [0, amount] tail and linearly rescale (amount, 1].
    return linstep(amount, 1, p_max);
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