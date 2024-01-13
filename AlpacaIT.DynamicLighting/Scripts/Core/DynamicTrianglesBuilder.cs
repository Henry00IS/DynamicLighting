using System.Collections.Generic;
using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>
    /// Builds an acceleration structure for the graphics card, containing pre-calculated raycasted
    /// light and shadow data for the scene.
    /// </summary>
    internal class DynamicTrianglesBuilder
    {
        /// <summary>Represents a triangle of a static mesh in the scene.</summary>
        private class DtbTriangle
        {
            /// <summary>
            /// The collection of <see cref="DynamicLight"/> source indices in the <see
            /// cref="DynamicLightManager.raycastedDynamicLights"/> that affect this triangle.
            /// </summary>
            public List<uint> dynamicLightIndices = new List<uint>();
        }

        // <summary>
        // Represents an immovable raycasted <see cref="DynamicLight"/> source in the scene.
        // </summary>
        //private class DtbDynamicLight
        //{
        //}

        /// <summary>The collection of triangles in the static mesh in the scene.</summary>
        private List<DtbTriangle> triangles;

        /// <summary>Creates a new instance of <see cref="DynamicTrianglesBuilder"/>.</summary>
        /// <param name="triangleCount">The amount of triangles in the static mesh in the scene.</param>
        public DynamicTrianglesBuilder(int triangleCount)
        {
            // create triangle data for every triangle in the mesh.
            triangles = new List<DtbTriangle>(triangleCount);
            for (int i = 0; i < triangleCount; i++)
                triangles.Add(new DtbTriangle());
        }

        /// <summary>Associates the specified light index with a triangle.</summary>
        /// <param name="triangleIndex">The triangle index in the mesh.</param>
        /// <param name="lightIndex">The raycasted light index in the scene.</param>
        public void AssociateLightWithTriangle(int triangleIndex, int lightIndex)
        {
            // fixme: contains sucks for speed.
            if (!triangles[triangleIndex].dynamicLightIndices.Contains((uint)lightIndex))
                triangles[triangleIndex].dynamicLightIndices.Add((uint)lightIndex);
        }

        /// <summary>
        /// Associates the specified light index with a triangle (fast version- does not check
        /// whether the light index has already been associated with the triangle).
        /// </summary>
        /// <param name="triangleIndex">The triangle index in the mesh.</param>
        /// <param name="lightIndex">The raycasted light index in the scene.</param>
        public void AssociateLightWithTriangleFast(int triangleIndex, int lightIndex)
        {
            triangles[triangleIndex].dynamicLightIndices.Add((uint)lightIndex);
        }

        /// <summary>
        /// Gets the list of raycasted light indices associated with a triangle.
        /// <para>Do not modify this collection!</para>
        /// </summary>
        /// <param name="triangleIndex">The triangle index in the mesh.</param>
        /// <returns>The list of raycasted light indices in the scene.</returns>
        public List<uint> GetAssociatedLightIndices(int triangleIndex)
        {
            return triangles[triangleIndex].dynamicLightIndices;
        }

        /// <summary>
        /// Builds an acceleration structure for the graphics card, containing pre-calculated
        /// raycasted light and shadow data for the scene.
        /// </summary>
        /// <returns>The StructuredBuffer&lt;uint&gt; data for the graphics card.</returns>
        public List<uint> BuildDynamicTrianglesData()
        {
            var trianglesCount = triangles.Count;
            List<uint> buffer = new List<uint>(trianglesCount);

            // +---------------+     +-----------------+
            // |SV_PrimitiveID |--+->|Light Data Offset|
            // |(TriangleIndex)|  |  +-----------------+
            // +---------------+  +->|Light Data Offset|
            //                    |  +-----------------+
            //                    +->|...              |
            //                       +-----------------+

            // iterate over all of the triangles:
            for (int triangleIndex = 0; triangleIndex < trianglesCount; triangleIndex++)
            {
                // create a light data offset entry for every triangle.
                // this will be filled out later.
                buffer.Add(0);
            }

            //                       +------------------+
            // Light Data Offset --> |Light Count       |
            //                       +------------------+

            // iterate over all of the triangles:
            uint lightDataOffset = (uint)buffer.Count;
            for (int triangleIndex = 0; triangleIndex < trianglesCount; triangleIndex++)
            {
                var triangle = triangles[triangleIndex];

                // add the light count.
                var triangleDynamicLightIndicesCount = triangle.dynamicLightIndices.Count;
                buffer.Add((uint)triangleDynamicLightIndicesCount);

                // +------------------+
                // |Light Index 1     | --> dynamic_lights[Light Index 1]
                // +------------------+
                // |Light Index 2     | --> dynamic_lights[Light Index 2]
                // +------------------+
                // |Light Index ...   | --> dynamic_lights[Light Index ...]
                // +------------------+
                // |...               |
                // +------------------+

                // iterate over all of the associated light indices:
                for (int lightIndex = 0; lightIndex < triangleDynamicLightIndicesCount; lightIndex++)
                {
                    // add the light index.
                    buffer.Add(triangle.dynamicLightIndices[lightIndex]);
                }

                // fill out the light data offset.
                buffer[triangleIndex] = lightDataOffset;
                lightDataOffset = (uint)buffer.Count;
            }

            return buffer;
        }
    }
}