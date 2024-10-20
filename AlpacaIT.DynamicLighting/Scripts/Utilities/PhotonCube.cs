using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>
    /// Cubemap containing photon data of direct illumination used for a technique similar to photon mapping.
    /// </summary>
    internal unsafe class PhotonCube
    {
        /// <summary>Represents a pixel on a <see cref="PhotonCubeFace"/> as returned by the shader.</summary>
        public struct ShaderPhotonCubePixel // 8 bytes
        {
            /// <summary>The distance to the pixel.</summary>
            public float distance;

            /// <summary>The world-space normal of the pixel.</summary>
            public NormalFloat8Vector3 normal;

            /// <summary>These are 8 unused bits of padding.</summary>
            public byte _padding_unused;
        }

        /// <summary>One side of a <see cref="PhotonCube"/> containing pixel data.</summary>
        private class PhotonCubeFace
        {
            /// <summary>The compressed distances to each pixel on the cubemap face.</summary>
            public readonly NativeArray<ScaledAbsFloat16> distances;

            /// <summary>The compressed distances to each pixel on the cubemap face.</summary>
            public readonly ScaledAbsFloat16* distancesPtr;

            /// <summary>The compressed world-space normals of each pixel on the cubemap face.</summary>
            public readonly NativeArray<NormalFloat8Vector3> normals;

            /// <summary>The compressed world-space normals of each pixel on the cubemap face.</summary>
            public readonly NormalFloat8Vector3* normalsPtr;

            /// <summary>The width and height of each face of the photon cube in pixels.</summary>
            public readonly int size;

            /// <summary>The maximum radius of the light.</summary>
            public readonly float lightRadius;

            /// <summary>Whether the world-space normals should be stored.</summary>
            private readonly bool storeNormals;

            public PhotonCubeFace(NativeArray<ShaderPhotonCubePixel> shaderColors, Allocator allocator, int size, float lightRadius, bool storeNormals)
            {
                this.size = size;
                this.lightRadius = lightRadius;
                this.storeNormals = storeNormals;

                var shaderColorsPtr = (ShaderPhotonCubePixel*)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(shaderColors);
                var colorsArraySize = size * size;

                distances = new NativeArray<ScaledAbsFloat16>(colorsArraySize, allocator, NativeArrayOptions.UninitializedMemory);
                distancesPtr = (ScaledAbsFloat16*)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(distances);

                if (storeNormals)
                {
                    normals = new NativeArray<NormalFloat8Vector3>(colorsArraySize, allocator, NativeArrayOptions.UninitializedMemory);
                    normalsPtr = (NormalFloat8Vector3*)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(normals);

                    // compress the shader data.
                    for (int i = 0; i < colorsArraySize; i++)
                    {
                        distancesPtr[i] = new ScaledAbsFloat16(shaderColorsPtr[i].distance, lightRadius);
                        normalsPtr[i] = shaderColorsPtr[i].normal;
                    }
                }
                else
                {
                    // compress the shader data.
                    for (int i = 0; i < colorsArraySize; i++)
                    {
                        distancesPtr[i] = new ScaledAbsFloat16(shaderColorsPtr[i].distance, lightRadius);
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private int Index(Vector2 position)
            {
                position.x = 1.0f - position.x;
                var x = (int)(position.x * size);
                var y = (int)(position.y * size);
                var sizeMinusOne = size - 1;
                if (x < 0) x = 0; else if (x > sizeMinusOne) x = sizeMinusOne;
                if (y < 0) y = 0; else if (y > sizeMinusOne) y = sizeMinusOne;
                return y * size + x;
            }

            public float SampleDistance(Vector2 position)
            {
                return distancesPtr[Index(position)].ToFloat(lightRadius);
            }

            public Vector3 SampleNormal(Vector2 position)
            {
                return normalsPtr[Index(position)];
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static int Index(Vector2 position, int size)
            {
                position.x = 1.0f - position.x;
                var x = (int)(position.x * size);
                var y = (int)(position.y * size);
                var sizeMinusOne = size - 1;
                if (x < 0) x = 0; else if (x > sizeMinusOne) x = sizeMinusOne;
                if (y < 0) y = 0; else if (y > sizeMinusOne) y = sizeMinusOne;
                return y * size + x;
            }

            public void Dispose()
            {
                if (distances.IsCreated)
                    distances.Dispose();

                if (storeNormals && normals.IsCreated)
                    normals.Dispose();
            }
        }

        /// <summary>The six faces of the photon cube.</summary>
        private readonly PhotonCubeFace[] faces = new PhotonCubeFace[6];

        /// <summary>The width and height of each face of the photon cube in pixels.</summary>
        private readonly int size;

        /// <summary>The maximum radius of the light.</summary>
        private readonly float lightRadius;

        /// <summary>Whether the normals should be stored.</summary>
        private readonly bool storeNormals;

        /// <summary>
        /// Creates a new <see cref="PhotonCube"/> instance that copies the data from a cubemap <see
        /// cref="RenderTexture"/> into memory.
        /// </summary>
        /// <param name="cubemapRenderTexture">
        /// The cubemap <see cref="RenderTexture"/> with a <see cref="RenderTexture.volumeDepth"/>
        /// of 6 faces.
        /// </param>
        /// <param name="lightRadius">The maximum radius of the light.</param>
        /// <param name="storeNormals">Whether the world-space normals should be stored.</param>
        public PhotonCube(RenderTexture cubemapRenderTexture, float lightRadius, bool storeNormals)
        {
            // validate the arguments to prevent any errors.
            if (cubemapRenderTexture == null) throw new System.ArgumentNullException(nameof(cubemapRenderTexture));
            if (cubemapRenderTexture.dimension != TextureDimension.Cube) throw new System.ArgumentException("The render texture must have the dimension set to cube.", nameof(cubemapRenderTexture));
            if (cubemapRenderTexture.volumeDepth != 6) throw new System.ArgumentException("The render texture for photon cubes must have 6 faces.", nameof(cubemapRenderTexture));

            this.lightRadius = lightRadius;
            this.storeNormals = storeNormals;

            // remember the size of the cubemap texture in pixels.
            size = cubemapRenderTexture.width;

#if UNITY_2021_3_OR_NEWER && !UNITY_2021_3_0 && !UNITY_2021_3_1 && !UNITY_2021_3_2 && !UNITY_2021_3_3 && !UNITY_2021_3_4 && !UNITY_2021_3_5 && !UNITY_2021_3_6 && !UNITY_2021_3_7 && !UNITY_2021_3_8 && !UNITY_2021_3_9 && !UNITY_2021_3_10 && !UNITY_2021_3_11 && !UNITY_2021_3_12 && !UNITY_2021_3_13 && !UNITY_2021_3_14 && !UNITY_2021_3_15 && !UNITY_2021_3_16 && !UNITY_2021_3_17 && !UNITY_2021_3_18 && !UNITY_2021_3_19 && !UNITY_2021_3_20 && !UNITY_2021_3_21 && !UNITY_2021_3_22 && !UNITY_2021_3_23 && !UNITY_2021_3_24 && !UNITY_2021_3_25 && !UNITY_2021_3_26 && !UNITY_2021_3_27
            var photonCameraRenderTextureDescriptor = new RenderTextureDescriptor(size, size, RenderTextureFormat.RGFloat, 16, 0, RenderTextureReadWrite.Linear);
#else
            var photonCameraRenderTextureDescriptor = new RenderTextureDescriptor(size, size, RenderTextureFormat.RGFloat, 16, 0);
#endif
            photonCameraRenderTextureDescriptor.autoGenerateMips = false;

            // extract the 6 sides of the cubemap:
            var rt = RenderTexture.GetTemporary(photonCameraRenderTextureDescriptor);
            rt.filterMode = FilterMode.Point;
            var readableTexture = new Texture2D(size, size, TextureFormat.RGFloat, false, true);
            readableTexture.filterMode = FilterMode.Point;
            for (int face = 0; face < 6; face++)
            {
                Graphics.CopyTexture(cubemapRenderTexture, face, 0, rt, 0, 0);
                RenderTexture.active = rt;
                readableTexture.ReadPixels(new Rect(0, 0, size, size), 0, 0);
                readableTexture.Apply();
                var pixels = readableTexture.GetPixelData<ShaderPhotonCubePixel>(0);
                faces[face] = new PhotonCubeFace(pixels, Allocator.Temp, size, lightRadius, storeNormals);
                pixels.Dispose(); // does nothing.
                RenderTexture.active = null;
            }
            Object.DestroyImmediate(readableTexture);
            RenderTexture.ReleaseTemporary(rt);
        }

        public void Dispose()
        {
            for (int i = 0; i < faces.Length; i++)
                faces[i].Dispose();
        }

        /// <summary>
        /// Samples a cubemap like a shader would and returns the face index to sample with the coordinates.
        /// <para>https://www.gamedev.net/forums/topic/687535-implementing-a-cube-map-lookup-function/</para>
        /// </summary>
        /// <param name="direction">The direction to sample the cubemap in.</param>
        /// <param name="face">The face index to be sampled from.</param>
        /// <returns>The UV coordinates to sample the cubemap at.</returns>
        public static unsafe Vector2 GetFaceUvByDirection(Vector3 direction, out int face)
        {
            var vAbs = direction;
            UMath.Abs(&vAbs);
            float ma;
            Vector2 uv; _ = &uv;
            if (vAbs.z >= vAbs.x && vAbs.z >= vAbs.y)
            {
                face = direction.z < 0.0f ? 5 : 4;
                ma = 0.5f / vAbs.z;
                uv.x = direction.z < 0.0f ? -direction.x : direction.x;
                uv.y = -direction.y;
            }
            else if (vAbs.y >= vAbs.x)
            {
                face = direction.y < 0.0f ? 3 : 2;
                ma = 0.5f / vAbs.y;
                uv.x = direction.x;
                uv.y = direction.y < 0.0f ? -direction.z : direction.z;
            }
            else
            {
                face = direction.x < 0.0f ? 1 : 0;
                ma = 0.5f / vAbs.x;
                uv.x = direction.x < 0.0f ? direction.z : -direction.z;
                uv.y = -direction.y;
            }
            UMath.Scale(&uv, ma);
            UMath.Add(&uv, 0.5f);
            return uv;//uv * ma + 0.5f;
        }

        /// <summary>
        /// Computes the direction vector from a given UV coordinate and face index of a cubemap.
        /// </summary>
        /// <param name="uv">The UV coordinates to sample the cubemap at.</param>
        /// <param name="face">The face index to be sampled from.</param>
        /// <returns>The direction vector corresponding to the UV and face index.</returns>
        public static Vector3 GetDirectionByFaceUv(Vector2 uv, int face)
        {
            // adjust uv coordinates from [0,1] to [-1,1].
            uv = 2.0f * uv;
            uv.x -= 1.0f;
            uv.y -= 1.0f;

            Vector3 direction = default;
            switch (face)
            {
                case 0: direction = new Vector3(1.0f, -uv.y, -uv.x); break;  // +X
                case 1: direction = new Vector3(-1.0f, -uv.y, uv.x); break;  // -X
                case 2: direction = new Vector3(uv.x, 1.0f, uv.y); break;    // +Y
                case 3: direction = new Vector3(uv.x, -1.0f, -uv.y); break;  // -Y
                case 4: direction = new Vector3(uv.x, -uv.y, 1.0f); break;   // +Z
                case 5: direction = new Vector3(-uv.x, -uv.y, -1.0f); break; // -Z
            }

            // normalize the direction vector to ensure it's a unit vector.
            return Vector3.Normalize(direction);
        }

        /// <summary>Does required prerequisite computation to access the fast-methods.</summary>
        /// <param name="direction">The direction to sample the cubemap in.</param>
        /// <param name="photonCubeFace">The face index of the cubemap.</param>
        /// <param name="photonCubeFaceIndex">The index into the cubemap face data arrays.</param>
        public void FastSamplePrerequisite(Vector3 direction, out int photonCubeFace, out int photonCubeFaceIndex)
        {
            var uv = GetFaceUvByDirection(direction, out photonCubeFace);
            photonCubeFaceIndex = PhotonCubeFace.Index(uv, size);
        }

        /// <summary>Gets the distance to the closest fragment in the given direction.</summary>
        /// <param name="direction">The direction to sample the cubemap in.</param>
        /// <returns>The distance to the fragment.</returns>
        public float SampleDistance(Vector3 direction)
        {
            var uv = GetFaceUvByDirection(direction, out var face);
            return faces[face].SampleDistance(uv);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float SampleDistanceFast(int photonCubeFace, int photonCubeFaceIndex)
        {
            return faces[photonCubeFace].distancesPtr[photonCubeFaceIndex].ToFloat(lightRadius);
        }

        /// <summary>Gets an approximate normal of the closest fragment in the given direction.</summary>
        /// <param name="direction">The direction to sample the cubemap in.</param>
        /// <returns>The normal of the fragment.</returns>
        public Vector3 SampleNormal(Vector3 direction)
        {
            var uv = GetFaceUvByDirection(direction, out var face);
            return faces[face].SampleNormal(uv);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3 SampleNormalFast(int photonCubeFace, int photonCubeFaceIndex)
        {
            // unpack normalfloat8vector3 manually for a big performance gain:
            Vector3 normal; _ = &normal;
            var bla = &faces[photonCubeFace].normalsPtr[photonCubeFaceIndex];
            normal.x = bla->x.byteValue * (2.0f / 254.0f) - 1.0f;
            normal.y = bla->y.byteValue * (2.0f / 254.0f) - 1.0f;
            normal.z = bla->z.byteValue * (2.0f / 254.0f) - 1.0f;
            return normal;
        }

        /// <summary>
        /// Gets an approximate world position of the closest fragment in the given direction.
        /// </summary>
        /// <param name="direction">The direction to sample the cubemap in.</param>
        /// <param name="lightPosition">The light position in world-space coordinates.</param>
        /// <returns>The world position of the fragment.</returns>
        public Vector3 SampleWorld(Vector3 direction, Vector3 lightPosition)
        {
            return lightPosition + direction * SampleDistance(direction);
        }

        public unsafe Vector3 SampleWorldFast(Vector3 direction, Vector3 lightPosition, int photonCubeFace, int photonCubeFaceIndex)
        {
            // return lightPosition + direction * SampleDistanceFast(photonCubeFace, photonCubeFaceIndex);
            // using 'direction' as scratch buffer.
            UMath.Scale(&direction, SampleDistanceFast(photonCubeFace, photonCubeFaceIndex));
            UMath.Add(&direction, &lightPosition);
            return direction;
        }

        /// <summary>
        /// Gets software rendered real-time shadows returning true when the given fragment world
        /// position is in the light else false.
        /// </summary>
        /// <param name="lightPosition">The light position in world-space coordinates.</param>
        /// <param name="worldPosition">The fragment position in world-space coordinates.</param>
        /// <param name="worldNormal">The normal of the fragment (e.g. triangle normal).</param>
        /// <returns>True when the fragment position is in light else false.</returns>
        public unsafe bool SampleShadow(Vector3 lightPosition, Vector3 worldPosition, Vector3 worldNormal)
        {
            // calculate the unnormalized direction between the light source and the fragment.
            // [unsafe] Vector3 light_direction = lightPosition - worldPosition;
            Vector3* light_direction = &lightPosition;
            UMath.Subtract(&lightPosition, &worldPosition);

            // calculate the square distance between the light source and the fragment.
            // distance(i.world, light.position); but squared to prevent a square root.
            float light_distanceSqr = UMath.SqrMagnitude(light_direction);

            // a simple dot product with the normal gives us diffusion.
            float NdotL = Mathf.Max(Vector3.Dot(worldNormal, *light_direction), 0);

            // magic bias function! it is amazing!
            float light_distance = Mathf.Sqrt(light_distanceSqr);
            float magic = 0.02f + 0.01f * light_distance;
            float autobias = magic * Mathf.Tan(Mathf.Acos(1.0f - NdotL));
            autobias = Mathf.Clamp(autobias, 0.0f, magic);

            // negative!
            UMath.Negate(light_direction);
            float shadow_mapping_distance = SampleDistance(*light_direction);

            // when the fragment is occluded we can early out here.
            if (light_distance - autobias > shadow_mapping_distance)
                return false;

            return true;
        }

        /// <summary>
        /// Gets software rendered real-time shadows returning true when the given fragment by
        /// direction and distance is in the light else false.
        /// </summary>
        /// <param name="lightDirection">The direction between the light source and the fragment.</param>
        /// <param name="lightDistanceToWorld">The distance between the light source and the fragment.</param>
        /// <param name="worldNormal">The normal of the fragment (e.g. triangle normal).</param>
        /// <returns>True when the fragment position is in light else false.</returns>
        public unsafe bool SampleShadow(Vector3 lightDirection, float lightDistanceToWorld, Vector3 worldNormal)
        {
            // calculate the square distance between the light source and the fragment.
            float light_distanceSqr = lightDistanceToWorld * lightDistanceToWorld;

            // a simple dot product with the normal gives us diffusion.
            float NdotL = Mathf.Max(Vector3.Dot(worldNormal, lightDirection), 0);

            // magic bias function! it is amazing!
            float light_distance = lightDistanceToWorld;
            float magic = 0.02f + 0.01f * light_distance;
            float autobias = magic * Mathf.Tan(Mathf.Acos(1.0f - NdotL));
            autobias = Mathf.Clamp(autobias, 0.0f, magic);

            // negative!
            UMath.Negate(&lightDirection);
            float shadow_mapping_distance = SampleDistance(lightDirection);

            // when the fragment is occluded we can early out here.
            if (light_distance - autobias > shadow_mapping_distance)
                return false;

            return true;
        }
    }
}