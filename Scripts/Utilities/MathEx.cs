using Unity.Mathematics;
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

        /// <summary>Calculates a look-at orientation matrix compatible with HLSL.</summary>
        /// <param name="forward">The forward direction (<see cref="Transform.forward"/>).</param>
        /// <param name="up">The up direction (<see cref="Transform.up"/>).</param>
        /// <returns>The compressed 3x3 matrix with the rotation.</returns>
        public static float3x3 ShaderLookAtMatrix(Vector3 forward, Vector3 up)
        {
            var right = math.normalize(math.cross(forward, up));
            return new float3x3(
                right.x, up.x, forward.x,
                right.y, up.y, forward.y,
                right.z, up.z, forward.z
            );
        }

        /// <summary>
        /// Given an oriented bounding box size calculates the largest radius it can possibly take
        /// when rotated.
        /// </summary>
        /// <param name="size">The size of the bounding box.</param>
        /// <returns>The largest enclosing radius of the bounding box.</returns>
        public static float CalculateLargestObbRadius(Vector3 size)
        {
            // calculate the length of the space diagonal using the Pythagorean theorem.
            float spaceDiagonalLength = math.sqrt(size.x * size.x + size.y * size.y + size.z * size.z);
            return spaceDiagonalLength * 0.5f; // return half of the space diagonal length (radius).
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