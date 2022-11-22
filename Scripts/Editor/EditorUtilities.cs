using System;
using System.IO;
using System.IO.Compression;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AlpacaIT.DynamicLighting
{
    public static class EditorUtilities
    {
        /// <summary>
        /// Gets the active scene storage directory (where Unity would place lightmaps and such).
        /// </summary>
        /// <returns>Returns the full path or null on error.</returns>
        public static string CreateAndGetActiveSceneStorageDirectory()
        {
            try
            {
                Scene scene = SceneManager.GetActiveScene();
                var path = Path.GetDirectoryName(scene.path) + Path.DirectorySeparatorChar + scene.name;

                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                if (!Directory.Exists(path + "\\Resources"))
                    Directory.CreateDirectory(path + "\\Resources");
                return path;
            }
            catch (Exception ex)
            {
                Debug.LogError("Error attempting to get or create the resources directory for the scene: " + ex.Message);
                return null;
            }
        }

        public static bool WriteLightmapData(int identifier, uint[] pixels)
        {
            try
            {
                string scene = Path.GetFileNameWithoutExtension(SceneManager.GetActiveScene().path);
                string path = CreateAndGetActiveSceneStorageDirectory();

                byte[] byteArray = new byte[pixels.Length * 4];
                Buffer.BlockCopy(pixels, 0, byteArray, 0, pixels.Length * 4);

                using (var memory = new MemoryStream(byteArray))
                using (var compressed = new MemoryStream())
                using (var gzip = new GZipStream(compressed, System.IO.Compression.CompressionLevel.Optimal))
                {
                    memory.CopyTo(gzip);
                    gzip.Close();

                    var lightmapFilePath = path + Path.DirectorySeparatorChar + "Resources" + Path.DirectorySeparatorChar + scene + "-Lightmap" + identifier + ".bytes";
                    File.WriteAllBytes(lightmapFilePath, compressed.ToArray());
#if UNITY_EDITOR
                    UnityEditor.AssetDatabase.ImportAsset(lightmapFilePath);
#endif
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError("Error writing lightmap " + identifier + " data file: " + ex.Message);
                return false;
            }
        }
    }
}