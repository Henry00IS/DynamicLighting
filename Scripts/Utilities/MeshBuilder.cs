using UnityEngine;

namespace AlpacaIT.VertexTracer
{
    public class MeshBuilder
    {
        // the original mesh data:
        private readonly Vector3[] meshVertices;
        public readonly int[] meshTriangles;
        public readonly Vector2[] meshUv1;

        // the world space mesh vertices:
        public readonly Vector3[] worldVertices;

        public MeshBuilder(Matrix4x4 localToWorldMatrix, Mesh mesh)
        {
            // read the mesh data into memory.
            meshVertices = mesh.vertices;
            meshTriangles = mesh.triangles;
            meshUv1 = mesh.uv2;

            // convert the vertices to world positions.
            worldVertices = new Vector3[meshVertices.Length];
            for (int i = 0; i < meshVertices.Length; i++)
                worldVertices[i] = localToWorldMatrix.MultiplyPoint(meshVertices[i]);

            // free the original mesh data from memory.
            meshVertices = null;
        }
    }
}