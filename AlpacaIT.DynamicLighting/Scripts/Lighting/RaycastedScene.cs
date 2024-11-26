using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
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

            /// <summary>Decompresses the serialized bytes and returns the <see cref="uint[]"/>-array.</summary>
            /// <param name="result">The decompressed <see cref="uint"/>-array.</param>
            /// <returns>True on success else false.</returns>
            public readonly bool Read(out uint[] result)
            {
                // this can happen when the scriptable object has not been fully initialized.
                if (bytes == null || bytes.Length == 0)
                {
                    result = default;
                    return false;
                }

                try
                {
                    byte[] decompressedBytes;
                    using (var memory = new MemoryStream(bytes))
                    using (var decompressed = new MemoryStream())
                    using (var gzip = new GZipStream(memory, CompressionMode.Decompress))
                    {
                        gzip.CopyTo(decompressed);
                        gzip.Close();
                        decompressedBytes = decompressed.ToArray();
                    }

                    uint[] uintArray = new uint[decompressedBytes.Length / 4];
                    Buffer.BlockCopy(decompressedBytes, 0, uintArray, 0, decompressedBytes.Length);
                    result = uintArray;

                    return true;
                }
                catch (Exception ex)
                {
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
                    byte[] byteArray = new byte[input.Length * 4];
                    Buffer.BlockCopy(input, 0, byteArray, 0, input.Length * 4);

                    using (var memory = new MemoryStream(byteArray))
                    using (var compressed = new MemoryStream())
                    using (var gzip = new GZipStream(compressed, System.IO.Compression.CompressionLevel.Optimal))
                    {
                        memory.CopyTo(gzip);
                        gzip.Close();
                        bytes = compressed.ToArray();
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    Debug.LogError(ex);
                    return false;
                }
            }
        }

        /// <summary>The version number of this scriptable object data.</summary>
        [SerializeField]
        [HideInInspector]
        internal int version = 1;

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
        public bool ReadDynamicTriangles(int index, out uint[] dynamicTriangles)
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