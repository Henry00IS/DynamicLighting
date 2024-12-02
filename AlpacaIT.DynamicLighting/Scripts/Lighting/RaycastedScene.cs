using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>
    /// Stores compressed binary data for the scene used by <see cref="DynamicLightManager"/>. This
    /// includes the dynamic triangles data structure used by raycasted mesh renderers as well as
    /// the bounding volume hierarchy for dynamic geometry.
    /// </summary>
    [PreferBinarySerialization]
    internal class RaycastedScene : ScriptableObject
    {
        /// <summary>Serializable structure containing compressed <see cref="uint"/>-array data.</summary>
        [Serializable]
        internal struct CompressedUInt32
        {
            /// <summary>The compressed <see cref="uint"/> data as an array of bytes.</summary>
            public byte[] bytes;

            /// <summary>The decompressed original length of <see cref="bytes"/>.</summary>
            public int length;

            /// <summary>Decompresses the serialized bytes and returns the <see cref="uint[]"/>-array.</summary>
            /// <param name="result">The decompressed <see cref="uint"/>-array.</param>
            /// <returns>True on success else false.</returns>
            public readonly bool Read(out NativeArrayStream<uint> result)
            {
                result = default;

                // this can happen when the scriptable object has not been fully initialized.
                if (bytes == null || bytes.Length == 0)
                {
                    return false;
                }

                // legacy compressed data from an old version of dynamic lighting:
                if (length == 0)
                {
                    try
                    {
                        using (var decompressed = new MemoryStream())
                        {
                            using (var compressed = new NativeArrayStream<byte>(bytes))
                            using (var gzip = new GZipStream(compressed, CompressionMode.Decompress))
                            {
                                gzip.CopyTo(decompressed);
                            }
                            result = new NativeArrayStream<uint>(MemoryMarshal.Cast<byte, uint>(decompressed.ToArray()).ToArray());
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError(ex);
                        result = default;
                        return false;
                    }
                }

                try
                {
                    result = new NativeArrayStream<uint>(length);
                    using (var compressed = new NativeArrayStream<byte>(bytes))
                    using (var gzip = new GZipStream(compressed, CompressionMode.Decompress))
                    {
                        gzip.CopyTo(result);
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    result?.Dispose();
                    Debug.LogError(ex);
                    result = default;
                    return false;
                }
            }

            /// <summary>Compresses the given <see cref="uint"/>-array and stores it.</summary>
            /// <param name="input">The <see cref="uint"/>-array to be compressed and stored.</param>
            /// <returns>True on success else false.</returns>
            public bool Write(uint[] input)
            {
                // this can happen when the scriptable object has not been fully initialized.
                if (input == null)
                    return false;

                try
                {
                    length = input.Length * sizeof(uint);
                    using (var memory = new NativeArrayStream<uint>(input))
                    using (var compressed = new MemoryStream())
                    {
                        using (var gzip = new GZipStream(compressed, System.IO.Compression.CompressionLevel.Optimal))
                        {
                            memory.CopyTo(gzip);
                        }
                        bytes = compressed.ToArray();
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    length = 0;
                    Debug.LogError(ex);
                    return false;
                }
            }
        }

        /// <summary>The version number of this scriptable object data.</summary>
        [SerializeField]
        [HideInInspector]
        internal int version = 2; // v1 was with length == 0 and without native array stream.

        /// <summary>The collection of dynamic triangles data structures used by raycasted meshes.</summary>
        [SerializeField]
        [HideInInspector]
        internal List<CompressedUInt32> dynamicTriangles = new List<CompressedUInt32>();

        /// <summary>The bounding volume hierarchy containing all raycasted light sources.</summary>
        [SerializeField]
        [HideInInspector]
        internal CompressedUInt32 dynamicLightsBvh;

        /// <summary>The distance cubes for all raycasted light sources.</summary>
        [SerializeField]
        [HideInInspector]
        internal CompressedUInt32 dynamicLightsDistanceCubes;

        /// <summary>
        /// Attempts to store the given dynamic triangles data structure and returns the unique
        /// identifier to the data.
        /// </summary>
        /// <param name="dynamicTriangles">
        /// The dynamic triangles data that can be uploaded to the graphics card.
        /// </param>
        /// <returns>The index into the data or -1 on failure.</returns>
        public int StoreDynamicTriangles(uint[] dynamicTriangles)
        {
            // compress the dynamic triangles data structure.
            var data = new CompressedUInt32();
            if (!data.Write(dynamicTriangles))
                return -1;

            // write it into this serialized object and return the index into the data.
            int result = this.dynamicTriangles.Count;
            this.dynamicTriangles.Add(data);
            return result;
        }

        /// <summary>
        /// Decompresses the dynamic triangles data structure and returns the <see cref="uint[]"/>
        /// array to be uploaded to the graphics card on success.
        /// </summary>
        /// <param name="index">The index to find in the data (can be out of bounds).</param>
        /// <param name="dynamicTriangles">
        /// The dynamic triangles data that can be uploaded to the graphics card.
        /// </param>
        /// <returns>True on success else false (such as an invalid index).</returns>
        public bool ReadDynamicTriangles(int index, out NativeArrayStream<uint> dynamicTriangles)
        {
            if (index < 0 || index >= this.dynamicTriangles.Count)
            {
                dynamicTriangles = default;
                return false;
            }
            return this.dynamicTriangles[index].Read(out dynamicTriangles);
        }
    }
}