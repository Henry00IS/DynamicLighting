using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace AlpacaIT.VertexTracer
{
    public static class VertexTracer
    {
        private static VertexPointLight[] pointLights;
        private static VertexAntiLight[] shadowLights;

        [UnityEditor.MenuItem("Vertex Tracer/Trace")]
        public static void Go()
        {
            pointLights = Object.FindObjectsOfType<VertexPointLight>();
            shadowLights = Object.FindObjectsOfType<VertexAntiLight>();
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
            traces = 0;

            MeshBuilder meshBuilder = new MeshBuilder(meshFilter.transform.localToWorldMatrix, meshFilter.sharedMesh);
            var mesh = meshBuilder.mesh;
            meshFilter.sharedMesh = mesh;

            //var triangles = mesh.triangles;
            //var vertices = mesh.vertices;
            //Triangulate(mesh.triangles, out var triangles, mesh.vertices, out var vertices, mesh.uv, out var uv0);

            var vertices = meshBuilder.worldVertices;
            var triangles = meshBuilder.meshTriangles;

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

            //mesh.SetVertices(vertices);
            //mesh.SetTriangles(triangles, 0);
            //mesh.SetUVs(0, uv0);
            mesh.colors = colors;
            Debug.Log("Raytracing Finished: " + traces);
        }

        private static float InverseSquareLaw(float distance)
        {
            //if (distance == 0f) return 0f;
            return 1.0f / (distance * distance);
        }

        private static int traces = 0;

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

                var v1dir = (position - v1).normalized;
                if (math.dot(normal, v1dir) < 0f) continue; // early out by normal.
                var v2dir = (position - v2).normalized;
                var v3dir = (position - v3).normalized;

                float v1dist = Vector3.Distance(v1, position);
                float v2dist = Vector3.Distance(v2, position);
                float v3dist = Vector3.Distance(v3, position);

                // use hit from light to vertex and check hit position around the same as vertex position.
                bool v1cast = false;
                if (Physics.Raycast(position, -v1dir, out var hit1))
                    v1cast = (Vector3.Distance(hit1.point, v1) < 0.1f);

                bool v2cast = false;
                if (Physics.Raycast(position, -v2dir, out var hit2))
                    v2cast = (Vector3.Distance(hit2.point, v2) < 0.1f);

                bool v3cast = false;
                if (Physics.Raycast(position, -v3dir, out var hit3))
                    v3cast = (Vector3.Distance(hit3.point, v3) < 0.1f);

                traces += 3;

                float diff1 = math.max(math.dot(normal, v1dir), 0f);
                float diff2 = math.max(math.dot(normal, v2dir), 0f);
                float diff3 = math.max(math.dot(normal, v3dir), 0f);

                float v1sq = InverseSquareLaw(v1dist);
                float v2sq = InverseSquareLaw(v2dist);
                float v3sq = InverseSquareLaw(v3dist);

                v1sq *= pointLight.lightIntensity;
                v2sq *= pointLight.lightIntensity;
                v3sq *= pointLight.lightIntensity;

                //Debug.DrawLine(v1 + (normal * 0.01f) + (v1tocenter * 0.01f), v1 + (normal * 0.01f) + (v1tocenter * 0.01f) + (v1dir * v1dist), Color.green, 10f);

                float attenuation1 = 1.0f / (pointLight.constant + pointLight.linear * v1dist + pointLight.quadratic * (v1dist * v1dist));
                float attenuation2 = 1.0f / (pointLight.constant + pointLight.linear * v2dist + pointLight.quadratic * (v2dist * v2dist));
                float attenuation3 = 1.0f / (pointLight.constant + pointLight.linear * v3dist + pointLight.quadratic * (v3dist * v3dist));
                
                if (v1cast)
                    c1 += v1sq * pointLight.lightColor * diff1;
                if (v2cast)
                    c2 += v2sq * pointLight.lightColor * diff2;
                if (v3cast)
                    c3 += v3sq * pointLight.lightColor * diff3;
            }

            for (int i = 0; i < shadowLights.Length; i++)
            {
                var pointLight = shadowLights[i];
                var position = pointLight.transform.position;

                var v1dir = (position - v1).normalized;
                if (math.dot(normal, v1dir) < 0f) continue; // early out by normal.
                var v2dir = (position - v2).normalized;
                var v3dir = (position - v3).normalized;

                float v1dist = Vector3.Distance(v1, position);
                float v2dist = Vector3.Distance(v2, position);
                float v3dist = Vector3.Distance(v3, position);

                float v1sq = InverseSquareLaw(v1dist);
                float v2sq = InverseSquareLaw(v2dist);
                float v3sq = InverseSquareLaw(v3dist);

                v1sq *= pointLight.shadowIntensity;
                v2sq *= pointLight.shadowIntensity;
                v3sq *= pointLight.shadowIntensity;

                // use hit from light to vertex and check hit position around the same as vertex position.
                bool v1cast = false;
                if (Physics.Raycast(position, -v1dir, out var hit1))
                    v1cast = (Vector3.Distance(hit1.point, v1) < 0.1f);

                bool v2cast = false;
                if (Physics.Raycast(position, -v2dir, out var hit2))
                    v2cast = (Vector3.Distance(hit2.point, v2) < 0.1f);

                bool v3cast = false;
                if (Physics.Raycast(position, -v3dir, out var hit3))
                    v3cast = (Vector3.Distance(hit3.point, v3) < 0.1f);

                //Debug.DrawLine(v1 + (normal * 0.1f) + (v1tocenter * 0.1f), v1 + (normal * 0.2f) + (v1tocenter * 0.1f), Color.green, 10f);

                if (v1cast) c1 = Subtract(c1, v1sq);
                if (v2cast) c2 = Subtract(c2, v2sq);
                if (v3cast) c3 = Subtract(c3, v3sq);
            }

            return (c1, c2, c3);
        }

        private static Color Subtract(Color a, float b)
        {
            return new Color(a.r - b, a.g - b, a.b - b, a.a - b);
        }
    }
}