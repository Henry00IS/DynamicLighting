using System.Collections;
using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>Internal extensions used by the Dynamic Lighting package.</summary>
    internal static class Extensions
    {
#if !UNITY_2021_1_OR_NEWER
        /// <summary>
        /// Adds a property to the block. If an integer property with the given name already exists,
        /// the old value is replaced.
        /// </summary>
        /// <param name="materialPropertyBlock">The <see cref="MaterialPropertyBlock"/> to be edited.</param>
        /// <param name="name">The name of the property.</param>
        /// <param name="value">The integer value to set.</param>
        public static void SetInteger(this MaterialPropertyBlock materialPropertyBlock, string name, int value)
        {
            materialPropertyBlock.SetInt(name, value);
        }
#endif

        /// <summary>
        /// Retrieves a hash based on underlying graphics API pointers of the mesh which are only
        /// likely to change with significant mesh edits (DX11).
        /// </summary>
        /// <param name="mesh">The <see cref="Mesh"/> to generate a hash for.</param>
        /// <returns>The hash of the mesh.</returns>
        public static int GetFastHash(this Mesh mesh)
        {
            int hash = mesh.GetNativeIndexBufferPtr().GetHashCode();
            for (int i = 0; i < mesh.vertexBufferCount; i++)
                hash += mesh.GetNativeVertexBufferPtr(i).GetHashCode();
            return hash;
        }
    }
}