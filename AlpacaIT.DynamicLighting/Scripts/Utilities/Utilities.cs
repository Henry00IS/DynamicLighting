using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AlpacaIT.DynamicLighting.Internal
{
    /// <summary>Utilities class mostly related to Unity Editor and the File System.</summary>
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
        /// Gets the path to the directory of the active scene in Unity Editor. This path is
        /// relative and usually starts with "Assets/" (the forward- or backslash depends on the
        /// operating system).
        /// </summary>
        /// <param name="ensureExists">
        /// Whether to check for the existence of the scene directory and create it if it is not found.
        /// </param>
        /// <returns>
        /// The path to the scene directory without trailing slash. If the active scene has not been
        /// saved to disk then this property returns null. If <paramref name="ensureExists"/> fails
        /// to create the directory it will also return null (and log an error message to the Unity
        /// console, but it will not throw an exception).
        /// </returns>
        public static string CreateScenePath(this Scene scene, bool ensureExists = true)
        {
            if (!scene.IsSavedToDisk()) return null;
            var scenePath = Path.GetDirectoryName(scene.path) + Path.DirectorySeparatorChar + scene.GetSceneName();

            if (ensureExists)
            {
                try
                {
                    if (!Directory.Exists(scenePath))
                        Directory.CreateDirectory(scenePath);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Unable to create the scene directory at {scenePath}. " + ex.Message);
                    return null;
                }
            }

            return scenePath;
        }

        /// <summary>
        /// For the active scene, ensures the scene directory exists (or returns false and logs an
        /// error message to the Unity console) then tries to write the <see cref="RaycastedScene"/>
        /// asset (or returns false when an exception occurs). Requires a call to <see
        /// cref="UnityEditor.AssetDatabase.Refresh"/> afterwards.
        /// </summary>
        /// <param name="raycastedScene">The <see cref="RaycastedScene"/> to be written to disk.</param>
        /// <returns>True when the file has been successfully written else false.</returns>
        internal static bool WriteRaycastedScene(RaycastedScene raycastedScene)
        {
            var scene = SceneManager.GetActiveScene();

            // ensure the resources path exists (and thus also that scene was saved to disk).
            var scenePath = scene.CreateScenePath();
            if (string.IsNullOrEmpty(scenePath)) return false;

            var sceneName = scene.GetSceneName();
            if (string.IsNullOrEmpty(sceneName)) return false; // redundant.

            try
            {
#if UNITY_EDITOR
                UnityEditor.AssetDatabase.CreateAsset(raycastedScene, scenePath + Path.DirectorySeparatorChar + "DynamicLighting3-" + sceneName + ".asset");
                UnityEditor.AssetDatabase.SaveAssets();
#endif
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError("Error writing raycasted scene data file: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// For the active scene, tries to delete the <see cref="RaycastedScene"/> asset. When there
        /// is an exception during file deletion an error message is logged to the Unity console.
        /// </summary>
        /// <returns>True when the file has been successfully deleted else false.</returns>
        public static bool DeleteRaycastedScene()
        {
            var scene = SceneManager.GetActiveScene();

            // get the scene path without creating it on disk (but we know scene was saved to disk).
            var scenePath = scene.CreateScenePath(false);
            if (string.IsNullOrEmpty(scenePath)) return true;

            var sceneName = scene.GetSceneName();
            if (string.IsNullOrEmpty(sceneName)) return true; // redundant.
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.DeleteAsset(scenePath + Path.DirectorySeparatorChar + "DynamicLighting3-" + sceneName + ".asset");
#endif
            return true;
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
        public static bool DeleteLegacyLightmapData(string name)
        {
            var scene = SceneManager.GetActiveScene();

            // get the resources path without creating it on disk (but we know scene was saved to disk).
            var scenePath = scene.CreateScenePath(false);
            if (string.IsNullOrEmpty(scenePath)) return true;

            var sceneName = scene.GetSceneName();
            if (string.IsNullOrEmpty(sceneName)) return true; // redundant.

            var resourcesPath = scenePath + Path.DirectorySeparatorChar + "Resources";

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

        /// <summary>Gets the version number of the specified package.</summary>
        /// <param name="packageName">The package name to find (e.g. "de.alpacait.dynamiclighting")</param>
        /// <returns>
        /// The version number of the package such as 1.0.0 or an empty string if not found.
        /// </returns>
        public static string GetPackageVersion(string packageName = "de.alpacait.dynamiclighting")
        {
#if UNITY_2021_2_OR_NEWER
            var packages = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages();
            for (int i = 0; i < packages.Length; i++)
            {
                var package = packages[i];
                if (package.name == packageName)
                    return package.version;
            }
#endif
            return "";
        }

#endif
    }
}