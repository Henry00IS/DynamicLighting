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
    }
}