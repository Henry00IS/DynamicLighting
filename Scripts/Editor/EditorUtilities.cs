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
                string path = SceneManager.GetActiveScene().path;
                path = Application.dataPath + "\\..\\" + Path.GetDirectoryName(path) + "\\" + Path.GetFileNameWithoutExtension(path);
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                if (!Directory.Exists(path + "\\Resources"))
                    Directory.CreateDirectory(path + "\\Resources");
                return path;
            }
            catch
            {
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
                    File.WriteAllBytes(path + "\\Resources\\" + scene + "-Lightmap" + identifier + ".bytes", compressed.ToArray());
#if UNITY_EDITOR
                    string path2 = SceneManager.GetActiveScene().path;
                    UnityEditor.AssetDatabase.ImportAsset(Path.GetDirectoryName(path2) + "\\" + Path.GetFileNameWithoutExtension(path2) + "\\Resources\\" + scene + "-Lightmap" + identifier + ".bytes");
#endif
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
                return false;
            }
        }
    }
}