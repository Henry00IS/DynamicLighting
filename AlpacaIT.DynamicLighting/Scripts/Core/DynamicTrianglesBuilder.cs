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
        /// <summary>
        /// The compression level for bounce lighting data. Choosing a higher compression can reduce
        /// VRAM usage, but may result in reduced visual quality. For best results, adjust based on
        /// your VRAM availability and visual preferences.
        /// </summary>
        private readonly DynamicBounceLightingCompressionMode bounceLightingCompression;

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
            public float[] bounceTexture;

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
        public DynamicTrianglesBuilder(MeshBuilder meshBuilder, int lightmapSize, DynamicBounceLightingCompressionMode bounceLightingCompression)
        {
            this.bounceLightingCompression = bounceLightingCompression;

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
            var lights = triangles[triangleIndex].lights;
            var lightsCount = lights.Count;
            for (int i = 0; i < lightsCount; i++)
                if (lights[i].dynamicLightIndex == raycastedLightIndex)
                    return true;
            return false;
        }

        public int TriangleGetRaycastedLightIndex(int triangleIndex, int raycastedLightIndex)
        {
            var lights = triangles[triangleIndex].lights;
            var lightsCount = lights.Count;
            for (int i = 0; i < lightsCount; i++)
                if (lights[i].dynamicLightIndex == raycastedLightIndex)
                    return i;
            return -1;
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
        public float[] GetBounceTexture(int triangleIndex, int lightIndex)
        {
            return triangles[triangleIndex].lights[lightIndex].bounceTexture;
        }

        /// <summary>Sets the bounce texture data for the specified light index in a triangle.</summary>
        /// <param name="triangleIndex">The triangle index in the mesh.</param>
        /// <param name="lightIndex">The triangle light index.</param>
        /// <param name="bounceTexture">The bounce texture data.</param>
        public void SetBounceTexture(int triangleIndex, int lightIndex, float[] bounceTexture)
        {
            triangles[triangleIndex].lights[lightIndex].bounceTexture = bounceTexture;
        }

        /// <summary>
        /// Builds an acceleration structure for the graphics card, containing pre-calculated
        /// raycasted light and shadow data for the scene.
        /// </summary>
        /// <param name="bounceLightingInScene">Whether bounce lighting was used in the scene.</param>
        /// <returns>The StructuredBuffer&lt;uint&gt; data for the graphics card.</returns>
        public List<uint> BuildDynamicTrianglesData(bool bounceLightingInScene)
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
                // |Bounce Data Offset 1| --> dynamic_lights[+2]              ONLY IF DYNAMIC_LIGHTING_BOUNCE ENABLED
                // +--------------------+
                // |Light Index 2       | --> dynamic_lights[Light Index 2]
                // +--------------------+
                // |Shadow Data Offset 2| --> dynamic_lights[+1]
                // +--------------------+
                // |Bounce Data Offset 2| --> dynamic_lights[+2]              ONLY IF DYNAMIC_LIGHTING_BOUNCE ENABLED
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

                    if (bounceLightingInScene)
                    {
                        // create a bounce data offset entry for every light.
                        // this will be filled out later.
                        buffer.Add(0);
                    }
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

                    if (bounceLightingInScene)
                    {
                        bufferTriangleOffset++; // Bounce Data Offset

                        // fill out the bounce data offset.
                        var bounceTexture = GetBounceTexture(triangleIndex, lightIndex);
                        if (bounceTexture != null)
                        {
                            switch (bounceLightingCompression)
                            {
                                case DynamicBounceLightingCompressionMode.EightBitsPerPixel:
                                    buffer.AddRange(CompressBounceLightingEightBitsPerPixel(bounceTexture));
                                    break;

                                case DynamicBounceLightingCompressionMode.SixBitsPerPixel:
                                    buffer.AddRange(CompressBounceLightingSixBitsPerPixel(bounceTexture));
                                    break;

                                case DynamicBounceLightingCompressionMode.FiveBitsPerPixel:
                                    buffer.AddRange(CompressBounceLightingFiveBitsPerPixel(bounceTexture));
                                    break;

                                case DynamicBounceLightingCompressionMode.FourBitsPerPixel:
                                    buffer.AddRange(CompressBounceLightingFourBitsPerPixel(bounceTexture));
                                    break;
                            }

                            buffer[(int)(bufferTriangleOffset)] = lightDataOffset;
                        }
                        else
                        {
                            buffer[(int)(bufferTriangleOffset)] = 0;
                        }
                    }
                    lightDataOffset = (uint)buffer.Count;

                    bufferTriangleOffset++; // Light Index
                }
            }

            return buffer;
        }

        /// <summary>
        /// Stores bounce lighting data with 4 pixels in 32-bit units on the graphics card, each
        /// pixel using 8-bit (0-255) depth.
        /// </summary>
        /// <param name="bounceTexture">The bounce lighting texture data to be compressed.</param>
        /// <returns>The 32-bit unsigned integer array for the graphics card.</returns>
        private uint[] CompressBounceLightingEightBitsPerPixel(float[] bounceTexture)
        {
            // calculate the number of uints needed to store all pixels on the graphics card.
            int numPixels = bounceTexture.Length;
            int numUInts = (numPixels + 3) / 4; // ceiling division to handle any remaining pixels.

            var bounceTexture32 = new uint[numUInts];

            // loop over each uint in the output array:
            for (int i = 0; i < numUInts; i++)
            {
                uint packed = 0;

                // loop over each of the four possible pixels in the current uint:
                for (int j = 0; j < 4; j++)
                {
                    int pixelIndex = i * 4 + j;

                    // check if the pixel index is within bounds:
                    if (pixelIndex < numPixels)
                    {
                        // get the grayscale value and clamp it between 0 and 1.
                        float color = bounceTexture[pixelIndex];
                        color = Mathf.Clamp01(color);

                        // store as non-linear color by square-root to store detail in darker shades.
                        var compressedColor = Mathf.Sqrt(color);

                        // convert the float to a byte (0-255).
                        byte byteValue = (byte)(compressedColor * 255f);

                        // shift the byte into the correct position and pack it into the uint.
                        packed |= ((uint)byteValue) << ((3 - j) * 8);
                    }
                }

                // store the packed uint in the output array.
                bounceTexture32[i] = packed;
            }

            return bounceTexture32;
        }

        /// <summary>
        /// Stores bounce lighting data with 5 pixels in 32-bit units on the graphics card, each
        /// pixel using 6-bit (0-63) depth. This reduces the amount of VRAM used by 20% (e.g., 4GiB
        /// becomes 3.2GiB). However, it may cause noticeable shading differences (color banding),
        /// which are softened by adding a slight noise pattern (dithering).
        /// </summary>
        /// <param name="bounceTexture">The bounce lighting texture data to be compressed.</param>
        /// <returns>The 32-bit unsigned integer array for the graphics card.</returns>
        private uint[] CompressBounceLightingSixBitsPerPixel(float[] bounceTexture)
        {
            // calculate the number of uints needed to store all pixels on the graphics card.
            int numPixels = bounceTexture.Length;
            int numUInts = (numPixels + 4) / 5; // ceiling division to handle any remaining pixels.

            var bounceTexture32 = new uint[numUInts];

            // loop over each uint in the output array:
            for (int i = 0; i < numUInts; i++)
            {
                uint packed = 0;

                // loop over each of the five possible pixels in the current uint:
                for (int j = 0; j < 5; j++)
                {
                    int pixelIndex = i * 5 + j;

                    // check if the pixel index is within bounds:
                    if (pixelIndex < numPixels)
                    {
                        // get the grayscale value and clamp it between 0 and 1.
                        float color = bounceTexture[pixelIndex];
                        color = Mathf.Clamp01(color);

                        // add dithering unless the color is extremely dark (black).
                        if (color > 0.001f)
                        {
                            float rng = color + UnityEngine.Random.Range(0.0f, 0.004f);
                            if (rng > 1.0f) rng = 1.0f;
                            color = rng;
                        }

                        // store as non-linear color by square-root to store detail in darker shades.
                        var compressedColor = Mathf.Sqrt(color);

                        // convert the float to 6 bits (0-63).
                        byte byteValue = (byte)(compressedColor * 63f);

                        // shift the byte into the correct position and pack it into the uint.
                        packed |= ((uint)byteValue) << ((3 - j) * 6);
                    }
                }

                // store the packed uint in the output array.
                bounceTexture32[i] = packed;
            }

            return bounceTexture32;
        }

        /// <summary>
        /// Stores bounce lighting data with 6 pixels in 32-bit units on the graphics card, each
        /// pixel using 5-bit (0-31) depth. This reduces the amount of VRAM used by 34% (e.g., 4GiB
        /// becomes 2.7GiB). However, it may cause noticeable shading differences (color banding),
        /// which are softened by adding a slight noise pattern (dithering).
        /// </summary>
        /// <param name="bounceTexture">The bounce lighting texture data to be compressed.</param>
        /// <returns>The 32-bit unsigned integer array for the graphics card.</returns>
        private uint[] CompressBounceLightingFiveBitsPerPixel(float[] bounceTexture)
        {
            // calculate the number of uints needed to store all pixels on the graphics card.
            int numPixels = bounceTexture.Length;
            int numUInts = (numPixels + 5) / 6; // ceiling division to handle any remaining pixels.

            var bounceTexture32 = new uint[numUInts];

            // loop over each uint in the output array:
            for (int i = 0; i < numUInts; i++)
            {
                uint packed = 0;

                // loop over each of the six possible pixels in the current uint:
                for (int j = 0; j < 6; j++)
                {
                    int pixelIndex = i * 6 + j;

                    // check if the pixel index is within bounds:
                    if (pixelIndex < numPixels)
                    {
                        // get the grayscale value and clamp it between 0 and 1.
                        float color = bounceTexture[pixelIndex];
                        color = Mathf.Clamp01(color);

                        // add dithering unless the color is extremely dark (black).
                        if (color > 0.001f)
                        {
                            float rng = color + UnityEngine.Random.Range(0.0f, 0.008f);
                            if (rng > 1.0f) rng = 1.0f;
                            color = rng;
                        }

                        // store as non-linear color by square-root to store detail in darker shades.
                        var compressedColor = Mathf.Sqrt(color);

                        // convert the float to 5 bits (0-31).
                        byte byteValue = (byte)(compressedColor * 31f);

                        // shift the byte into the correct position and pack it into the uint.
                        packed |= ((uint)byteValue) << ((3 - j) * 5);
                    }
                }

                // store the packed uint in the output array.
                bounceTexture32[i] = packed;
            }

            return bounceTexture32;
        }

        /// <summary>
        /// Stores bounce lighting data with 8 pixels in 32-bit units on the graphics card, each
        /// pixel using 4-bit (0-15) depth. This reduces the amount of VRAM used by 50% (e.g., 4GiB
        /// becomes 2GiB). However, it may cause noticeable shading differences (color banding),
        /// which are softened by adding a slight noise pattern (dithering).
        /// </summary>
        /// <param name="bounceTexture">The bounce lighting texture data to be compressed.</param>
        /// <returns>The 32-bit unsigned integer array for the graphics card.</returns>
        private uint[] CompressBounceLightingFourBitsPerPixel(float[] bounceTexture)
        {
            // calculate the number of uints needed to store all pixels on the graphics card.
            int numPixels = bounceTexture.Length;
            int numUInts = (numPixels + 7) / 8; // ceiling division to handle any remaining pixels.

            var bounceTexture32 = new uint[numUInts];

            // loop over each uint in the output array:
            for (int i = 0; i < numUInts; i++)
            {
                uint packed = 0;

                // loop over each of the eight possible pixels in the current uint:
                for (int j = 0; j < 8; j++)
                {
                    int pixelIndex = i * 8 + j;

                    // check if the pixel index is within bounds:
                    if (pixelIndex < numPixels)
                    {
                        // get the grayscale value and clamp it between 0 and 1.
                        float color = bounceTexture[pixelIndex];
                        color = Mathf.Clamp01(color);

                        // add dithering unless the color is extremely dark (black).
                        if (color > 0.001f)
                        {
                            float rng = color + UnityEngine.Random.Range(0.0f, 0.02f);
                            if (rng > 1.0f) rng = 1.0f;
                            color = rng;
                        }

                        // store as non-linear color by square-root to store detail in darker shades.
                        var compressedColor = Mathf.Sqrt(color);

                        // convert the float to 4 bits (0-15).
                        byte byteValue = (byte)(compressedColor * 15f);

                        // shift the byte into the correct position and pack it into the uint.
                        packed |= ((uint)byteValue) << ((3 - j) * 4);
                    }
                }

                // store the packed uint in the output array.
                bounceTexture32[i] = packed;
            }

            return bounceTexture32;
        }
    }
}