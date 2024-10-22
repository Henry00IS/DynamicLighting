using AlpacaIT.DynamicLighting.Internal;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    // special thanks to jbikker https://jacco.ompf2.com/2022/04/13/how-to-build-a-bvh-part-1-basics/

    /// <summary>
    /// Bounding Volume Hierarchy (BVH) acceleration structure. The structure can be passed to the
    /// graphics card to quickly find all objects overlapping at a specific position.
    /// <para>Used with <see cref="DynamicLight"/> to accelerate rendering of non-static <see cref="MeshRenderer"/>.</para>
    /// </summary>
    internal class BvhLightStructure
    {
        private DynamicLight[] dynamicLights;
        public List<int> dynamicLightsIdx;

        private BvhLightNode[] nodes;
        private int rootNodeIdx = 0, nodesUsed = 1;

        public unsafe BvhLightStructure(DynamicLight[] dynamicLights)
        {
            this.dynamicLights = dynamicLights;
            nodes = new BvhLightNode[dynamicLights.Length * 2];

            // populate light index array.
            dynamicLightsIdx = new List<int>(dynamicLights.Length);
            for (int i = 0; i < dynamicLights.Length; i++)
                dynamicLightsIdx.Add(i);

            // assign all lights to the root node.
            fixed (BvhLightNode* root = &nodes[rootNodeIdx])
            {
                root->leftFirst = 0;
                root->count = dynamicLights.Length;
            }

            // update the root node bounds.
            UpdateNodeBounds(rootNodeIdx);

            // subdivide recursively.
            SubdivideNodeRecursive(rootNodeIdx);

            // free up memory.
            //dynamicLights = null;
            //dynamicLightsIdx = null;
        }

        /// <summary>
        /// Updates the <see cref="BvhLightNode.aabbMin"/> and <see cref="BvhLightNode.aabbMax"/> to
        /// encompass all light sources contained within.
        /// </summary>
        /// <param name="index">The index of the node into the <see cref="nodes"/> array.</param>
        private unsafe void UpdateNodeBounds(int index)
        {
            fixed (BvhLightNode* node = &nodes[index])
            {
                node->aabbMin = Vector3.positiveInfinity;
                node->aabbMax = Vector3.negativeInfinity;
                for (int first = node->leftFirst, i = 0; i < node->count; i++)
                {
                    int leafLightIdx = dynamicLightsIdx[first + i];
                    var light = dynamicLights[leafLightIdx];
                    var bounds = MathEx.GetSphereBounds(light.transform.position, light.lightRadius);
                    node->aabbMin = Vector3.Min(node->aabbMin, bounds.min);
                    node->aabbMax = Vector3.Max(node->aabbMax, bounds.max);
                }
            }
        }

        /// <summary>Subdivides the node at the specified index recursively.</summary>
        /// <param name="index">The index of the node into the <see cref="nodes"/> array.</param>
        private unsafe void SubdivideNodeRecursive(int index)
        {
            fixed (BvhLightNode* node = &nodes[index])
            {
                // terminate recursion if the node has 2 or fewer lights.
                if (node->count <= 2)
                    return;

                // compute parent cost.
                Vector3 e = node->aabbMax - node->aabbMin; // extent of parent
                float parentArea = 2 * (e.x * e.y + e.y * e.z + e.z * e.x);
                float parentCost = node->count * parentArea;

                int bestAxis = -1;
                float bestPos = 0;
                float bestCost = float.MaxValue;

                // try splitting along each axis at the position of each light.
                for (int axis = 0; axis < 3; axis++)
                {
                    for (int k = 0; k < node->count; k++)
                    {
                        int lightIdx = dynamicLightsIdx[node->leftFirst + k];
                        var light = dynamicLights[lightIdx];
                        float candidatePos = light.transform.position[axis];

                        float cost = EvaluateSAH(node, axis, candidatePos);

                        if (cost < bestCost)
                        {
                            bestCost = cost;
                            bestAxis = axis;
                            bestPos = candidatePos;
                        }
                    }
                }

                // abort split if no valid split was found or if the best cost is not better than parent cost.
                if (bestAxis == -1 || bestCost >= parentCost)
                    return;

                // partition the lights using the best axis and position.
                float splitPos = bestPos;

                int i = node->leftFirst;
                int j = i + node->count - 1;
                while (i <= j)
                {
                    int lightIdx = dynamicLightsIdx[i];
                    var light = dynamicLights[lightIdx];
                    if (light.transform.position[bestAxis] < splitPos)
                    {
                        i++;
                    }
                    else
                    {
                        (dynamicLightsIdx[j], dynamicLightsIdx[i]) = (dynamicLightsIdx[i], dynamicLightsIdx[j]);
                        j--;
                    }
                }

                int leftCount = i - node->leftFirst;
                if (leftCount == 0 || leftCount == node->count)
                    return;

                // create child nodes.
                int leftChildIdx = nodesUsed++;
                int rightChildIdx = nodesUsed++;
                nodes[leftChildIdx].leftFirst = node->leftFirst;
                nodes[leftChildIdx].count = leftCount;
                nodes[rightChildIdx].leftFirst = i;
                nodes[rightChildIdx].count = node->count - leftCount;
                node->leftFirst = leftChildIdx;
                node->count = 0;
                UpdateNodeBounds(leftChildIdx);
                UpdateNodeBounds(rightChildIdx);

                // Recurse.
                SubdivideNodeRecursive(leftChildIdx);
                SubdivideNodeRecursive(rightChildIdx);
            }
        }

        /// <summary>
        /// Evaluates the SAH cost for splitting the node at the specified axis and position.
        /// </summary>
        /// <param name="node">The node to split.</param>
        /// <param name="axis">The axis along which to split (0,1,2).</param>
        /// <param name="pos">The position along the axis at which to split.</param>
        /// <returns>The SAH cost of the split.</returns>
        private unsafe float EvaluateSAH(BvhLightNode* node, int axis, float pos)
        {
            int leftCount = 0;
            int rightCount = 0;
            Vector3 leftAabbMin = Vector3.positiveInfinity;
            Vector3 leftAabbMax = Vector3.negativeInfinity;
            Vector3 rightAabbMin = Vector3.positiveInfinity;
            Vector3 rightAabbMax = Vector3.negativeInfinity;

            for (int i = 0; i < node->count; i++)
            {
                int lightIdx = dynamicLightsIdx[node->leftFirst + i];
                var light = dynamicLights[lightIdx];
                float lightPos = light.transform.position[axis];

                var bounds = MathEx.GetSphereBounds(light.transform.position, light.lightRadius);

                if (lightPos < pos)
                {
                    leftCount++;
                    leftAabbMin = Vector3.Min(leftAabbMin, bounds.min);
                    leftAabbMax = Vector3.Max(leftAabbMax, bounds.max);
                }
                else
                {
                    rightCount++;
                    rightAabbMin = Vector3.Min(rightAabbMin, bounds.min);
                    rightAabbMax = Vector3.Max(rightAabbMax, bounds.max);
                }
            }

            // if either side is empty, return a large cost to avoid this split.
            if (leftCount == 0 || rightCount == 0)
                return float.MaxValue;

            // compute surface areas.
            Vector3 leftExtent = leftAabbMax - leftAabbMin;
            float leftArea = 2 * (leftExtent.x * leftExtent.y + leftExtent.y * leftExtent.z + leftExtent.z * leftExtent.x);

            Vector3 rightExtent = rightAabbMax - rightAabbMin;
            float rightArea = 2 * (rightExtent.x * rightExtent.y + rightExtent.y * rightExtent.z + rightExtent.z * rightExtent.x);

            // compute cost.
            float cost = (leftCount * leftArea) + (rightCount * rightArea);

            return cost;
        }

        /// <summary>
        /// Converts this <see cref="BvhLightStructure"/> into a <see cref="uint"/> array.
        /// </summary>
        /// <returns>The array of uint containing the data.</returns>
        public uint[] ToUInt32Array()
        {
            var byteArray = Utilities.StructArrayToByteArray(nodes);

            uint[] uintArray = new uint[byteArray.Length / 4];

            Buffer.BlockCopy(byteArray, 0, uintArray, 0, byteArray.Length);
            return uintArray;
        }

        #region Debug Stuff

        /// <summary>
        /// Loads a <see cref="BvhLightStructure"/> from the given <see cref="uint"/> array.
        /// </summary>
        /// <param name="data">The data to be loaded.</param>
        public BvhLightStructure(uint[] data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (data.Length == 0) throw new ArgumentException(nameof(data), "The data can not be an empty array.");

            byte[] byteArray = new byte[data.Length * 4];
            Buffer.BlockCopy(data, 0, byteArray, 0, data.Length * 4);

            nodes = Utilities.ByteArrayToStructArray<BvhLightNode>(byteArray);
        }

        public void DebugTraverseDraw(Vector3 position)
        {
            /* we traverse the bounding volume hierarchy starting at the root node: */
            BvhLightNode[] stack = new BvhLightNode[64];
            uint stackPointer = 0;
            BvhLightNode node = nodes[0];

            while (true)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawWireCube(node.center, node.size);

                /* if the current node is a leaf (has light indices): */
                if (node.isLeaf)
                {
                    /* process the light indices: */
                    for (int k = node.leftFirst; k < node.leftFirst + node.count; k++)
                    {
                        DynamicLight light = dynamicLights[dynamicLightsIdx[k]];

                        Gizmos.color = light.lightColorAdjusted;
                        Gizmos.DrawWireSphere(light.transform.position, light.lightRadius);
                    }

                    /* check whether we are done traversing the bvh: */
                    if (stackPointer == 0) break; else node = stack[--stackPointer];
                    continue;
                }

                /* find the left and right child node. */
                BvhLightNode left = nodes[node.leftFirst];
                BvhLightNode right = nodes[node.rightNode];

                if (new Bounds(left.center, left.size).Contains(position))
                    stack[stackPointer++] = left;

                if (new Bounds(right.center, right.size).Contains(position))
                    stack[stackPointer++] = right;

                if (stackPointer == 0) break; else node = stack[--stackPointer];
            }
        }

        #endregion Debug Stuff
    }
}