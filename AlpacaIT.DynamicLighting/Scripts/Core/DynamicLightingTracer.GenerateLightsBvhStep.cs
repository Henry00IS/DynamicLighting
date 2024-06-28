using AlpacaIT.DynamicLighting.Internal;
using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    internal partial class DynamicLightingTracer
    {
        /// <summary>
        /// This step finds dynamic light sources in the scene that can be raytraced, builds a
        /// Bounding Volume Hierarchy and adjusts the light source order to match the Bounding
        /// Volume Hierarchy.
        /// </summary>
        public class GenerateLightsBvhStep : IStep
        {
            /// <summary>
            /// The collection of <see cref="DynamicLight"/> that can be raycasted in an order
            /// compatible with the Bounding Volume Hierarchy (read only).
            /// </summary>
            public DynamicLight[] dynamicLights;

            /// <summary>The bounding volume hierarchy for the <see cref="dynamicLights"/>.</summary>
            public BvhAccelerationStructure<DynamicLight> boundingVolumeHierarchy;

            /// <summary>
            /// After calling <see cref="WriteBoundingVolumeHierarchyToDisk"/> contains the VRAM
            /// required in bytes.
            /// </summary>
            public ulong vramRequired;

            public void Execute()
            {
                // find all of the dynamic lights in the scene that are not realtime.
                dynamicLights = DynamicLightManager.FindDynamicLightsInScene().ToArray();

                // there must be at least one light in order to create the bounding volume hierarchy.
                if (dynamicLights.Length > 0)
                {
                    // create the dynamic lights bounding volume hierarchy and write it to disk.
                    boundingVolumeHierarchy = new BvhAccelerationStructure<DynamicLight>(dynamicLights);

                    // create the point lights array with the order the bvh tree desires.
                    var pointLights = new DynamicLight[dynamicLights.Length];
                    for (int i = 0; i < dynamicLights.Length; i++)
                        pointLights[i] = dynamicLights[boundingVolumeHierarchy.itemsIdx[i]]; // pigeonhole sort!

                    // todo: prevent the additional array by swapping elements?

                    // replace the original array of lights with the sorted array.
                    dynamicLights = pointLights;
                }
            }

            /// <summary>Writes the <see cref="boundingVolumeHierarchy"/> to disk.</summary>
            public void WriteBoundingVolumeHierarchyToDisk()
            {
                // create the bounding volume hierarchy as uint[] compute buffer data.
                var bvhDynamicLights32 = boundingVolumeHierarchy.ToUInt32Array();
                vramRequired = (ulong)bvhDynamicLights32.Length * 4;

                // write the bounding volume hierarchy to disk.
                if (!Utilities.WriteLightmapData(0, "DynamicLightingBvh2", bvhDynamicLights32))
                    Debug.LogError($"Unable to write the dynamic lights bounding volume hierarchy file in the active scene resources directory!");
            }
        }
    }
}