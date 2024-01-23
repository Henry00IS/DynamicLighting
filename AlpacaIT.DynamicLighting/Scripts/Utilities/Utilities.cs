using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AlpacaIT.DynamicLighting.Internal
{
    public static class Utilities
    {
        /// <summary>Gets whether the active scene has been saved to disk in Unity Editor.</summary>
        public static bool IsActiveSceneSavedToDisk
        {
            get
            {
                var scene = SceneManager.GetActiveScene();
                return scene.IsSavedToDisk();
            }
        }

        /// <summary>Gets whether the active scene has been saved to disk in Unity Editor.</summary>
        /// <returns>True when the scene has been saved to disk else false.</returns>
        public static bool IsSavedToDisk(this Scene scene)
        {
            return !string.IsNullOrEmpty(scene.path);
        }

        /// <summary>
        /// Gets the scene name from the <see cref="Scene.path"/>. This is more reliable than <see
        /// cref="Scene.name"/> that (more research required) may be different and changed by the
        /// user (there is a setter).
        /// </summary>
        /// <returns>The scene name or null when the scene has not been saved to disk.</returns>
        public static string GetSceneName(this Scene scene)
        {
            if (!scene.IsSavedToDisk()) return null;
            return Path.GetFileNameWithoutExtension(scene.path);
        }

        /// <summary>
        /// Gets the path to the resources directory of the active scene in Unity Editor. This path
        /// is relative and usually starts with "Assets/" (the forward- or backslash depends on the
        /// operating system).
        /// </summary>
        /// <param name="ensureExists">
        /// Whether to check for the existence of the resource directory and create it if it is not found.
        /// </param>
        /// <returns>
        /// The path to the resources directory without trailing slash. If the active scene has not
        /// been saved to disk then this property returns null. If <paramref name="ensureExists"/>
        /// fails to create the directory it will also return null (and log an error message to the
        /// Unity console, but it will not throw an exception).
        /// </returns>
        public static string CreateResourcesPath(this Scene scene, bool ensureExists = true)
        {
            if (!scene.IsSavedToDisk()) return null;
            var resourcesPath = Path.GetDirectoryName(scene.path) + Path.DirectorySeparatorChar + scene.GetSceneName() + Path.DirectorySeparatorChar + "Resources";

            if (ensureExists)
            {
                try
                {
                    if (!Directory.Exists(resourcesPath))
                        Directory.CreateDirectory(resourcesPath);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Unable to create the resources directory at {resourcesPath}. " + ex.Message);
                    return null;
                }
            }

            return resourcesPath;
        }

        /// <summary>
        /// For the active scene, ensures the resources directory exists (or returns false and logs
        /// an error message to the Unity console) then tries to write the lightmap data file with
        /// the specified identifier (or returns false when an exception occurs). Requires a call to
        /// <see cref="UnityEditor.AssetDatabase.Refresh"/> afterwards.
        /// </summary>
        /// <param name="identifier">
        /// The lightmap identifier is an index stored in the scene and (using this function) to
        /// disk, allowing us to find these resource files later.
        /// </param>
        /// <param name="pixels">The raytraced shadow bits data.</param>
        /// <returns>True when the file has been successfully written else false.</returns>
        public static bool WriteLightmapData(int identifier, string name, uint[] pixels)
        {
            var scene = SceneManager.GetActiveScene();

            // ensure the resources path exists (and thus also that scene was saved to disk).
            var resourcesPath = scene.CreateResourcesPath();
            if (string.IsNullOrEmpty(resourcesPath)) return false;

            var sceneName = scene.GetSceneName();
            if (string.IsNullOrEmpty(sceneName)) return false; // redundant.

            try
            {
                byte[] byteArray = new byte[pixels.Length * 4];
                Buffer.BlockCopy(pixels, 0, byteArray, 0, pixels.Length * 4);

                using (var memory = new MemoryStream(byteArray))
                using (var compressed = new MemoryStream())
                using (var gzip = new GZipStream(compressed, System.IO.Compression.CompressionLevel.Optimal))
                {
                    memory.CopyTo(gzip);
                    gzip.Close();

                    var lightmapFilePath = resourcesPath + Path.DirectorySeparatorChar + sceneName + "-" + name + identifier + ".bytes";
                    File.WriteAllBytes(lightmapFilePath, compressed.ToArray());
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError("Error writing " + name + " " + identifier + " data file: " + ex.Message);
                return false;
            }
        }

        public static bool ReadLightmapData(int identifier, string name, out uint[] pixels)
        {
            try
            {
                string scene = Path.GetFileNameWithoutExtension(SceneManager.GetActiveScene().path);

                var lightmap = Resources.Load<TextAsset>(scene + "-" + name + identifier);
                if (lightmap == null)
                {
                    Debug.LogError("Cannot find '" + scene + "-" + name + identifier + ".bytes'!");
                    pixels = null;
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

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
                pixels = default;
                return false;
            }
        }

        /// <summary>
        /// For the active scene, tries to delete the lightmap data files on disk. When there is an
        /// exception during file deletion an error message is logged to the Unity console.
        /// </summary>
        /// <returns>
        /// True whenever it can be assumed that the files do not exist on disk, including an
        /// unsaved scene and missing resources directory. Returns false only when there was an
        /// exception during file deletion.
        /// </returns>
        public static bool DeleteLightmapData(string name)
        {
            var scene = SceneManager.GetActiveScene();

            // get the resources path without creating it on disk (but we know scene was saved to disk).
            var resourcesPath = scene.CreateResourcesPath(false);
            if (string.IsNullOrEmpty(resourcesPath)) return true;

            var sceneName = scene.GetSceneName();
            if (string.IsNullOrEmpty(sceneName)) return true; // redundant.

            // if the resources path does not exist there is no work to be done.
            if (!Directory.Exists(resourcesPath)) return true;

            try
            {
                var directoryInfo = new DirectoryInfo(resourcesPath);
                foreach (var lightmapFile in directoryInfo.EnumerateFiles(sceneName + "-" + name + "*.bytes"))
                    lightmapFile.Delete();
                foreach (var lightmapFile in directoryInfo.EnumerateFiles(sceneName + "-" + name + "*.bytes.meta"))
                    lightmapFile.Delete();
            }
            catch (Exception ex)
            {
                Debug.LogError("Error deleting lightmap data files on disk in " + resourcesPath + ". " + ex.Message);
                return false;
            }
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
            return true;
        }

        /// <summary>Converts the given array of structs, in memory, to a byte array.</summary>
        public static byte[] StructArrayToByteArray<T>(T[] array) where T : struct
        {
            int size = Marshal.SizeOf(typeof(T));
            int length = array.Length;
            byte[] byteArray = new byte[size * length];

            GCHandle handle = default;

            try
            {
                handle = GCHandle.Alloc(array, GCHandleType.Pinned);
                IntPtr ptr = handle.AddrOfPinnedObject();

                for (int i = 0; i < length; i++)
                {
                    IntPtr offset = IntPtr.Add(ptr, i * size);
                    Marshal.Copy(offset, byteArray, i * size, size);
                }
            }
            finally
            {
                if (handle.IsAllocated)
                    handle.Free();
            }

            return byteArray;
        }

        /// <summary>Converts the given array of bytes, in memory, to an array of structs.</summary>
        public static T[] ByteArrayToStructArray<T>(byte[] byteArray) where T : struct
        {
            int size = Marshal.SizeOf(typeof(T));
            int length = byteArray.Length / size;
            T[] structArray = new T[length];

            GCHandle handle = default;

            try
            {
                handle = GCHandle.Alloc(structArray, GCHandleType.Pinned);
                IntPtr ptr = handle.AddrOfPinnedObject();

                for (int i = 0; i < length; i++)
                {
                    IntPtr offset = IntPtr.Add(ptr, i * size);
                    Marshal.Copy(byteArray, i * size, offset, size);
                }
            }
            finally
            {
                if (handle.IsAllocated)
                    handle.Free();
            }

            return structArray;
        }

#if UNITY_EDITOR

        /// <summary>Attempts to find the most likely scene view camera.</summary>
        /// <returns>The camera if found else null.</returns>
        public static Camera GetSceneViewCamera()
        {
            var sceneView = UnityEditor.SceneView.lastActiveSceneView;
            if (sceneView)
            {
                return sceneView.camera;
            }
            else
            {
                var current = Camera.current;
                if (current)
                {
                    return current;
                }
            }
            return null;
        }

#endif
    }
}