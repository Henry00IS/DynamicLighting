using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>
    /// Compute shader for the dynamic triangles data structure, that fixes all pointers to allow
    /// position independent correctness. This avoids additional offset computations on the GPU in
    /// the fragment shader.
    /// </summary>
    internal class DynamicTrianglesFixupComputeShader
    {
        private ComputeShader computeShader;
        private int computeKernel;

        public DynamicTrianglesFixupComputeShader()
        {
            computeShader = DynamicLightingResources.Instance.dynamicTrianglesFixupComputeShader;
            computeKernel = computeShader.FindKernel("FixupChunk");
        }

        /// <summary>
        /// Dispatches the compute shader to offset the dynamic triangles data structure.
        /// <para>Requires global shader variable "dynamic_triangles" to be set.</para>
        /// </summary>
        /// <param name="dynamicTrianglesOffset">The position in the compute buffer.</param>
        /// <param name="meshTriangleCount">The total amount of triangles of all sub meshes.</param>
        /// <param name="hasBounceLighting">Whether bounce lighting is active in the current scene.</param>
        public void Dispatch(int dynamicTrianglesOffset, int meshTriangleCount, bool hasBounceLighting)
        {
            if (meshTriangleCount <= 0)
                return;

            computeShader.SetInt("dynamic_triangles_offset", dynamicTrianglesOffset);
            computeShader.SetInt("dynamic_triangles_count", meshTriangleCount);
            computeShader.SetInt("dynamic_triangles_bounce_stride", hasBounceLighting ? 3 : 2);
            computeShader.Dispatch(computeKernel, (meshTriangleCount + 63) / 64, 1, 1);
        }
    }
}
