using System;
using System.IO;
using System.IO.Compression;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AlpacaIT.DynamicLighting
{
    public class RuntimeUtilities
    {
        public static bool ReadLightmapData(int identifier, out uint[] pixels, out Vector3[] pixels_world)
        {
            try
            {
                string scene = Path.GetFileNameWithoutExtension(SceneManager.GetActiveScene().path);

                {
                    var lightmap = Resources.Load<TextAsset>(scene + "-Lightmap" + identifier);
                    if (lightmap == null)
                    {
                        Debug.LogError("Cannot find '" + scene + "-Lightmap" + identifier + ".bytes" + "'!");
                        pixels = null;
                        pixels_world = null;
                        return false;
                    }
                    var byteArray = lightmap.bytes;

                    using (var memory = new MemoryStream(byteArray))
                    using (var decompressed = new MemoryStream())
                    using (var gzip = new GZipStream(memory, CompressionMode.Decompress))
                    {
                        gzip.CopyTo(decompressed);
                        gzip.Close();
                        byteArray = decompressed.ToArray();
                    }

                    uint[] uintArray = new uint[byteArray.Length / 4];
                    Buffer.BlockCopy(byteArray, 0, uintArray, 0, byteArray.Length);
                    pixels = uintArray;
                }

                {
                    // todo: optimize this

                    var lightmap = Resources.Load<TextAsset>(scene + "-World" + identifier);
                    if (lightmap == null)
                    {
                        Debug.LogError("Cannot find '" + scene + "-World" + identifier + ".bytes" + "'!");
                        pixels = null;
                        pixels_world = null;
                        return false;
                    }
                    var byteArray = lightmap.bytes;

                    using (var memory = new MemoryStream(byteArray))
                    using (var decompressed = new MemoryStream())
                    using (var gzip = new GZipStream(memory, CompressionMode.Decompress))
                    {
                        gzip.CopyTo(decompressed);
                        gzip.Close();
                        byteArray = decompressed.ToArray();
                    }

                    Vector3[] vector3Array = new Vector3[byteArray.Length / 12];
                    for (int i = 0; i < vector3Array.Length; i++)
                    {
                        var x = BitConverter.ToSingle(byteArray, (i * 12));
                        var y = BitConverter.ToSingle(byteArray, (i * 12) + 4);
                        var z = BitConverter.ToSingle(byteArray, (i * 12) + 8);
                        vector3Array[i] = new Vector3(x, y, z);
                    }
                    pixels_world = vector3Array;
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
                pixels = default;
                pixels_world = default;
                return false;
            }
        }
    }
}