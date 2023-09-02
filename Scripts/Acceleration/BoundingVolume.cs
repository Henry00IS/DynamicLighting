using System;
using Unity.Mathematics;

namespace AlpacaIT.DynamicLighting.Acceleration
{
    /// <summary>A bounding volume that encompasses an object.</summary>
    public abstract class BoundingVolume
    {
        /// <summary>The center world position of the bounding volume.</summary>
        public float3 center;

        /// <summary>
        /// Finds the optimal splitting axis with the maximum extent of the bounding volumes.
        /// </summary>
        /// <param name="boundingVolumes">The bounding volumes to be tested.</param>
        /// <returns>0 for x-axis, 1 for y-axis, and 2 for z-axis.</returns>
        public abstract int FindSplittingAxis(BoundingVolume[] boundingVolumes);

        public static void SplitBoundingVolumes(BoundingVolume[] boundingVolumes, int axis, out BoundingVolume[] leftSubset, out BoundingVolume[] rightSubset)
        {
            // Sort the bounding volumes based on the chosen axis.
            Array.Sort(boundingVolumes, (a, b) => a.center[axis].CompareTo(b.center[axis]));

            // Calculate the index for the median element.
            int medianIndex = boundingVolumes.Length / 2;

            // Initialize left and right subsets.
            leftSubset = new BoundingVolume[medianIndex];
            rightSubset = new BoundingVolume[boundingVolumes.Length - medianIndex];

            // Populate the left and right subsets.
            for (int i = 0; i < medianIndex; i++)
            {
                leftSubset[i] = boundingVolumes[i];
            }

            for (int i = medianIndex; i < boundingVolumes.Length; i++)
            {
                rightSubset[i - medianIndex] = boundingVolumes[i];
            }
        }

        /// <summary>
        /// Check if the current node's bounding volume intersects with the target world position.
        /// </summary>
        /// <param name="worldPosition">The world position to check for overlap.</param>
        /// <returns>True if there is an intersection; false otherwise.</returns>
        public abstract bool Contains(float3 worldPosition);
    }
}