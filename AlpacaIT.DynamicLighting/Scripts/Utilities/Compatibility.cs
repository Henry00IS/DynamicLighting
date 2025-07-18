using System.Runtime.CompilerServices;
using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>Compatibility layer for Unity functions that were deprecated.</summary>
    internal static class Compatibility
    {
        /// <summary>
        /// Returns the first active loaded object of <typeparamref name="T"/> type.
        /// <para>Unity 6 deprecated <see cref="Object.FindObjectOfType"/> and we use <see cref="Object.FindFirstObjectByType"/>.</para>
        /// </summary>
        /// <typeparam name="T">The type of object or asset to find.</typeparam>
        /// <returns>The object or asset found matching the type specified.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T FindObjectOfType<T>() where T : Object
        {
#if UNITY_6000_0_OR_NEWER
            return Object.FindFirstObjectByType<T>();
#else
            return Object.FindObjectOfType<T>();
#endif
        }

        /// <summary>
        /// Returns a list of all active and inactive loaded objects of <typeparamref name="T"/> type, including assets.
        /// <para>Unity 6 deprecated <see cref="Object.FindObjectsOfType"/> and we use <see cref="Object.FindObjectsByType"/>.</para>
        /// </summary>
        /// <typeparam name="T">The type of object or asset to find.</typeparam>
        /// <returns>The array of objects or assets found matching the type specified.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] FindObjectsOfType<T>() where T : Object
        {
#if UNITY_6000_0_OR_NEWER
            return Object.FindObjectsByType<T>(FindObjectsSortMode.InstanceID);
#else
            return Object.FindObjectsOfType<T>();
#endif
        }
    }
}