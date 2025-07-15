using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>
    /// Fixes some issues with DirectX12 that has strict rules that all buffers must be assigned in shaders.
    /// </summary>
    internal static class DirectX12
    {
        /// <summary>
        /// The global shader buffer "dynamic_triangles" for DirectX12 compatibility, which requires
        /// all buffers to be assigned. The "dynamic_triangles" buffer is only set using Material
        /// Property Blocks, so Unity will complain whenever this is not the case. MPBs have
        /// priority over global shader variables.
        /// </summary>
        private static ComputeBuffer dynamicTrianglesGlobalBuffer;

        /// <summary>Creates the global fallback buffers so that DirectX 12 is satisfied.</summary>
        private static void CreateFallbackBuffers()
        {
            if (dynamicTrianglesGlobalBuffer != null && dynamicTrianglesGlobalBuffer.IsValid()) return;

            dynamicTrianglesGlobalBuffer = new ComputeBuffer(1, 4, ComputeBufferType.Default);
            Shader.SetGlobalBuffer("dynamic_triangles", dynamicTrianglesGlobalBuffer);
        }

        /// <summary>Releases the global fallback buffers (created by <see cref="CreateFallbackBuffers"/>).</summary>
        private static void ReleaseFallbackBuffers()
        {
            if (dynamicTrianglesGlobalBuffer != null && dynamicTrianglesGlobalBuffer.IsValid())
            {
                dynamicTrianglesGlobalBuffer.Release();
                dynamicTrianglesGlobalBuffer = null;
            }
        }

#if UNITY_EDITOR

        [UnityEditor.InitializeOnLoadMethod]
        private static void Initialize()
        {
            // for now we only apply these hacks when running on directx 12.
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Direct3D12) return;

            // immediately create the fallback buffers.
            CreateFallbackBuffers();

            // before assemblies reload (could cause memory leak) release the fallback buffers.
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += ReleaseFallbackBuffers;
        }

#else

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            // for now we only apply these hacks when running on directx 12.
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Direct3D12) return;

            // immediately create the fallback buffers.
            CreateFallbackBuffers();

            // on application quit in builds release the fallback buffers.
            Application.quitting += ReleaseFallbackBuffers;
        }

#endif
    }
}