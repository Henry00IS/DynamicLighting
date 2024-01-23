using AlpacaIT.DynamicLighting.Internal;
using System;
using UnityEngine;
using System.Collections.Generic;

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
                // terminate recursion.
                if (node->count <= 2)
                    return;

                // determine split axis and position.
                var extent = node->size;
                int axis = 0;
                if (extent.y > extent.x) axis = 1;
                if (extent.z > extent[axis]) axis = 2;
                float splitPos = node->aabbMin[axis] + extent[axis] * 0.5f;

                // in-place partition.
                int i = node->leftFirst;
                int j = i + node->count - 1;
                while (i <= j)
                {
                    if (dynamicLights[dynamicLightsIdx[i]].transform.position[axis] < splitPos)
                    {
                        i++;
                    }
                    else
                    {
                        (dynamicLightsIdx[j], dynamicLightsIdx[i]) = (dynamicLightsIdx[i], dynamicLightsIdx[j]);
                        j--;
                    }
                }

                // abort split if one of the sides is empty.
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

                // recurse.
                SubdivideNodeRecursive(leftChildIdx);
                SubdivideNodeRecursive(rightChildIdx);
            }
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
                Gizmos.DrawWireCube(node.center, node.size);

                /* if the current node is a leaf (has light indices): */
                if (node.isLeaf)
                {
                    /* process the light indices: */
                    for (int k = node.leftFirst; k < node.leftFirst + node.count; k++)
                    {
                        DynamicLight light = dynamicLights[dynamicLightsIdx[k]];

                        Gizmos.DrawSphere(light.transform.position, 0.2f);
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