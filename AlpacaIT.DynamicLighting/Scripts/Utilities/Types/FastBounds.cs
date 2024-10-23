using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>Similar to <see cref="Bounds"/> just much faster.</summary>
    internal unsafe struct FastBounds
    {
        public Vector3 center;
        public Vector3 extents;

        /// <summary>
        /// Creates a new instance of <see cref="FastBounds"/>.
        /// </summary>
        /// <param name="center">The location of the origin of the <see cref="FastBounds"/>.</param>
        /// <param name="size">The dimensions of the <see cref="FastBounds"/>.</param>
        public FastBounds(Vector3 center, Vector3 size)
        {
            this.center = center;
            extents.x = size.x * 0.5f;
            extents.y = size.y * 0.5f;
            extents.z = size.z * 0.5f;
        }

        /// <summary>Does another bounding box intersect with this bounding box?</summary>
        /// <param name="bounds">The bounding box to check against.</param>
        /// <returns>True when the bounding boxes intersect else false.</returns>
        public bool Intersects(Bounds bounds)
        {
            var fastBounds = *(FastBounds*)&bounds;

            var boundsMin = fastBounds.center;
            boundsMin.x -= fastBounds.extents.x;
            boundsMin.y -= fastBounds.extents.y;
            boundsMin.z -= fastBounds.extents.z;

            var boundsMax = fastBounds.center;
            boundsMax.x += fastBounds.extents.x;
            boundsMax.y += fastBounds.extents.y;
            boundsMax.z += fastBounds.extents.z;

            var min = center;
            min.x -= extents.x;
            min.y -= extents.y;
            min.z -= extents.z;

            var max = center;
            max.x += extents.x;
            max.y += extents.y;
            max.z += extents.z;

            return min.x <= boundsMax.x && max.x >= boundsMin.x && min.y <= boundsMax.y && max.y >= boundsMin.y && min.z <= boundsMax.z && max.z >= boundsMin.z;
        }

        public Vector3 min
        {
            get
            {
                var min = center;
                min.x -= extents.x;
                min.y -= extents.y;
                min.z -= extents.z;
                return min;
            }
        }

        public Vector3 max
        {
            get
            {
                var max = center;
                max.x += extents.x;
                max.y += extents.y;
                max.z += extents.z;
                return max;
            }
        }
    }
}