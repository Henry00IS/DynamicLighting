using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    internal static class MathEx
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
        /// Calculates an optimal texture size that fits the specified surface area (in meters
        /// squared) in the desired pixel density (e.g. 256 for 256x256 per meter squared).
        /// </summary>
        /// <param name="surfaceAreaMeterSquared">The surface area in meters squared.</param>
        /// <param name="pixelDensity">The pixel density (e.g. 256 for 256x256 per meter squared).</param>
        /// <returns>The texture dimension with no upper bound.</returns>
        public static int SurfaceAreaToTextureSize(float surfaceAreaMeterSquared, float pixelDensity)
        {
            float pixelDensitySquared = pixelDensity * pixelDensity; // pixel density squared.
            float surfaceSquareRoot = Mathf.Sqrt(pixelDensitySquared * surfaceAreaMeterSquared);
            return Mathf.CeilToInt(surfaceSquareRoot);
        }

        /// <summary>
        /// Given a set of camera frustum planes, checks if the sphere (as a position and radius)
        /// intersects with it.
        /// </summary>
        /// <param name="planes">The 6 camera frustum planes.</param>
        /// <param name="center">The center position of the sphere.</param>
        /// <param name="radius">The radius of the sphere.</param>
        /// <returns>True when the planes intersect the sphere else false.</returns>
        public static bool CheckSphereIntersectsFrustum(Plane[] planes, Vector3 center, float radius)
        {
            for (int i = 0; i < planes.Length; i++)
                if (planes[i].normal.x * center.x + planes[i].normal.y * center.y + planes[i].normal.z * center.z + planes[i].distance < -radius)
                    // ^ is the same as: if (planes[i].GetDistanceToPoint(center) < -radius)
                    return false;
            return true;
        }

        /// <summary>Checks whether a sphere intersects with a triangle.</summary>
        /// <param name="center">The center position of the sphere.</param>
        /// <param name="radius">The radius of the sphere.</param>
        /// <param name="a">The first vertex position of the triangle.</param>
        /// <param name="b">The second vertex position of the triangle.</param>
        /// <param name="c">The third vertex position of the triangle.</param>
        /// <returns>True when there was an intersection else false.</returns>
        public static bool CheckSphereIntersectsTriangle(Vector3 center, float radius, Vector3 a, Vector3 b, Vector3 c)
        {
            // shoutouts to Zode (https://github.com/Zode)!

            // find point on triangle surface closest to the center of the sphere.
            var closestPointOnTriangle = ClosestPointOnTriangle(center, a, b, c);

            // the sphere and triangle intersect if the (squared) distance from the sphere center to
            // the closest point on the triangle is less than the (squared) sphere radius.
            Vector3 v = closestPointOnTriangle - center;
            return Vector3.Dot(v, v) <= radius * radius;
        }

        /// <summary>Finds the closest point to the given point on the surface of the triangle.</summary>
        /// <param name="p">The point to find on the triangle.</param>
        /// <param name="a">The first vertex position of the triangle.</param>
        /// <param name="b">The second vertex position of the triangle.</param>
        /// <param name="c">The third vertex position of the triangle.</param>
        /// <returns>The closest point on the surface of the triangle.</returns>
        // special thanks to Christer Ericson, taken from the book Real-Time Collision Detection.
        public static Vector3 ClosestPointOnTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
        {
            // check if p is in vertex region outside a.
            var ab = b - a;
            var ac = c - a;
            var ap = p - a;
            float d1 = Vector3.Dot(ab, ap);
            float d2 = Vector3.Dot(ac, ap);
            if (d1 <= 0.0f && d2 <= 0.0f) return a; // barycentric coordinates (1,0,0).

            // check if p in vertex region outside b.
            var bp = p - b;
            float d3 = Vector3.Dot(ab, bp);
            float d4 = Vector3.Dot(ac, bp);
            if (d3 >= 0.0f && d4 <= d3) return b; // barycentric coordinates (0,1,0).

            // check if p in edge region of ab, if so return projection of p onto ab.
            float vc = d1 * d4 - d3 * d2;
            if (vc <= 0.0f && d1 >= 0.0f && d3 <= 0.0f)
            {
                float v = d1 / (d1 - d3);
                return a + v * ab; // barycentric coordinates (1-v,v,0).
            }

            // check if p in vertex region outside c.
            var cp = p - c;
            float d5 = Vector3.Dot(ab, cp);
            float d6 = Vector3.Dot(ac, cp);
            if (d6 >= 0.0f && d5 <= d6) return c; // barycentric coordinates (0,0,1).

            // check if p in edge region of ac, if so return projection of p onto ac.
            float vb = d5 * d2 - d1 * d6;
            if (vb <= 0.0f && d2 >= 0.0f && d6 <= 0.0f)
            {
                float w = d2 / (d2 - d6);
                return a + w * ac; // barycentric coordinates (1-w,0,w).
            }

            // check if p in edge region of bc, if so return projection of p onto bc.
            float va = d3 * d6 - d5 * d4;
            if (va <= 0.0f && (d4 - d3) >= 0.0f && (d5 - d6) >= 0.0f)
            {
                float w = (d4 - d3) / ((d4 - d3) + (d5 - d6));
                return b + w * (c - b); // barycentric coordinates (0,1-w,w).
            }

            // p inside face region. compute q through its barycentric coordinates (u,v,w).
            float denom = 1.0f / (va + vb + vc);
            float v2 = vb * denom;
            float w2 = vc * denom;
            return a + ab * v2 + ac * w2; // = u*a + v*b + w*c, u = va * denom = 1.0f-v-w.
        }

        /// <summary>Calculates a bounding box for the given triangle.</summary>
        /// <param name="a">The first vertex position of the triangle.</param>
        /// <param name="b">The second vertex position of the triangle.</param>
        /// <param name="c">The third vertex position of the triangle.</param>
        /// <returns>The <see cref="Rect"/> encompassing the triangle.</returns>
        public static Rect ComputeTriangleBoundingBox(Vector2 a, Vector2 b, Vector2 c)
        {
            float sx1 = a.x;
            float sx2 = b.x;
            float sx3 = c.x;
            float sy1 = a.y;
            float sy2 = b.y;
            float sy3 = c.y;

            float xmax = sx1 > sx2 ? (sx1 > sx3 ? sx1 : sx3) : (sx2 > sx3 ? sx2 : sx3);
            float ymax = sy1 > sy2 ? (sy1 > sy3 ? sy1 : sy3) : (sy2 > sy3 ? sy2 : sy3);
            float xmin = sx1 < sx2 ? (sx1 < sx3 ? sx1 : sx3) : (sx2 < sx3 ? sx2 : sx3);
            float ymin = sy1 < sy2 ? (sy1 < sy3 ? sy1 : sy3) : (sy2 < sy3 ? sy2 : sy3);

            return new Rect(xmin, ymin, xmax - xmin, ymax - ymin);
        }

        /// <summary>Calculates a signed triangle area using a kind of "2D cross product".</summary>
        /// <param name="a">The first vertex position of the triangle.</param>
        /// <param name="b">The second vertex position of the triangle.</param>
        /// <param name="c">The third vertex position of the triangle.</param>
        /// <returns>The signed triangle area.</returns>
        public static float SignedTriangleArea(Vector2 a, Vector2 b, Vector2 c)
        {
            var v1 = a - c;
            var v2 = b - c;
            return (v1.x * v2.y - v1.y * v2.x) / 2f;
        }

        /// <summary>
        /// Using the vertices and associated uv-coordinates of a triangle, converts the given point
        /// in uv-space to vertex-space. For example, if the vertices are in world-space then the
        /// given point in uv-space will be converted to world-space. It will return <see
        /// cref="Vector3.zero"/> when the given point lies outside of the triangle's uv-coordinates.
        /// </summary>
        /// <param name="uv">The point in uv-space to be converted to vertex-space.</param>
        /// <param name="v1">The first vertex position of the triangle.</param>
        /// <param name="v2">The second vertex position of the triangle.</param>
        /// <param name="v3">The third vertex position of the triangle.</param>
        /// <param name="t1">The first vertex uv-coordinate of the triangle.</param>
        /// <param name="t2">The second vertex uv-coordinate of the triangle.</param>
        /// <param name="t3">The third vertex uv-coordinate of the triangle.</param>
        /// <returns>
        /// The point converted to vertex-space or else <see cref="Vector3.zero"/> when outside of
        /// the triangle's uv-coordinates.
        /// </returns>
        public static Vector3 UvTo3dFast(Vector2 uv, Vector3 v1, Vector3 v2, Vector3 v3, Vector2 t1, Vector2 t2, Vector2 t3)
        {
            // calculate triangle area - if zero, skip it.
            var a = SignedTriangleArea(t1, t2, t3); if (a == 0f) return Vector3.zero;

            // calculate barycentric coordinates of u1, u2 and u3.
            // if anyone is negative, point is outside the triangle: skip it.
            var a1 = SignedTriangleArea(t2, t3, uv) / a; if (a1 < 0f) return Vector3.zero;
            var a2 = SignedTriangleArea(t3, t1, uv) / a; if (a2 < 0f) return Vector3.zero;
            var a3 = SignedTriangleArea(t1, t2, uv) / a; if (a3 < 0f) return Vector3.zero;

            // point inside the triangle - find mesh position by interpolation.
            return a1 * v1 + a2 * v2 + a3 * v3;
        }

        /// <summary>
        /// Rounds a size in bytes into a human-readable format of unit sizes (20B, 128KiB, 32MiB, ...).
        /// </summary>
        /// <param name="bytes">The amount of bytes as an unsigned 64-bit integer.</param>
        /// <returns>The rounded unit size as a string.</returns>
        public static string BytesToUnitString(ulong bytes)
        {
            if (bytes < (ulong)1024) return bytes + "B";
            if (bytes < (ulong)1024 * 1024) return bytes / 1024 + "KiB";
            if (bytes < (ulong)1024 * 1024 * 1024) return bytes / 1024 / 1024 + "MiB";
            if (bytes < (ulong)1024 * 1024 * 1024 * 1024) return bytes / 1024 / 1024 / 1024 + "GiB";
            if (bytes < (ulong)1024 * 1024 * 1024 * 1024 * 1024) return bytes / 1024 / 1024 / 1024 / 1024 + "TiB"; // the future!
            return bytes + "B";
        }

        /// <summary>
        /// Calculates a framerate independent fixed timestep.
        /// </summary>
        public class FixedTimestep
        {
            /// <summary>The amount of time in seconds between each fixed timestep.</summary>
            public float timePerStep;
            private float timeAccumulator;

            /// <summary>
            /// The amount of steps that have to be executed since the last call to <see cref="Update"/>.
            /// </summary>
            public int pendingSteps;

            /// <summary>Creates a new framerate independent fixed timestep calculator.</summary>
            /// <param name="timePerStep">The amount of time in seconds between each fixed timestep.</param>
            public FixedTimestep(float timePerStep)
            {
                this.timePerStep = timePerStep;
            }

            /// <summary>
            /// Updates the framerate independent fixed timestep and returns how many steps to execute
            /// this frame. This is to be called once every Unity Update and relies on scaled <see
            /// cref="Time.deltaTime"/> so that it can be adjusted or even paused by the user. This will
            /// become inaccurate when the current <see cref="Time.deltaTime"/> exceeds <see cref="Time.maximumDeltaTime"/>.
            /// </summary>
            public void Update()
            {
                pendingSteps = 0;
                timeAccumulator += Time.deltaTime;

                if (timeAccumulator >= timePerStep)
                {
                    pendingSteps = Mathf.FloorToInt(timeAccumulator / timePerStep);
                    timeAccumulator -= pendingSteps * timePerStep;
                }
            }

            /// <summary>
            /// Updates the framerate independent fixed timestep and returns how many steps to execute
            /// this frame.
            /// </summary>
            /// <param name="deltaTime">
            /// The interval in seconds from the last frame to the current one.
            /// </param>
            public void Update(float deltaTime)
            {
                pendingSteps = 0;
                timeAccumulator += deltaTime;

                if (timeAccumulator >= timePerStep)
                {
                    pendingSteps = Mathf.FloorToInt(timeAccumulator / timePerStep);
                    timeAccumulator -= pendingSteps * timePerStep;
                }
            }
        }
    }
}