using System.Collections.Generic;
using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    internal partial class DynamicLightingTracer
    {
        /// <summary>This step finds compatible mesh renderers in the scene that can be raytraced.</summary>
        public class FindMeshFiltersStep : IStep
        {
            /// <summary>
            /// The collection of static <see cref="MeshFilter"/> that can be raycasted (read only).
            /// </summary>
            public MeshFilter[] staticMeshFilters;

            public void Execute()
            {
                // we iterate over all meshes in the scene:
                var sceneMeshFilters = Object.FindObjectsOfType<MeshFilter>();
                var results = new List<MeshFilter>(sceneMeshFilters.Length);
                for (int i = 0; i < sceneMeshFilters.Length; i++)
                {
                    // check whether the object is static and the mesh filter actually has a mesh assigned:
                    var meshFilter = sceneMeshFilters[i];
                    if (meshFilter.gameObject.isStatic && meshFilter.sharedMesh != null)
                        results.Add(meshFilter);
                }
                // store the compatible mesh filters.
                staticMeshFilters = results.ToArray();
            }
        }
    }
}