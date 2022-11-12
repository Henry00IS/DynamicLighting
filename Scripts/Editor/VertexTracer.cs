using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace AlpacaIT
{
    public static class VertexTracer
    {
        private static VertexPointLight[] pointLights;

        [UnityEditor.MenuItem("Vertex Tracer/Trace")]
        public static void Go()
        {
            pointLights = Object.FindObjectsOfType<VertexPointLight>();
            var meshFilters = Object.FindObjectsOfType<MeshFilter>();
            foreach (var meshFilter in meshFilters)
            {
                if (meshFilter.gameObject.isStatic)
                {
                    Raytrace(meshFilter);
                }
            }
        }

        private static void Raytrace(MeshFilter meshFilter)
        {
            Debug.Log("Raytracing " + meshFilter.name);
            var mesh = meshFilter.sharedMesh;

            //var triangles = mesh.triangles;
            //var vertices = mesh.vertices;
            Triangulate(mesh.triangles, out var triangles, mesh.vertices, out var vertices, mesh.uv, out var uv0);

            var colors = new Color[vertices.Count];

            for (int i = 0; i < triangles.Count; i += 3)
            {
                var v1 = vertices[triangles[i]];
                var v2 = vertices[triangles[i] + 1];
                var v3 = vertices[triangles[i] + 2];

                var res = Raycast(v1, v2, v3);

                colors[triangles[i]] = res.c1;
                colors[triangles[i + 1]] = res.c2;
                colors[triangles[i + 2]] = res.c3;
            }

            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(0, uv0);
            mesh.colors = colors;
        }

        private static void Triangulate(int[] triangles, out List<int> trianglesOut, Vector3[] vertices, out List<Vector3> verticesOut, Vector2[] uv0, out List<Vector2> uvOut)
        {
            trianglesOut = new List<int>();
            verticesOut = new List<Vector3>();
            uvOut = new List<Vector2>();

            var j = 0;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                var v1 = vertices[triangles[i]];
                var v2 = vertices[triangles[i + 1]];
                var v3 = vertices[triangles[i + 2]];

                var uv1 = uv0[triangles[i]];
                var uv2 = uv0[triangles[i + 1]];
                var uv3 = uv0[triangles[i + 2]];

                trianglesOut.Add(j++);
                trianglesOut.Add(j++);
                trianglesOut.Add(j++);

                verticesOut.Add(v1);
                verticesOut.Add(v2);
                verticesOut.Add(v3);

                uvOut.Add(uv1);
                uvOut.Add(uv2);
                uvOut.Add(uv3);
            }
        }

        private static float InverseSquareLaw(float distance)
        {
            //if (distance == 0f) return 0f;
            return 1.0f / (distance * distance);
        }

        private static (Color c1, Color c2, Color c3) Raycast(Vector3 v1, Vector3 v2, Vector3 v3)
        {
            Color c1 = new Color(), c2 = new Color(), c3 = new Color();
            c1.a = 1f;
            c2.a = 1f;
            c3.a = 1f;

            var center = (v1 + v2 + v3) / 3f;
            var plane = new Plane(v1, v2, v3);
            var normal = plane.normal;

            for (int i = 0; i < pointLights.Length; i++)
            {
                var pointLight = pointLights[i];
                var position = pointLight.transform.position;

                float v1dist = Vector3.Distance(v1, position);
                float v2dist = Vector3.Distance(v2, position);
                float v3dist = Vector3.Distance(v3, position);

                float v1sq = InverseSquareLaw(v1dist);
                float v2sq = InverseSquareLaw(v2dist);
                float v3sq = InverseSquareLaw(v3dist);

                v1sq *= pointLight.lightIntensity;
                v2sq *= pointLight.lightIntensity;
                v3sq *= pointLight.lightIntensity;

                var v1dir = (position - v1).normalized;
                var v2dir = (position - v2).normalized;
                var v3dir = (position - v3).normalized;

                var v1tocenter = (center - v1).normalized;
                var v2tocenter = (center - v2).normalized;
                var v3tocenter = (center - v3).normalized;

                bool v1cast = Physics.Raycast(v1 + (normal * 0.1f) + (v1tocenter * 0.1f), v1dir, v1dist);
                bool v2cast = Physics.Raycast(v2 + (normal * 0.1f) + (v2tocenter * 0.1f), v2dir, v2dist);
                bool v3cast = Physics.Raycast(v3 + (normal * 0.1f) + (v3tocenter * 0.1f), v3dir, v3dist);

                //Debug.DrawLine(v1 + (normal * 0.1f) + (v1tocenter * 0.1f), v1 + (normal * 0.2f) + (v1tocenter * 0.1f), Color.green, 10f);

                if (!v1cast) c1 += v1sq * pointLight.lightColor;
                if (!v2cast) c2 += v2sq * pointLight.lightColor;
                if (!v3cast) c3 += v3sq * pointLight.lightColor;
            }

            return (c1, c2, c3);
        }
    }
}