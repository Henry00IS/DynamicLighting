using System;
using Unity.Mathematics;
using UnityEngine;

namespace AlpacaIT.DynamicLighting.Acceleration
{
    /// <summary>An axis-aligned bounding box volume.</summary>
    public class BvhBoundingVolume
    {
        /// <summary>The minimal point of the axis-aligned bounding box.</summary>
        public float3 min;
        /// <summary>The maximal point of the axis-aligned bounding box.</summary>
        public float3 max;

        /// <summary>Creates a new instance of the axis-aligned bounding box.</summary>
        public BvhBoundingVolume()
        {
        }

        /// <summary>
        /// Creates an axis-aligned bounding box encapsulating the sphere of the specified radius.
        /// </summary>
        /// <param name="radius">The radius of the sphere to be included.</param>
        public BvhBoundingVolume(float3 center, float radius)
        {
            this.center = center;

            // calculate the half-extents of the bounding box based on the radius.
            var halfExtents = new float3(radius);

            // update min and max points to encompass the sphere.
            min = center - halfExtents;
            max = center + halfExtents;
        }

        /// <summary>Gets or sets the center point of the axis-aligned bounding box.</summary>
        public float3 center
        {
            get => (min + max) * 0.5f;
            set
            {
                var halfExtent = extents;
                min = value - halfExtent;
                max = value + halfExtent;
            }
        }

        /// <summary>Gets or sets the extents (half the size) of the axis-aligned bounding box.</summary>
        public float3 extents
        {
            get => (max - min) * 0.5f;
            set
            {
                var centerPoint = center;
                min = centerPoint - value;
                max = centerPoint + value;
            }
        }

        /// <summary>Gets or sets the size of the axis-aligned bounding box.</summary>
        public float3 size
        {
            get => max - min;
            set
            {
                var centerPoint = center;
                var halfSize = value * 0.5f;
                min = centerPoint - halfSize;
                max = centerPoint + halfSize;
            }
        }

        /// <summary>Checks whether a point is contained within the axis-aligned bounding box.</summary>
        /// <param name="point">The point to be checked.</param>
        /// <returns>True when the point is within the bounding box else false.</returns>
        public bool Contains(float3 point)
        {
            return point.x >= min.x && point.x <= max.x &&
                   point.y >= min.y && point.y <= max.y &&
                   point.z >= min.z && point.z <= max.z;
        }

        /// <summary>Grows the axis-aligned bounding box to include the point.</summary>
        /// <param name="point">The point to be included.</param>
        public void Encapsulate(float3 point)
        {
            // Update min and max points to include the provided point.
            min = math.min(min, point);
            max = math.max(max, point);
        }

        /// <summary>
        /// Grows the axis-aligned bounding box to include the other axis-aligned bounding box.
        /// </summary>
        /// <param name="other">The other axis-aligned bounding box to be included.</param>
        public void Encapsulate(BvhBoundingVolume other)
        {
            // ensure the child bounding volume is not null.
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            // update min and max points to encompass the child bounding volume.
            min = math.min(min, other.min);
            max = math.max(max, other.max);
        }

        /// <summary>
        /// Finds the optimal splitting axis with the maximum extent of the bounding volumes.
        /// </summary>
        /// <param name="boundingVolumes">The bounding volumes to be tested.</param>
        /// <returns>0 for x-axis, 1 for y-axis, and 2 for z-axis.</returns>
        public int FindSplittingAxis(BvhBoundingVolume[] boundingVolumes)
        {
            // Initialize variables to store the min and max values for each axis.
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float minZ = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;
            float maxZ = float.MinValue;

            // Iterate through the bounding boxes to find the min and max values for each axis.
            for (int i = 0; i < boundingVolumes.Length; i++)
            {
                Vector3 center = boundingVolumes[i].center;
                Vector3 extents = boundingVolumes[i].extents;

                minX = math.min(minX, center.x - extents.x);
                minY = math.min(minY, center.y - extents.y);
                minZ = math.min(minZ, center.z - extents.z);
                maxX = math.max(maxX, center.x + extents.x);
                maxY = math.max(maxY, center.y + extents.y);
                maxZ = math.max(maxZ, center.z + extents.z);
            }

            // Calculate the extents for each axis.
            float extentX = maxX - minX;
            float extentY = maxY - minY;
            float extentZ = maxZ - minZ;

            // Determine the axis with the maximum extent.
            if (extentX >= extentY && extentX >= extentZ)
            {
                return 0; // x-axis
            }
            else if (extentY >= extentX && extentY >= extentZ)
            {
                return 1; // y-axis
            }
            else
            {
                return 2; // z-axis
            }
        }

        public static void SplitBoundingVolumes(BvhBoundingVolume[] boundingVolumes, int axis, out BvhBoundingVolume[] leftSubset, out BvhBoundingVolume[] rightSubset)
        {
            // sort the bounding volumes based on the chosen axis.
            Array.Sort(boundingVolumes, (a, b) => a.center[axis].CompareTo(b.center[axis]));

            // calculate the index for the median element.
            int medianIndex = boundingVolumes.Length / 2;

            // initialize left and right subsets.
            leftSubset = new BvhBoundingVolume[medianIndex];
            rightSubset = new BvhBoundingVolume[boundingVolumes.Length - medianIndex];

            // populate the left and right subsets.
            for (int i = 0; i < medianIndex; i++)
            {
                leftSubset[i] = boundingVolumes[i];
            }

            for (int i = medianIndex; i < boundingVolumes.Length; i++)
            {
                rightSubset[i - medianIndex] = boundingVolumes[i];
            }
        }
    }
}