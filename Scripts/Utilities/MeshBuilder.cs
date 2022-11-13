using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace AlpacaIT.VertexTracer
{
    public class MeshBuilder
    {
        // the original mesh data:
        private int[] originalTriangles;
        private Vector3[] originalVertices;
        private Vector2[] originalUv0;

        // the modified mesh data:
        public List<int> meshTriangles;
        public List<Vector3> meshVertices;
        public List<Vector2> meshUv0;

        // the world space mesh vertices:
        public List<Vector3> worldVertices;

        public readonly Mesh mesh;

        public MeshBuilder(Matrix4x4 localToWorldMatrix, Mesh mesh)
        {
            // read the mesh data into memory.
            originalTriangles = mesh.triangles;
            originalVertices = mesh.vertices;
            originalUv0 = mesh.uv;

            // triangulate the mesh into our own lists and convert the vertices to world positions.
            Triangulate(localToWorldMatrix);

            // free the original mesh data from memory.
            originalTriangles = null;
            originalVertices = null;
            originalUv0 = null;

            // Realtime CSG hack:
            if (mesh.name.StartsWith("<Renderable generated -"))
            {
                // use the same mesh so RealtimeCSG does not reset it.
                this.mesh = mesh;
            }
            else
            {
                // create a replacement mesh.
                this.mesh = new Mesh();
                this.mesh.name = mesh.name.Replace(" (Vertex Tracer)", "") + " (Vertex Tracer)";
            }

            this.mesh.SetVertices(meshVertices);
            this.mesh.SetUVs(0, meshUv0);
            this.mesh.SetTriangles(meshTriangles, 0);
        }

        private void Triangulate(Matrix4x4 localToWorldMatrix)
        {
            var triangleCount = originalTriangles.Length;
            var vertexCount = triangleCount * 3;

            meshTriangles = new List<int>(triangleCount);
            meshVertices = new List<Vector3>(vertexCount);
            worldVertices = new List<Vector3>(vertexCount);
            meshUv0 = new List<Vector2>(vertexCount);

            var j = 0;
            for (int i = 0; i < originalTriangles.Length; i += 3)
            {
                var v1 = originalVertices[originalTriangles[i]];
                var v2 = originalVertices[originalTriangles[i + 1]];
                var v3 = originalVertices[originalTriangles[i + 2]];

                var uv1 = originalUv0[originalTriangles[i]];
                var uv2 = originalUv0[originalTriangles[i + 1]];
                var uv3 = originalUv0[originalTriangles[i + 2]];

                meshTriangles.Add(j++);
                meshTriangles.Add(j++);
                meshTriangles.Add(j++);

                meshVertices.Add(v1);
                meshVertices.Add(v2);
                meshVertices.Add(v3);

                meshUv0.Add(uv1);
                meshUv0.Add(uv2);
                meshUv0.Add(uv3);

                // store the world positions of all vertices.
                worldVertices.Add(localToWorldMatrix.MultiplyPoint(v1));
                worldVertices.Add(localToWorldMatrix.MultiplyPoint(v2));
                worldVertices.Add(localToWorldMatrix.MultiplyPoint(v3));
            }
        }
    }
}