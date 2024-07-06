using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>
    /// Builds an acceleration structure for the graphics card, containing pre-calculated raycasted
    /// light and shadow data for the scene.
    /// </summary>
    internal class DynamicTrianglesBuilder
    {
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
            public Color[] bounceTexture;

            /// <summary>Creates a new light with the specified dynamic light index.</summary>
            /// <param name="dynamicLightIndex">The dynamic light source index.</param>
            public DtbTriangleLightData(uint dynamicLightIndex)
            {
                this.dynamicLightIndex = dynamicLightIndex;
                shadowOcclusionBits = null;
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
        public DynamicTrianglesBuilder(MeshBuilder meshBuilder, int lightmapSize)
        {
            // create triangle data for every triangle in the mesh.
            triangles = new List<DtbTriangle>(meshBuilder.triangleCount);
            for (int i = 0; i < meshBuilder.triangleCount; i++)
            {
                var (t1, t2, t3) = meshBuilder.GetTriangleUv1(i);

                // calculate the bounding box of the polygon in UV space.
                var triangleBoundingBox = MathEx.ComputeTriangleBoundingBox(t1, t2, t3);

                var minX = Mathf.FloorToInt(triangleBoundingBox.xMin * lightmapSize) - 2;
                var minY = Mathf.FloorToInt(triangleBoundingBox.yMin * lightmapSize) - 2;
                var maxX = Mathf.CeilToInt(triangleBoundingBox.xMax * lightmapSize) + 2;

                minX = Mathf.Clamp(minX, 0, lightmapSize - 1);
                minY = Mathf.Clamp(minY, 0, lightmapSize - 1);
                maxX = Mathf.Clamp(maxX, 0, lightmapSize - 1);

                triangles.Add(new DtbTriangle((uint)minX, (uint)minY, (uint)(1 + maxX - minX)));
            }
        }

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

        /// <summary>
        /// Checks whether a triangle has the specified light index associated with it.
        /// </summary>
        /// <param name="triangleIndex">The triangle index in the mesh.</param>
        /// <param name="raycastedLightIndex">The raycasted light index in the scene.</param>
        /// <returns>True when the light source has been associated with the triangle else false.</returns>
        public bool TriangleHasRaycastedLight(int triangleIndex, int raycastedLightIndex)
        {
            return triangles[triangleIndex].lights.FindIndex(a => a.dynamicLightIndex == raycastedLightIndex) != -1;
        }

        public int TriangleGetRaycastedLightIndex(int triangleIndex, int raycastedLightIndex)
        {
            return triangles[triangleIndex].lights.FindIndex(a => a.dynamicLightIndex == raycastedLightIndex);
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
        public Color[] GetBounceTexture(int triangleIndex, int lightIndex)
        {
            return triangles[triangleIndex].lights[lightIndex].bounceTexture;
        }

        /// <summary>Sets the bounce texture data for the specified light index in a triangle.</summary>
        /// <param name="triangleIndex">The triangle index in the mesh.</param>
        /// <param name="lightIndex">The triangle light index.</param>
        /// <param name="shadowBits">The bounce texture data.</param>
        public void SetBounceTexture(int triangleIndex, int lightIndex, Color[] bounceTexture)
        {
            triangles[triangleIndex].lights[lightIndex].bounceTexture = bounceTexture;
        }

        // packs a float into a byte so that 0.0 is 0 and +1.0 is 255.
        private uint saturated_float_to_byte(float value)
        {
            return (uint)(value * 255f);
        }

        private uint pack_saturated_float4_into_uint(float4 value)
        {
            value = math.saturate(value);
            uint x8 = saturated_float_to_byte(value.x);
            uint y8 = saturated_float_to_byte(value.y);
            uint z8 = saturated_float_to_byte(value.z);
            uint w8 = saturated_float_to_byte(value.w);
            uint combined = (x8 << 24) | (y8 << 16) | (z8 << 8) | w8;
            return combined;
        }

        /// <summary>
        /// Builds an acceleration structure for the graphics card, containing pre-calculated
        /// raycasted light and shadow data for the scene.
        /// </summary>
        /// <returns>The StructuredBuffer&lt;uint&gt; data for the graphics card.</returns>
        public List<uint> BuildDynamicTrianglesData()
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
                        //buffer.Add(0);
                        buffer[(int)(bufferTriangleOffset)] = 0;
                    }

                    lightDataOffset = (uint)buffer.Count;
                    bufferTriangleOffset++; // Bounce Data Offset

                    // fill out the bounce data offset.
                    var bounceTexture = GetBounceTexture(triangleIndex, lightIndex);
                    if (bounceTexture != null)
                    {
                        // convert colors to uint RGBA bytes of 0-255.
                        var bounceTexture32 = new uint[bounceTexture.Length];
                        for (int i = 0; i < bounceTexture32.Length; i++)
                        {
                            var color = bounceTexture[i];
                            bounceTexture32[i] = pack_saturated_float4_into_uint(new float4(color.r, color.g, color.b, color.a));
                        }

                        buffer.AddRange(bounceTexture32);
                        buffer[(int)(bufferTriangleOffset)] = lightDataOffset;
                        lightDataOffset = (uint)buffer.Count;
                    }
                    else
                    {
                        buffer[(int)(bufferTriangleOffset)] = 0;
                    }

                    bufferTriangleOffset++; // Light Index
                }
            }

            return buffer;
        }
    }
}