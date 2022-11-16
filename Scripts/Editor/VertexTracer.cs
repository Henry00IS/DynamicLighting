using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace AlpacaIT.VertexTracer
{
    public static class VertexTracer
    {
        private static int traces = 0;
        private static float meshBuilderTime = 0f;
        private static float tracingTime = 0f;

        private static VertexPointLight[] pointLights;
        private static VertexAntiLight[] shadowLights;

        private const int lightmapSize = 512;
        private const float lightmapSizeMin1 = lightmapSize - 1;

        [UnityEditor.MenuItem("Vertex Tracer/Trace")]
        public static void Go()
        {
            meshBuilderTime = 0f;
            tracingTime = 0f;
            traces = 0;

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

            Debug.Log("Raytracing Finished: " + traces + " traces in " + tracingTime + "s mesh edits " + meshBuilderTime + "s!");
        }

        private static void Raytrace(MeshFilter meshFilter)
        {
            var tt1 = Time.realtimeSinceStartup;
            MeshBuilder meshBuilder = new MeshBuilder(meshFilter.transform.localToWorldMatrix, meshFilter.sharedMesh);
            var mesh = meshBuilder.mesh;
            meshFilter.sharedMesh = mesh;
            meshBuilderTime += Time.realtimeSinceStartup - tt1;

            var pixels = new Color[lightmapSize * lightmapSize];
            {
                var vertices = meshBuilder.worldVertices;
                var uv1 = meshBuilder.meshUv1;
                var triangles = meshBuilder.meshTriangles;

                for (int i = 0; i < triangles.Count; i += 3)
                {
                    var v1 = vertices[triangles[i]];
                    var v2 = vertices[triangles[i] + 1];
                    var v3 = vertices[triangles[i] + 2];

                    var t1 = uv1[triangles[i]];
                    var t2 = uv1[triangles[i] + 1];
                    var t3 = uv1[triangles[i] + 2];

                    RaycastTriangle(ref pixels, v1, v2, v3, t1, t2, t3);
                }
            }

            Texture2D lightmap = new Texture2D(lightmapSize, lightmapSize, TextureFormat.RGBA32, false);
            // lightmap.filterMode = FilterMode.Point;
            lightmap.wrapMode = TextureWrapMode.Clamp;
            lightmap.SetPixels(pixels);
            lightmap.Apply();

            //var renderTexture = new RenderTexture(256, 256, 24);
            //RenderTexture.active = renderTexture;

            var renderer = meshFilter.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = new Material(renderer.sharedMaterial);
            renderer.sharedMaterial.name = "Vertex Tracer Material";
            renderer.sharedMaterial.SetTexture("_LightmapTex", lightmap);
            //RenderTexture.active = null;
            //renderTexture.Release();

            /*
            var vertices = meshBuilder.worldVertices;
            var triangles = meshBuilder.meshTriangles;

            var colors = new Color[vertices.Count];

            for (int i = 0; i < triangles.Count; i += 3)
            {
                var v1 = vertices[triangles[i]];
                var v2 = vertices[triangles[i] + 1];
                var v3 = vertices[triangles[i] + 2];

                var t2 = Time.realtimeSinceStartup;
                var res = Raycast(v1, v2, v3);
                tracingTime += Time.realtimeSinceStartup - t2;

                colors[triangles[i]] = res.c1;
                colors[triangles[i + 1]] = res.c2;
                colors[triangles[i + 2]] = res.c3;
            }

            mesh.colors = colors;*/
        }

        private static void SetPixel(ref Color[] pixels, int x, int y, Color color)
        {
            if (x < 0 || y < 0 || x >= lightmapSize || y >= lightmapSize) return;
            pixels[y * lightmapSize + x] = color;
        }

        private static Vector3 UvTo3d(Vector2 uv, Vector3 v1, Vector3 v2, Vector3 v3, Vector2 t1, Vector2 t2, Vector2 t3)
        {
            // calculate triangle area - if zero, skip it
            var a = Area(t1, t2, t3); if (a == 0f) return Vector3.zero;

            // calculate barycentric coordinates of u1, u2 and u3
            // if anyone is negative, point is outside the triangle: skip it
            var a1 = Area(t2, t3, uv) / a; if (a1 < 0f) return Vector3.zero;
            var a2 = Area(t3, t1, uv) / a; if (a2 < 0f) return Vector3.zero;
            var a3 = Area(t1, t2, uv) / a; if (a3 < 0f) return Vector3.zero;

            // point inside the triangle - find mesh position by interpolation...
            return a1 * v1 + a2 * v2 + a3 * v3;
        }

        private static void RaycastTriangle(ref Color[] pixels, Vector3 v1, Vector3 v2, Vector3 v3, Vector2 t1, Vector2 t2, Vector2 t3)
        {
            Plane plane = new Plane(v1, v2, v3);

            //Vector3 center = (v1 + v2 + v3) / 3f;
            //v1 -= (center - v1).normalized * ((1.0f / lightmapSizeMin1) * 4f);
            //v2 -= (center - v2).normalized * ((1.0f / lightmapSizeMin1) * 4f);
            //v3 -= (center - v3).normalized * ((1.0f / lightmapSizeMin1) * 4f);

            Vector2 center2 = (t1 + t2 + t3) / 3f;
            t1 -= (center2 - t1).normalized * ((1.0f / lightmapSizeMin1) * 4f);
            t2 -= (center2 - t2).normalized * ((1.0f / lightmapSizeMin1) * 4f);
            t3 -= (center2 - t3).normalized * ((1.0f / lightmapSizeMin1) * 4f);

            for (int x = 0; x < lightmapSize; x++)
            {
                for (int y = 0; y < lightmapSize; y++)
                {
                    float xx = 0.0f;
                    float yy = 0.0f;
                    if (x != 0) xx = x / (lightmapSizeMin1);
                    if (y != 0) yy = y / (lightmapSizeMin1);

                    var world = UvTo3d(new Vector2(xx, yy), v1, v2, v3, t1, t2, t3);
                    if (world.Equals(Vector3.zero)) continue;

                    Color px = Color.black;
                    for (int i = 0; i < pointLights.Length; i++)
                    {
                        var pointLight = pointLights[i];
                        px += Raycast(pointLight, world, plane);
                    }

                    SetPixel(ref pixels, x, y, px);
                }
            }

            /*
            var t1x = Mathf.FloorToInt(t1.x * 255);
            var t1y = Mathf.FloorToInt(t1.y * 255);
            var t2x = Mathf.FloorToInt(t2.x * 255);
            var t2y = Mathf.FloorToInt(t2.y * 255);
            var t3x = Mathf.FloorToInt(t3.x * 255);
            var t3y = Mathf.FloorToInt(t3.y * 255);

            if (t1x < t2x)
            {
                if (t1y < t2y)
                {
                    for (int x = t1x - 2; x <= t2x + 1; x++)
                    {
                        for (int y = t1y - 2; y <= t2y + 1; y++)
                        {
                            if (t1y < t3y)
                            {
                                if (y >= t1y && y <= t3y)
                                {
                                    Debug.DrawLine(Vector3.Lerp(v1, v2, x / (float)(t2x - t1x)), Vector3.Lerp(v1, v2, y / (float)t2y) + Vector3.up * 0.1f, Color.green, 10f);

                                    SetPixel(ref pixels, x, y, Color.white);
                                }
                            }
                        }
                    }
                }
            }*/

            /*
            var v1dir = (position - v1).normalized;
            var v2dir = (position - v2).normalized;
            var v3dir = (position - v3).normalized;
            if (Physics.Raycast(position, UnityEngine.Random.onUnitSphere, out var hit1))
            {
                var coord = hit1.lightmapCoord;
                SetPixel(ref pixels, Mathf.FloorToInt(coord.x * 255), Mathf.FloorToInt(coord.y * 255), Color.white);

                Debug.Log(coord);
            }*/
        }

        private static Color Raycast(VertexPointLight pointLightsi, Vector3 v1, Plane plane)
        {
            Color c1 = Color.black;

            var normal = plane.normal;

            var pointLight = pointLightsi;
            var radius = pointLight.lightRadius;
            if (radius == 0.0f) return c1;
            var position = pointLight.transform.position;

            var v1dir = (position - v1).normalized;
            if (math.dot(normal, v1dir) < 0f) return c1; // early out by normal.

            float v1dist = Vector3.Distance(v1, position);

            // trace from the light to the vertex and check whether we hit close to the vertex.
            // we check whether the vertex is within the light radius or else skip it.
            bool v1cast = false;
            if (v1dist <= radius && Physics.Raycast(position, -v1dir, out var hit1))
                v1cast = (Vector3.Distance(hit1.point, v1) < 0.01f);

            traces += (v1dist <= radius ? 1 : 0);

            //float diff1 = math.max(math.dot(normal, v1dir), 0f);

            //float attenuation1 = math.clamp(1.0f - v1dist * v1dist / (radius * radius), 0.0f, 1.0f) * pointLight.lightIntensity;

            if (v1cast)
            {
                if (pointLightsi.lightChannel == 0)
                {
                    c1 = Color.red;
                }
                else if (pointLightsi.lightChannel == 1)
                {
                    c1 = Color.green;
                }
            }
                //c1 += attenuation1 * pointLight.lightColor * diff1;

            return c1;
        }

        private static (Color c1, Color c2, Color c3) Raycast(Vector3 v1, Vector3 v2, Vector3 v3)
        {
            Color c1 = new Color(), c2 = new Color(), c3 = new Color();
            c1.a = 1f;
            c2.a = 1f;
            c3.a = 1f;

            var plane = new Plane(v1, v2, v3);
            var normal = plane.normal;

            for (int i = 0; i < pointLights.Length; i++)
            {
                var pointLight = pointLights[i];
                var radius = pointLight.lightRadius;
                if (radius == 0.0f) continue;
                var position = pointLight.transform.position;

                var v1dir = (position - v1).normalized;
                if (math.dot(normal, v1dir) < 0f) continue; // early out by normal.
                var v2dir = (position - v2).normalized;
                var v3dir = (position - v3).normalized;

                float v1dist = Vector3.Distance(v1, position);
                float v2dist = Vector3.Distance(v2, position);
                float v3dist = Vector3.Distance(v3, position);

                // trace from the light to the vertex and check whether we hit close to the vertex.
                // we check whether the vertex is within the light radius or else skip it.
                bool v1cast = false;
                if (v1dist <= radius && Physics.Raycast(position, -v1dir, out var hit1))
                    v1cast = (Vector3.Distance(hit1.point, v1) < 0.1f);

                bool v2cast = false;
                if (v2dist <= radius && Physics.Raycast(position, -v2dir, out var hit2))
                    v2cast = (Vector3.Distance(hit2.point, v2) < 0.1f);

                bool v3cast = false;
                if (v3dist <= radius && Physics.Raycast(position, -v3dir, out var hit3))
                    v3cast = (Vector3.Distance(hit3.point, v3) < 0.1f);

                traces += (v1dist <= radius ? 1 : 0);
                traces += (v2dist <= radius ? 1 : 0);
                traces += (v3dist <= radius ? 1 : 0);

                float diff1 = math.max(math.dot(normal, v1dir), 0f);
                float diff2 = math.max(math.dot(normal, v2dir), 0f);
                float diff3 = math.max(math.dot(normal, v3dir), 0f);

                //Debug.DrawLine(v1 + (normal * 0.01f) + (v1tocenter * 0.01f), v1 + (normal * 0.01f) + (v1tocenter * 0.01f) + (v1dir * v1dist), Color.green, 10f);

                float attenuation1 = math.clamp(1.0f - v1dist * v1dist / (radius * radius), 0.0f, 1.0f) * pointLight.lightIntensity;
                float attenuation2 = math.clamp(1.0f - v2dist * v2dist / (radius * radius), 0.0f, 1.0f) * pointLight.lightIntensity;
                float attenuation3 = math.clamp(1.0f - v3dist * v3dist / (radius * radius), 0.0f, 1.0f) * pointLight.lightIntensity;

                if (v1cast)
                    c1 += attenuation1 * pointLight.lightColor * diff1;
                if (v2cast)
                    c2 += attenuation2 * pointLight.lightColor * diff2;
                if (v3cast)
                    c3 += attenuation3 * pointLight.lightColor * diff3;
            }

            for (int i = 0; i < shadowLights.Length; i++)
            {
                var pointLight = shadowLights[i];
                var radius = pointLight.shadowRadius;
                if (radius == 0.0f) continue;
                var position = pointLight.transform.position;

                var v1dir = (position - v1).normalized;
                if (math.dot(normal, v1dir) < 0f) continue; // early out by normal.
                var v2dir = (position - v2).normalized;
                var v3dir = (position - v3).normalized;

                float v1dist = Vector3.Distance(v1, position);
                float v2dist = Vector3.Distance(v2, position);
                float v3dist = Vector3.Distance(v3, position);

                // trace from the light to the vertex and check whether we hit close to the vertex.
                // we check whether the vertex is within the light radius or else skip it.
                bool v1cast = false;
                if (v1dist <= radius && Physics.Raycast(position, -v1dir, out var hit1))
                    v1cast = (Vector3.Distance(hit1.point, v1) < 0.1f);

                bool v2cast = false;
                if (v2dist <= radius && Physics.Raycast(position, -v2dir, out var hit2))
                    v2cast = (Vector3.Distance(hit2.point, v2) < 0.1f);

                bool v3cast = false;
                if (v3dist <= radius && Physics.Raycast(position, -v3dir, out var hit3))
                    v3cast = (Vector3.Distance(hit3.point, v3) < 0.1f);

                traces += 3;

                float diff1 = math.max(math.dot(normal, v1dir), 0f);
                float diff2 = math.max(math.dot(normal, v2dir), 0f);
                float diff3 = math.max(math.dot(normal, v3dir), 0f);

                //Debug.DrawLine(v1 + (normal * 0.01f) + (v1tocenter * 0.01f), v1 + (normal * 0.01f) + (v1tocenter * 0.01f) + (v1dir * v1dist), Color.green, 10f);

                float attenuation1 = math.clamp(1.0f - v1dist * v1dist / (radius * radius), 0.0f, 1.0f) * pointLight.shadowIntensity;
                float attenuation2 = math.clamp(1.0f - v2dist * v2dist / (radius * radius), 0.0f, 1.0f) * pointLight.shadowIntensity;
                float attenuation3 = math.clamp(1.0f - v3dist * v3dist / (radius * radius), 0.0f, 1.0f) * pointLight.shadowIntensity;

                if (v1cast) c1 = Subtract(c1, attenuation1 * diff1);
                if (v2cast) c2 = Subtract(c2, attenuation2 * diff2);
                if (v3cast) c3 = Subtract(c3, attenuation3 * diff3);
            }

            return (c1, c2, c3);
        }

        private static Color Subtract(Color a, float b)
        {
            return new Color(a.r - b, a.g - b, a.b - b, a.a - b);
        }

        public static Texture2D toTexture2D(RenderTexture rTex)
        {
            Texture2D tex = new Texture2D(256, 256, TextureFormat.RGBA32, false);
            // ReadPixels looks at the active RenderTexture.
            RenderTexture.active = rTex;
            tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
            tex.Apply();
            return tex;
        }

        // calculate signed triangle area using a kind of "2D cross product":
        public static float Area(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            var v1 = p1 - p3;
            var v2 = p2 - p3;
            return (v1.x * v2.y - v1.y * v2.x) / 2f;
        }
    }
}