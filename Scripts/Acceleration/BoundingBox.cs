using Unity.Mathematics;
using UnityEngine;

namespace AlpacaIT.DynamicLighting.Acceleration
{
    /// <summary>A bounding box.</summary>
    public class BoundingBox : BoundingVolume
    {
        /// <summary>The bounds of the bounding box.</summary>
        public Bounds bounds;

        public override int FindSplittingAxis(BoundingVolume[] boundingVolumes)
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
                Vector3 extents = ((BoundingBox)boundingVolumes[i]).bounds.extents;

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

        public override bool Contains(float3 worldPosition)
        {
            return bounds.Contains(worldPosition);
        }
    }
}