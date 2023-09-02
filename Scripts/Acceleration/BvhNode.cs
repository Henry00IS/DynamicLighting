using System.Collections.Generic;
using Unity.Mathematics;

namespace AlpacaIT.DynamicLighting.Acceleration
{
    /// <summary>A node encompassing a subset of bounding volumes or object(s).</summary>
    public class BvhNode
    {
        /// <summary>
        /// The bounding volume that encompasses both the <see cref="leftChild"/> and the <see cref="rightChild"/>.
        /// </summary>
        public BvhBoundingVolume boundingVolume;

        /// <summary>The left bounding volume at this node (if any).</summary>
        public BvhNode leftChild;

        /// <summary>The right bounding volume at this node (if any).</summary>
        public BvhNode rightChild;

        /// <summary>Takes the given bounding volumes and constructs a bounding volume hierarchy.</summary>
        /// <param name="boundingVolumes">The bounding volumes to be processed into a hierarchy.</param>
        /// <returns>The root node of the bounding volume hierarchy.</returns>
        public static BvhNode Build(BvhBoundingVolume[] boundingVolumes)
        {
            var node = new BvhNode();

            // only one bounding volume remains so we are a leaf node.
            if (boundingVolumes.Length == 1)
            {
                node.boundingVolume = boundingVolumes[0];
                return node;
            }

            // find the splitting axis (e.g. the axis with the maximum extent).
            int splittingAxis = boundingVolumes[0].FindSplittingAxis(boundingVolumes);

            // Split the bounding volumes into two subsets.
            BvhBoundingVolume.SplitBoundingVolumes(boundingVolumes, splittingAxis, out BvhBoundingVolume[] leftSubset, out BvhBoundingVolume[] rightSubset);

            node.leftChild = Build(leftSubset);
            node.rightChild = Build(rightSubset);

            // Calculate the bounding volume for the current node (e.g., union of left and right children's bounding volumes).
            var aabb = new BvhBoundingVolume();
            node.boundingVolume = aabb;

            if (leftSubset.Length > 0)
                aabb.center = leftSubset[0].center;
            if (rightSubset.Length > 0)
                aabb.center = rightSubset[0].center;

            foreach (var child in leftSubset)
                aabb.Encapsulate(child);

            foreach (var child in rightSubset)
                aabb.Encapsulate(child);

            return node;
        }

        /// <summary>Find the nodes overlapping the specified world position.</summary>
        /// <param name="position">The world position to check for overlap.</param>
        /// <param name="nodes">A list to store the overlapping nodes.</param>
        public void FindNodesOverlappingPosition(float3 position, ICollection<BvhBoundingVolume> nodes)
        {
            // check if the current node's bounding volume intersects with the target position.
            if (boundingVolume.Contains(position))
            {
                // recursively search the left child node.
                if (leftChild != null)
                    leftChild.FindNodesOverlappingPosition(position, nodes);

                // recursively search the right child node.
                if (rightChild != null)
                    rightChild.FindNodesOverlappingPosition(position, nodes);

                // if this is a leaf node, add it to the result list.
                if (leftChild == null && rightChild == null)
                    nodes.Add(boundingVolume);
            }
        }
    }
}