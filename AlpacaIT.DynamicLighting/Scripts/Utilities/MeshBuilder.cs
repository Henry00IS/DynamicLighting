using Unity.Mathematics;
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

        /// <summary>
        /// The bounding boxes encompassing the triangles in UV1 coordinates. The flag <see
        /// cref="hasLightmapCoordinates"/> must be true for this to be set.
        /// </summary>
        public readonly PixelTriangleRect[] triangleUv1BoundingBoxes;

        /// <summary>
        /// The flat triangle normals each calculated using a plane of 3 vertices (i.e. not the
        /// normals stored inside of the mesh data). Degenerate triangles have have a normal of <see cref="Vector3.zero"/>.
        /// </summary>
        public readonly Vector3[] triangleNormals;

        /// <summary>The surface area of the mesh in meters squared.</summary>
        public readonly float surfaceArea;

        /// <summary>The texture size required to evenly distribute texels over this mesh.</summary>
        public readonly int textureSize;

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
        public MeshBuilder(Matrix4x4 localToWorldMatrix, Mesh mesh, int pixelDensityPerSquareMeter, int maximumTextureSize)
        {
            // read the original mesh data into memory.
            var meshVertices = mesh.vertices;
            meshTriangles = mesh.triangles;
            meshUv1 = mesh.uv2;

            // convert the vertices to world positions.
            worldVertices = new Vector3[meshVertices.Length];
            for (int i = 0; i < meshVertices.Length; i++)
                worldVertices[i] = localToWorldMatrix.MultiplyPoint(meshVertices[i]);

            // check whether the mesh has lightmap coordinates.
            hasLightmapCoordinates = meshUv1.Length > 0;

            // calculate additional triangle data.
            triangleNormals = new Vector3[meshTriangles.Length];
            for (int i = 0, j = 0; i < meshTriangles.Length; i += 3)
            {
                var vertex1 = meshTriangles[i];
                var vertex2 = meshTriangles[i + 1];
                var vertex3 = meshTriangles[i + 2];

                // calculate the surface area of the mesh by adding the surface area of every triangle.
                var world1 = worldVertices[vertex1];
                var world2 = worldVertices[vertex2];
                var world3 = worldVertices[vertex3];
                surfaceArea += MathEx.CalculateSurfaceAreaOfTriangle(worldVertices[vertex1], worldVertices[vertex2], worldVertices[vertex3]);

                // calculate the triangle normals (identical equation to using a plane).
                triangleNormals[j] = Vector3.Normalize(Vector3.Cross(world2 - world1, world3 - world1));

                j++;
            }

            // calculate the texture size required for this mesh.
            textureSize = MathEx.SurfaceAreaToTextureSize(surfaceArea, pixelDensityPerSquareMeter);
            if (textureSize > maximumTextureSize)
                textureSize = maximumTextureSize;
            // ensure there is at least one pixel (when taken -1 for array index).
            if (textureSize <= 1)
                textureSize = 2;

            // calculate the triangle bounding boxes in UV1 space.
            if (hasLightmapCoordinates)
            {
                triangleUv1BoundingBoxes = new PixelTriangleRect[meshTriangles.Length];

                for (int i = 0, j = 0; i < meshTriangles.Length; i += 3)
                {
                    var vertex1 = meshTriangles[i];
                    var vertex2 = meshTriangles[i + 1];
                    var vertex3 = meshTriangles[i + 2];

                    // calculate the texture size needed for the uv1 bounds of the triangle.
                    var lm1 = meshUv1[vertex1];
                    var lm2 = meshUv1[vertex2];
                    var lm3 = meshUv1[vertex3];
                    triangleUv1BoundingBoxes[j] = new PixelTriangleRect(textureSize, MathEx.ComputeTriangleBoundingBox(lm1, lm2, lm3));

                    j++;
                }
            }

            // calculate the triangle count.
            triangleCount = meshTriangles.Length / 3;
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