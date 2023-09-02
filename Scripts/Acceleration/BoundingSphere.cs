using Unity.Mathematics;

namespace AlpacaIT.DynamicLighting.Acceleration
{
    /// <summary>A spherical bounding volume that encompasses an object.</summary>
    public class BoundingSphere : BoundingVolume
    {
        /// <summary>The radius of the bounding sphere.</summary>
        public float radius;

        public override int FindSplittingAxis(BoundingVolume[] boundingVolumes)
        {
            // Initialize variables to store the min and max values for each axis.
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float minZ = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;
            float maxZ = float.MinValue;

            // Iterate through the bounding volumes to find the min and max values for each axis.
            for (int i = 0; i < boundingVolumes.Length; i++)
            {
                float3 center = boundingVolumes[i].center;
                float radius = ((BoundingSphere)boundingVolumes[i]).radius;

                minX = math.min(minX, center.x - radius);
                minY = math.min(minY, center.y - radius);
                minZ = math.min(minZ, center.z - radius);
                maxX = math.max(maxX, center.x + radius);
                maxY = math.max(maxY, center.y + radius);
                maxZ = math.max(maxZ, center.z + radius);
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

        /// <summary>
        /// Check if the current node's bounding volume intersects with the target world position.
        /// </summary>
        /// <param name="worldPosition">The world position to check for overlap.</param>
        /// <returns>True if there is an intersection; false otherwise.</returns>
        public override bool Contains(float3 worldPosition)
        {
            // Check if the current node's bounding volume intersects with the target position.
            // This depends on the type of bounding volume (e.g., AABB or BoundingSphere).
            // Implement the appropriate intersection check here.
            // For example, for an AABB, you would check if the position is within the bounds.
            // For a BoundingSphere, you would check if the distance between the position and the sphere's center is less than the radius.

            // Example for BoundingSphere:
            float distance = math.distance(worldPosition, center);
            return distance <= radius;
        }
    }
}