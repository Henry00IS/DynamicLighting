using System.Collections.Generic;
using UnityEngine;
using static AlpacaIT.DynamicLighting.DynamicLightingTracer;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>
    /// Builds an acceleration structure for the graphics card, containing pre-calculated raycasted
    /// light and shadow data for the scene.
    /// </summary>
    internal class DynamicTrianglesBuilder
    {
        /// <summary>Used to pass the lightmap index to the bounce lighting pass.</summary>
        public int lightmapIndex;

        private class DtbTriangleLightData
        {
            /// <summary>
            /// The dynamic light source index into the <see
            /// cref="DynamicLightManager.raycastedDynamicLights"/> that affects this triangle.
            /// </summary>
            public uint dynamicLightIndex;

            /// <summary>The collection of 1bpp shadow occlusion data.</summary>
            public BitArray2 shadowOcclusionBits;

            /// <summary>The collection of bounce texture data.</summary>
            public HighColorTexture bounceTexture;

            /// <summary>Creates a new light with the specified dynamic light index.</summary>
            /// <param name="dynamicLightIndex">The dynamic light source index.</param>
            public DtbTriangleLightData(uint dynamicLightIndex)
            {
                this.dynamicLightIndex = dynamicLightIndex;
            }
        }

        /// <summary>Represents a triangle of a static mesh in the scene.</summary>
        private class DtbTriangle
        {
            /// <summary>The x-position of the bounds of the triangle.</summary>
            public uint boundsX;
            /// <summary>The y-position of the bounds of the triangle.</summary>
            public uint boundsY;
            /// <summary>The width of the bounds of the triangle.</summary>
            public uint boundsW;

            /// <summary>
            /// The collection of <see cref="DtbTriangleLightData"/> that affects this triangle.
            /// </summary>
            public List<DtbTriangleLightData> lights;

            /// <summary>
            /// Creates a new triangle with the specified bounds and an empty lights collection.
            /// </summary>
            /// <param name="boundsX">The x-position of the bounds of the triangle.</param>
            /// <param name="boundsY">The y-position of the bounds of the triangle.</param>
            /// <param name="boundsW">The width of the bounds of the triangle.</param>
            public DtbTriangle(uint boundsX, uint boundsY, uint boundsW)
            {
                this.boundsX = boundsX;
                this.boundsY = boundsY;
                this.boundsW = boundsW;
                lights = new List<DtbTriangleLightData>();
            }
        }

        /// <summary>The collection of triangles in the static mesh in the scene.</summary>
        private List<DtbTriangle> triangles;

        /// <summary>Creates a new instance of <see cref="DynamicTrianglesBuilder"/>.</summary>
        /// <param name="triangleCount">The amount of triangles in the static mesh in the scene.</param>
        public DynamicTrianglesBuilder(ProcessMeshDataStep meshBuilder, int lightmapSize)
        {
            // create triangle data for every triangle in the mesh.
            triangles = new List<DtbTriangle>(meshBuilder.triangleCount);
            for (int i = 0; i < meshBuilder.triangleCount; i++)
            {
                // calculate the bounding box of the polygon in UV space.
                var triangleBoundingBox = meshBuilder.triangleUv1BoundingBoxes[i];

                var minX = triangleBoundingBox.xMin - 2;
                var minY = triangleBoundingBox.yMin - 2;
                var maxX = triangleBoundingBox.xMax + 2;

                minX = Mathf.Clamp(minX, 0, lightmapSize - 1);
                minY = Mathf.Clamp(minY, 0, lightmapSize - 1);
                maxX = Mathf.Clamp(maxX, 0, lightmapSize - 1);

                triangles.Add(new DtbTriangle((uint)minX, (uint)minY, (uint)(maxX - minX)));
            }
        }

        // todo: load this file from disk so that we can make adjustments during bouncing and then write it back to disk.

        /// <summary>Adds the specified raycasted light index to a triangle.</summary>
        /// <param name="triangleIndex">The triangle index in the mesh.</param>
        /// <param name="raycastedLightIndex">The raycasted light index in the scene.</param>
        public void AddRaycastedLightToTriangle(int triangleIndex, int raycastedLightIndex)
        {
            triangles[triangleIndex].lights.Add(new DtbTriangleLightData((uint)raycastedLightIndex));
        }

        /// <summary>Removes the specified light index from a triangle.</summary>
        /// <param name="triangleIndex">The triangle index in the mesh.</param>
        /// <param name="triangleLightIndex">The triangle light index.</param>
        public void RemoveLightFromTriangle(int triangleIndex, int triangleLightIndex)
        {
            triangles[triangleIndex].lights.RemoveAt(triangleLightIndex);
        }

        /// <summary>Gets the list of raycasted light indices associated with a triangle.</summary>
        /// <param name="triangleIndex">The triangle index in the mesh.</param>
        /// <returns>The list of raycasted light indices in the scene.</returns>
        public IReadOnlyList<uint> GetRaycastedLightIndices(int triangleIndex)
        {
            var result = new List<uint>();

            var lightsCount = triangles[triangleIndex].lights.Count;
            for (int i = 0; i < lightsCount; i++)
            {
                var lightData = triangles[triangleIndex].lights[i];
                result.Add(lightData.dynamicLightIndex);
            }

            return result;
        }

        /// <summary>Sets the shadow occlusion bits for the specified light index in a triangle.</summary>
        /// <param name="triangleIndex">The triangle index in the mesh.</param>
        /// <param name="lightIndex">The triangle light index.</param>
        /// <param name="shadowBits">The two-dimensional 1bpp shadow occlusion bits.</param>
        public void SetShadowOcclusionBits(int triangleIndex, int lightIndex, BitArray2 shadowBits)
        {
            triangles[triangleIndex].lights[lightIndex].shadowOcclusionBits = shadowBits;
        }

        /// <summary>Gets the shadow occlusion bits for the specified light index in a triangle.</summary>
        /// <param name="triangleIndex">The triangle index in the mesh.</param>
        /// <param name="lightIndex">The triangle light index.</param>
        /// <returns>The two-dimensional 1bpp shadow occlusion bits</returns>
        public BitArray2 GetShadowOcclusionBits(int triangleIndex, int lightIndex)
        {
            return triangles[triangleIndex].lights[lightIndex].shadowOcclusionBits;
        }

        /// <summary>Gets the bounce texture data for the specified light index in a triangle.</summary>
        /// <param name="triangleIndex">The triangle index in the mesh.</param>
        /// <param name="lightIndex">The triangle light index.</param>
        /// <returns>The bounce texture data.</returns>
        public HighColorTexture GetBounceTexture(int triangleIndex, int lightIndex)
        {
            return triangles[triangleIndex].lights[lightIndex].bounceTexture;
        }

        /// <summary>Sets the bounce texture data for the specified light index in a triangle.</summary>
        /// <param name="triangleIndex">The triangle index in the mesh.</param>
        /// <param name="lightIndex">The triangle light index.</param>
        /// <param name="shadowBits">The bounce texture data.</param>
        public void SetBounceTexture(int triangleIndex, int lightIndex, HighColorTexture bounceTexture)
        {
            triangles[triangleIndex].lights[lightIndex].bounceTexture = bounceTexture;
        }

        /// <summary>
        /// Builds an acceleration structure for the graphics card, containing pre-calculated
        /// raycasted light and shadow data for the scene.
        /// </summary>
        /// <returns>The StructuredBuffer&lt;uint&gt; data for the graphics card.</returns>
        public unsafe List<uint> BuildDynamicTrianglesData()
        {
            // +---------------+     +------------------+
            // |SV_PrimitiveID |--+->|Light Data Offset |
            // |(TriangleIndex)|  |  +------------------+
            // +---------------+  |  |Triangle Bounds X |
            //                    |  +------------------+
            //                    |  |Triangle Bounds Y |
            //                    |  +------------------+
            //                    |  |Triangle Bounds W |
            //                    |  +------------------+
            //                    +->|Light Data Offset |
            //                       +------------------+
            //                       |...               |
            //                       +------------------+
            int triangleHeaderSize = 4;

            var trianglesCount = triangles.Count;
            List<uint> buffer = new List<uint>(trianglesCount * triangleHeaderSize);

            // iterate over all of the triangles:
            for (int triangleIndex = 0; triangleIndex < trianglesCount; triangleIndex++)
            {
                var triangle = triangles[triangleIndex];

                // create a light data offset entry for every triangle.
                // this will be filled out later.
                buffer.Add(0);

                // add the triangle bounds used as the shadow data resolution.
                // pre-compute the 2px padding to prevent these calculations on the GPU.

                buffer.Add(triangle.boundsX - 2);
                buffer.Add(triangle.boundsY - 2);
                buffer.Add(triangle.boundsW + 4);
            }

            //                       +------------------+
            // Light Data Offset --> |Light Count       |
            //                       +------------------+

            // iterate over all of the triangles:
            uint lightDataOffset = (uint)buffer.Count;
            for (int triangleIndex = 0; triangleIndex < trianglesCount; triangleIndex++)
            {
                // add the light count.
                var triangleDynamicLightIndices = GetRaycastedLightIndices(triangleIndex);
                var triangleDynamicLightIndicesCount = triangleDynamicLightIndices.Count;
                buffer.Add((uint)triangleDynamicLightIndicesCount);

                // +--------------------+
                // |Light Index 1       | --> dynamic_lights[Light Index 1]
                // +--------------------+
                // |Shadow Data Offset 1| --> dynamic_lights[+1]
                // +--------------------+
                // |Bounce Data Offset 1| --> dynamic_lights[+2]
                // +--------------------+
                // |Light Index 2       | --> dynamic_lights[Light Index 2]
                // +--------------------+
                // |Shadow Data Offset 2| --> dynamic_lights[+1]
                // +--------------------+
                // |Bounce Data Offset 2| --> dynamic_lights[+2]
                // +--------------------+
                // |...                 |
                // +--------------------+

                // iterate over all of the associated light indices:
                for (int lightIndex = 0; lightIndex < triangleDynamicLightIndicesCount; lightIndex++)
                {
                    // add the raycasted light index.
                    buffer.Add(triangleDynamicLightIndices[lightIndex]);

                    // create a shadow data offset entry for every light.
                    // this will be filled out later.
                    buffer.Add(0);

                    // create a bounce data offset entry for every light.
                    // this will be filled out later.
                    buffer.Add(0);
                }

                // fill out the light data offset.
                buffer[triangleIndex * triangleHeaderSize] = lightDataOffset;
                lightDataOffset = (uint)buffer.Count;
            }

            // Shadow Data

            // iterate over all of the triangles:
            for (int triangleIndex = 0; triangleIndex < trianglesCount; triangleIndex++)
            {
                var bufferTriangleOffset = buffer[triangleIndex * triangleHeaderSize];

                // iterate over all of the associated light indices:
                var bufferLightCount = buffer[(int)bufferTriangleOffset++];
                for (int lightIndex = 0; lightIndex < bufferLightCount; lightIndex++)
                {
                    // fill out the shadow data offset.
                    bufferTriangleOffset++; // Shadow Data Offset

                    // add the shadow data (null if the triangle is fully illuminated).
                    var shadowOcclusionBits = GetShadowOcclusionBits(triangleIndex, lightIndex);
                    if (shadowOcclusionBits != null)
                    {
                        buffer.AddRange(GetShadowOcclusionBits(triangleIndex, lightIndex).ToUInt32Array());
                        buffer[(int)(bufferTriangleOffset)] = lightDataOffset;
                    }
                    else
                    {
                        // the shader can skip all shadow bits related work.
                        buffer.Add(0);
                        buffer[(int)(bufferTriangleOffset)] = 0;
                    }

                    lightDataOffset = (uint)buffer.Count;
                    bufferTriangleOffset++; // Bounce Data Offset

                    // fill out the bounce data offset.
                    var bounceTexture = GetBounceTexture(triangleIndex, lightIndex);
                    if (bounceTexture != null)
                    {
                        buffer.AddRange(bounceTexture.ToUInt32Array());
                        buffer[(int)(bufferTriangleOffset)] = lightDataOffset;
                        lightDataOffset = (uint)buffer.Count;
                    }

                    bufferTriangleOffset++; // Light Index
                }
            }

            return buffer;
        }
    }
}