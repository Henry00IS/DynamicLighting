using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    public static class MathEx
    {
        public static bool SpheresIntersect(Vector3 spherePosition1, float sphereRadius1, Vector3 spherePosition2, float sphereRadius2)
        {
            return Vector3.Distance(spherePosition1, spherePosition2) <= (sphereRadius1 + sphereRadius2);
        }

        // thanks to https://gist.github.com/openroomxyz/c67e77f710eb53aaaae7390ded9ce86d
        public static float CalculateSurfaceAreaOfTriangle(Vector3 n1, Vector3 n2, Vector3 n3)
        {
            float res = Mathf.Pow((n2.x * n1.y) - (n3.x * n1.y) - (n1.x * n2.y) + (n3.x * n2.y) + (n1.x * n3.y) - (n2.x * n3.y), 2.0f);
            res += Mathf.Pow((n2.x * n1.z) - (n3.x * n1.z) - (n1.x * n2.z) + (n3.x * n2.z) + (n1.x * n3.z) - (n2.x * n3.z), 2.0f);
            res += Mathf.Pow((n2.y * n1.z) - (n3.y * n1.z) - (n1.y * n2.z) + (n3.y * n2.z) + (n1.y * n3.z) - (n2.y * n3.z), 2.0f);
            return Mathf.Sqrt(res) * 0.5f;
        }

        /// <summary>
        /// Calculates an optimal texture size (in power of 2) that fits the specified surface area
        /// (in meters squared) in the desired pixel density (e.g. 256 for 256x256 per meter squared).
        /// </summary>
        /// <param name="surfaceAreaMeterSquared">The surface area in meters squared.</param>
        /// <param name="pixelDensity">The pixel density (e.g. 256 for 256x256 per meter squared).</param>
        /// <returns>
        /// The texture dimension in power of two (32, 64, 512, etc.) with no upper bound.
        /// </returns>
        public static int SurfaceAreaToTextureSize(float surfaceAreaMeterSquared, float pixelDensity)
        {
            float pixelDensitySquared = pixelDensity * pixelDensity; // pixel density squared.
            float surfaceSquareRoot = Mathf.Sqrt(pixelDensitySquared * surfaceAreaMeterSquared);
            return Mathf.NextPowerOfTwo(Mathf.CeilToInt(surfaceSquareRoot));
        }
    }
}