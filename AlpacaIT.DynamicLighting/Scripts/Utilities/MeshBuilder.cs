using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>Processes a <see cref="Mesh"/> for fast data access and raycasting in the scene.</summary>
    internal class MeshBuilder
    {
        /// <summary>The original unmodified triangle indices of the mesh.</summary>
        public readonly int[] meshTriangles;

        /// <summary>The original unmodified UV1 coordinates of the mesh.</summary>
        public readonly Vector2[] meshUv1;

        /// <summary>The vertices of the mesh in world-space coordinates.</summary>
        public readonly Vector3[] worldVertices;

        /// <summary>The surface area of the mesh in meters squared.</summary>
        public readonly float surfaceArea;

        /// <summary>Gets the triangle count of the mesh i.e. <see cref="meshTriangles"/> / 3.</summary>
        public readonly int triangleCount;

        /// <summary>
        /// Gets whether the mesh has lightmap coordinates in <see cref="meshUv1"/>. If this is
        /// false, the mesh is unable to store lightmap data.
        /// </summary>
        public readonly bool hasLightmapCoordinates;

        /// <summary>
        /// Processes the given mesh and pre-calculates all required values for fast access.
        /// </summary>
        /// <param name="localToWorldMatrix">
        /// The transformation matrix to transform local vertex coordinates to world-space.
        /// </param>
        /// <param name="mesh">The mesh to be processed.</param>
        public MeshBuilder(Matrix4x4 localToWorldMatrix, Mesh mesh)
        {
            // read the original mesh data into memory.
            var meshVertices = mesh.vertices;
            meshTriangles = mesh.triangles;
            meshUv1 = mesh.uv2;

            // convert the vertices to world positions.
            worldVertices = new Vector3[meshVertices.Length];
            for (int i = 0; i < meshVertices.Length; i++)
            {
                worldVertices[i] = localToWorldMatrix.MultiplyPoint(meshVertices[i]);
            }

            // calculate the surface area of the mesh.
            for (int i = 0; i < meshTriangles.Length; i += 3)
            {
                // add the surface area of every triangle of the mesh.
                surfaceArea += MathEx.CalculateSurfaceAreaOfTriangle(worldVertices[meshTriangles[i]], worldVertices[meshTriangles[i + 1]], worldVertices[meshTriangles[i + 2]]);
            }

            // check whether the mesh has lightmap coordinates.
            hasLightmapCoordinates = meshUv1.Length > 0;

            // calculate the triangle count.
            triangleCount = meshTriangles.Length / 3;

            // protect against uv1 having not-a-number values as they cause severe errors.
            if (hasLightmapCoordinates)
            {
                bool finiteWarning = false;
                for (int i = 0; i < meshUv1.Length; i++)
                {
                    var v1 = meshUv1[i];
                    if (!float.IsFinite(v1.x + v1.y))
                    {
                        meshUv1[i] = Vector2.zero;
                        finiteWarning = true;
                    }
                }
                if (finiteWarning) Debug.LogWarning("The mesh " + mesh.name + " has non-finite UV1 coordinates (NaN or Infinite)!");
            }
        }

        /// <summary>Gets the 3 vertices that make up the triangle at the given triangle index.</summary>
        /// <param name="triangleIndex">The index of the triangle in the mesh.</param>
        /// <returns>The 3 vertices that make up the triangle.</returns>
        public (Vector3 a, Vector3 b, Vector3 c) GetTriangleVertices(int triangleIndex)
        {
            triangleIndex *= 3;
            var v1 = worldVertices[meshTriangles[triangleIndex]];
            var v2 = worldVertices[meshTriangles[triangleIndex + 1]];
            var v3 = worldVertices[meshTriangles[triangleIndex + 2]];
            return (v1, v2, v3);
        }

        /// <summary>
        /// Gets the 3 vertex UV1 coordinates for the triangle at the given triangle index. The flag
        /// <see cref="hasLightmapCoordinates"/> must be true before calling this function.
        /// </summary>
        /// <param name="triangleIndex">The index of the triangle in the mesh.</param>
        /// <returns>The 3 vertex UV1 coordinates associated with the triangle.</returns>
        public (Vector3 a, Vector3 b, Vector3 c) GetTriangleUv1(int triangleIndex)
        {
            triangleIndex *= 3;
            var v1 = meshUv1[meshTriangles[triangleIndex]];
            var v2 = meshUv1[meshTriangles[triangleIndex + 1]];
            var v3 = meshUv1[meshTriangles[triangleIndex + 2]];
            return (v1, v2, v3);
        }
    }
}