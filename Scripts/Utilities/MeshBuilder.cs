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
        private Vector3[] originalNormals;
        private Vector2[] originalUv0;
        private Vector2[] originalUv1;

        // the modified mesh data:
        public List<int> meshTriangles;
        public List<Vector3> meshVertices;
        public List<Vector3> meshNormals;
        public List<Vector2> meshUv0;
        public List<Vector2> meshUv1;

        // the world space mesh vertices:
        public List<Vector3> worldVertices;

        public readonly Mesh mesh;

        public MeshBuilder(Matrix4x4 localToWorldMatrix, Mesh mesh)
        {
            // read the mesh data into memory.
            originalTriangles = mesh.triangles;
            originalVertices = mesh.vertices;
            originalNormals = mesh.normals;
            originalUv0 = mesh.uv;
            originalUv1 = mesh.uv2;

            // triangulate the mesh into our own lists and convert the vertices to world positions.
            Triangulate(localToWorldMatrix);

            // Tessellate(localToWorldMatrix);
            // Tessellate(localToWorldMatrix);

            // free the original mesh data from memory.
            originalTriangles = null;
            originalVertices = null;
            originalNormals = null;
            originalUv0 = null;
            originalUv1 = null;

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

            this.mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            this.mesh.SetVertices(meshVertices);
            this.mesh.SetNormals(meshNormals);
            this.mesh.SetUVs(0, meshUv0);
            this.mesh.SetUVs(1, meshUv1);
            this.mesh.SetTriangles(meshTriangles, 0);
        }

        private void Triangulate(Matrix4x4 localToWorldMatrix)
        {
            var triangleCount = originalTriangles.Length;
            var vertexCount = triangleCount * 3;

            meshTriangles = new List<int>(triangleCount);
            meshVertices = new List<Vector3>(vertexCount);
            meshNormals = new List<Vector3>(vertexCount);
            worldVertices = new List<Vector3>(vertexCount);
            meshUv0 = new List<Vector2>(vertexCount);
            meshUv1 = new List<Vector2>(vertexCount);

            var j = 0;
            for (int i = 0; i < originalTriangles.Length; i += 3)
            {
                var v1 = originalVertices[originalTriangles[i]];
                var v2 = originalVertices[originalTriangles[i + 1]];
                var v3 = originalVertices[originalTriangles[i + 2]];

                var n1 = originalNormals[originalTriangles[i]];
                var n2 = originalNormals[originalTriangles[i + 1]];
                var n3 = originalNormals[originalTriangles[i + 2]];

                var uv01 = originalUv0[originalTriangles[i]];
                var uv02 = originalUv0[originalTriangles[i + 1]];
                var uv03 = originalUv0[originalTriangles[i + 2]];

                var uv11 = originalUv1[originalTriangles[i]];
                var uv12 = originalUv1[originalTriangles[i + 1]];
                var uv13 = originalUv1[originalTriangles[i + 2]];

                meshTriangles.Add(j++);
                meshTriangles.Add(j++);
                meshTriangles.Add(j++);

                meshVertices.Add(v1);
                meshVertices.Add(v2);
                meshVertices.Add(v3);

                meshNormals.Add(n1);
                meshNormals.Add(n2);
                meshNormals.Add(n3);

                meshUv0.Add(uv01);
                meshUv0.Add(uv02);
                meshUv0.Add(uv03);

                meshUv1.Add(uv11);
                meshUv1.Add(uv12);
                meshUv1.Add(uv13);

                // store the world positions of all vertices.
                worldVertices.Add(localToWorldMatrix.MultiplyPoint(v1));
                worldVertices.Add(localToWorldMatrix.MultiplyPoint(v2));
                worldVertices.Add(localToWorldMatrix.MultiplyPoint(v3));
            }
        }

        private void Tessellate(Matrix4x4 localToWorldMatrix)
        {
            var originalMeshTrianglesCount = meshTriangles.Count;
            var j = originalMeshTrianglesCount;
            for (int i = 0; i < originalMeshTrianglesCount; i += 3)
            {
                // fetch the 3 vertices of the triangle.
                var v1 = meshVertices[meshTriangles[i]];
                var v2 = meshVertices[meshTriangles[i + 1]];
                var v3 = meshVertices[meshTriangles[i + 2]];
                var t1 = meshUv0[i];
                var t2 = meshUv0[i + 1];
                var t3 = meshUv0[i + 2];

                // find the halfway point on every edge.
                var l1 = Vector3.Lerp(v1, v2, 0.5f);
                var l2 = Vector3.Lerp(v2, v3, 0.5f);
                var l3 = Vector3.Lerp(v3, v1, 0.5f);

                var tl1 = Vector3.Lerp(t1, t2, 0.5f);
                var tl2 = Vector3.Lerp(t2, t3, 0.5f);
                var tl3 = Vector3.Lerp(t3, t1, 0.5f);

                // replace the original triangle to be the center triangle.
                meshVertices[meshTriangles[i]] = l1;
                meshVertices[meshTriangles[i + 1]] = l2;
                meshVertices[meshTriangles[i + 2]] = l3;
                meshUv0[i] = tl1;
                meshUv0[i + 1] = tl2;
                meshUv0[i + 2] = tl3;
                worldVertices[i] = localToWorldMatrix.MultiplyPoint(l1);
                worldVertices[i + 1] = localToWorldMatrix.MultiplyPoint(l2);
                worldVertices[i + 2] = localToWorldMatrix.MultiplyPoint(l3);

                // create the new triangles for the triangle.
                System.Action<Vector3, Vector3, Vector3, Vector2, Vector2, Vector2> AddTri = (a, b, c, t1, t2, t3) =>
                {
                    meshTriangles.Add(j++);
                    meshTriangles.Add(j++);
                    meshTriangles.Add(j++);

                    meshVertices.Add(a);
                    meshVertices.Add(b);
                    meshVertices.Add(c);

                    meshUv0.Add(t1);
                    meshUv0.Add(t2);
                    meshUv0.Add(t3);

                    // store the world positions of all vertices.
                    worldVertices.Add(localToWorldMatrix.MultiplyPoint(a));
                    worldVertices.Add(localToWorldMatrix.MultiplyPoint(b));
                    worldVertices.Add(localToWorldMatrix.MultiplyPoint(c));
                };

                AddTri(v1, l1, l3, t1, tl1, tl3);
                AddTri(l1, v2, l2, tl1, t2, tl2);
                AddTri(l2, v3, l3, tl2, t3, tl3);
            }
        }
    }
}