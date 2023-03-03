using Unity.Mathematics;
using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>The raytracer that calculates shadows for all dynamic lights.</summary>
    public class DynamicLightingTracer
    {
        /// <summary>The maximum size of the lightmap to be baked (defaults to 2048x2048).</summary>
        public int maximumLightmapSize { get; set; } = 2048;

        private int traces = 0;
        private float tracingTime = 0f;
        private float seamTime = 0f;
        private long vramTotal = 0;
        private DynamicLight[] pointLights;
        private int lightmapSize = 2048;
        private float lightmapSizeMin1;
        private int uniqueIdentifier = 0;
        private LayerMask raycastLayermask = ~0;
        private int pixelDensityPerSquareMeter = 128;

#if UNITY_EDITOR
        private float progressBarLastUpdate = 0f;
        private bool progressBarCancel = false;
#endif

        /// <summary>Creates a new instance of the dynamic lighting tracer.</summary>
        public DynamicLightingTracer()
        {
            Prepare();
        }

        /// <summary>Resets the internal state so that it's ready for raytracing.</summary>
        private void Prepare()
        {
            traces = 0;
            tracingTime = 0f;
            seamTime = 0f;
            vramTotal = 0;
            pointLights = null;
            lightmapSizeMin1 = lightmapSize - 1;
            uniqueIdentifier = 0;
            raycastLayermask = DynamicLightManager.Instance.raytraceLayers;
            pixelDensityPerSquareMeter = DynamicLightManager.Instance.pixelDensityPerSquareMeter;
#if UNITY_EDITOR
            progressBarLastUpdate = 0f;
            progressBarCancel = false;
#endif
        }

        /// <summary>Starts raytracing the world.</summary>
        public void StartRaytracing()
        {
            try
            {
                // reset the internal state.
                Prepare();

                // find all of the dynamic lights in the scene and assign channels.
                pointLights = DynamicLightManager.FindDynamicLightsInScene().ToArray();
                AssignPointLightChannels();

                var meshFilters = Object.FindObjectsOfType<MeshFilter>();
                for (int i = 0; i < meshFilters.Length; i++)
                {
                    var meshFilter = meshFilters[i];
                    if (meshFilter.gameObject.isStatic)
                    {
                        float progressMin = i / (float)meshFilters.Length;
                        float progressMax = (i + 1) / (float)meshFilters.Length;

                        Raytrace(meshFilter, progressMin, progressMax);
#if UNITY_EDITOR
                        if (progressBarCancel) break;
#endif
                    }
                }

                Debug.Log("Raytracing Finished: " + traces + " traces in " + tracingTime + "s! Seams padding in " + seamTime + "s! VRAM estimation: " + BytesToUnitString(vramTotal));
                DynamicLightManager.Instance.Reload();
#if UNITY_EDITOR
                UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
#endif
            }
            catch
            {
                throw;
            }
            finally
            {
#if UNITY_EDITOR
                UnityEditor.EditorUtility.ClearProgressBar();
#endif
            }
        }

#if UNITY_EDITOR

        [UnityEditor.MenuItem("Dynamic Lighting/Raytrace Scene: 512", false, 0)]
        private static void EditorRaytrace512()
        {
            var tracer = new DynamicLightingTracer();
            tracer.maximumLightmapSize = 512;
            tracer.StartRaytracing();
        }

        [UnityEditor.MenuItem("Dynamic Lighting/Raytrace Scene: 1024", false, 1)]
        private static void EditorRaytrace1024()
        {
            var tracer = new DynamicLightingTracer();
            tracer.maximumLightmapSize = 1024;
            tracer.StartRaytracing();
        }

        [UnityEditor.MenuItem("Dynamic Lighting/Raytrace Scene: 2048 (Recommended)", false, 1)]
        private static void EditorRaytrace2048()
        {
            var tracer = new DynamicLightingTracer();
            tracer.maximumLightmapSize = 2048;
            tracer.StartRaytracing();
        }

        [UnityEditor.MenuItem("Dynamic Lighting/Raytrace Scene: 4096", false, 1)]
        private static void EditorRaytrace4096()
        {
            var tracer = new DynamicLightingTracer();
            tracer.maximumLightmapSize = 4096;
            tracer.StartRaytracing();
        }

#endif

        private static string BytesToUnitString(long bytes)
        {
            if (bytes < (long)1024) return bytes + "B";
            if (bytes < (long)1024 * 1024) return bytes / 1024 + "KiB";
            if (bytes < (long)1024 * 1024 * 1024) return bytes / 1024 / 1024 + "MiB";
            if (bytes < (long)1024 * 1024 * 1024 * 1024) return bytes / 1024 / 1024 / 1024 + "GiB";
            if (bytes < (long)1024 * 1024 * 1024 * 1024 * 1024) return bytes / 1024 / 1024 / 1024 / 1024 + "TiB"; // the future!
            return bytes + "B";
        }

        private void AssignPointLightChannels()
        {
            // first reset all the channels to an invalid value.
            for (int i = 0; i < pointLights.Length; i++)
            {
                var light = pointLights[i];
                light.lightChannel = 255;
            }

            for (int i = 0; i < pointLights.Length; i++)
            {
                var light = pointLights[i];

                if (TryFindFreeLightChannelAt(light.transform.position, light.lightRadius, out var channel))
                {
#if UNITY_EDITOR
                    // required to use serialized object to override fields in prefabs:
                    // this does: light.lightChannel = channel;
                    var serializedObject = new UnityEditor.SerializedObject(light);
                    var lightChannelProperty = serializedObject.FindProperty(nameof(DynamicLight.lightChannel));
                    lightChannelProperty.intValue = (int)channel;
                    serializedObject.ApplyModifiedProperties();
#else
                    light.lightChannel = channel;
#endif
                }
                else
                {
                    Debug.LogError("More than 32 lights intersect at the same position! This is not supported! Please spread your light sources further apart or reduce their radius.");
                }
            }
        }

        private bool TryFindFreeLightChannelAt(Vector3 position, float radius, out uint channel)
        {
            // find all used channels that intersect our radius.
            var channels = new bool[32];
            for (int i = 0; i < pointLights.Length; i++)
            {
                var light = pointLights[i];
                if (light.lightChannel == 255) continue;

                if (MathEx.SpheresIntersect(light.transform.position, light.lightRadius, position, radius))
                    channels[light.lightChannel] = true;
            }

            // find a free channel.
            for (channel = 0; channel < channels.Length; channel++)
                if (!channels[channel])
                    return true;

            return false;
        }

        private void Raytrace(MeshFilter meshFilter, float progressMin, float progressMax)
        {
            var meshBuilder = new MeshBuilder(meshFilter.transform.localToWorldMatrix, meshFilter.sharedMesh);
            lightmapSize = MathEx.SurfaceAreaToTextureSize(meshBuilder.surfaceArea, pixelDensityPerSquareMeter);
            if (lightmapSize > maximumLightmapSize)
                lightmapSize = maximumLightmapSize;
            lightmapSizeMin1 = lightmapSize - 1;

#if UNITY_EDITOR
            var progressTitle = "Raytracing Scene " + meshBuilder.surfaceArea.ToString("0.00") + "m² (" + lightmapSize + "x" + lightmapSize + ")";
            var progressDescription = "Raytracing " + meshFilter.name;
            if (meshBuilder.meshUv1.Length == 0)
            {
                Debug.LogWarning("Raytracer skipping " + meshFilter.name + " because it does not have uv1 lightmap coordinates!");
                return;
            }
            else
            {
                // estimate the amount of vram required (purely statistical).
                long vramLightmap = lightmapSize * lightmapSize * 4; // uint32
                vramTotal += vramLightmap;

                Debug.Log(meshFilter.name + " surface area: " + meshBuilder.surfaceArea.ToString("0.00") + "m² lightmap size: " + lightmapSize + "x" + lightmapSize + " VRAM: " + BytesToUnitString(vramLightmap), meshFilter);
            }
#endif

            var tt1 = Time.realtimeSinceStartup;
            var pixels_lightmap = new uint[lightmapSize * lightmapSize];
            var pixels_visited = new uint[lightmapSize * lightmapSize];
            {
                var vertices = meshBuilder.worldVertices;
                var uv1 = meshBuilder.meshUv1;
                var triangles = meshBuilder.meshTriangles;

                for (int i = 0; i < triangles.Length; i += 3)
                {
#if UNITY_EDITOR
                    if (Time.realtimeSinceStartup - progressBarLastUpdate > 0.25f)
                    {
                        progressBarLastUpdate = Time.realtimeSinceStartup;
                        if (UnityEditor.EditorUtility.DisplayCancelableProgressBar(progressTitle, progressDescription, Mathf.Lerp(progressMin, progressMax, i / (float)triangles.Length)))
                        {
                            progressBarCancel = true;
                            break;
                        }
                    }
#endif
                    var v1 = vertices[triangles[i]];
                    var v2 = vertices[triangles[i + 1]];
                    var v3 = vertices[triangles[i + 2]];

                    var t1 = uv1[triangles[i]];
                    var t2 = uv1[triangles[i + 1]];
                    var t3 = uv1[triangles[i + 2]];

                    RaycastTriangle(ref pixels_lightmap, ref pixels_visited, v1, v2, v3, t1, t2, t3);
                }
            }
            tracingTime += Time.realtimeSinceStartup - tt1;

            tt1 = Time.realtimeSinceStartup;
            {
                for (int x = 0; x < lightmapSize; x++)
                {
                    for (int y = 0; y < lightmapSize; y++)
                    {
                        // if we find an unvisited pixel it will appear as a black seam in the scene.
                        var visited = GetPixel(ref pixels_visited, x, y);
                        if (visited == 0)
                        {
                            uint res = 0;

                            // fetch 5x5 "visited" pixels (where p22 is the center).

                            // bool p00 = GetPixel(ref pixels_visited, x - 2, y - 2) == 1;
                            // bool p10 = GetPixel(ref pixels_visited, x - 1, y - 2) == 1;
                            bool p20 = GetPixel(ref pixels_visited, x, y - 2) == 1;
                            // bool p30 = GetPixel(ref pixels_visited, x + 1, y - 2) == 1;
                            // bool p40 = GetPixel(ref pixels_visited, x + 2, y - 2) == 1;

                            // bool p01 = GetPixel(ref pixels_visited, x - 2, y - 1) == 1;
                            // bool p11 = GetPixel(ref pixels_visited, x - 1, y - 1) == 1;
                            bool p21 = GetPixel(ref pixels_visited, x, y - 1) == 1;
                            // bool p31 = GetPixel(ref pixels_visited, x + 1, y - 1) == 1;
                            // bool p41 = GetPixel(ref pixels_visited, x + 2, y - 1) == 1;
                            
                            bool p02 = GetPixel(ref pixels_visited, x - 2, y) == 1;
                            bool p12 = GetPixel(ref pixels_visited, x - 1, y) == 1;
                            bool p32 = GetPixel(ref pixels_visited, x + 1, y) == 1;
                            bool p42 = GetPixel(ref pixels_visited, x + 2, y) == 1;
                            
                            // bool p03 = GetPixel(ref pixels_visited, x - 2, y + 1) == 1;
                            // bool p13 = GetPixel(ref pixels_visited, x - 1, y + 1) == 1;
                            bool p23 = GetPixel(ref pixels_visited, x, y + 1) == 1;
                            // bool p33 = GetPixel(ref pixels_visited, x + 1, y + 1) == 1;
                            // bool p43 = GetPixel(ref pixels_visited, x + 2, y + 1) == 1;
                            
                            // bool p04 = GetPixel(ref pixels_visited, x - 2, y + 2) == 1;
                            // bool p14 = GetPixel(ref pixels_visited, x - 1, y + 2) == 1;
                            bool p24 = GetPixel(ref pixels_visited, x, y + 2) == 1;
                            // bool p34 = GetPixel(ref pixels_visited, x + 1, y + 2) == 1;
                            // bool p44 = GetPixel(ref pixels_visited, x + 2, y + 2) == 1;

                            // fetch 5x5 "lightmap" pixels (where p22 is the center).

                            // uint l00 = GetPixel(ref pixels_lightmap, x - 2, y - 2);
                            // uint l10 = GetPixel(ref pixels_lightmap, x - 1, y - 2);
                            uint l20 = GetPixel(ref pixels_lightmap, x, y - 2);
                            // uint l30 = GetPixel(ref pixels_lightmap, x + 1, y - 2);
                            // uint l40 = GetPixel(ref pixels_lightmap, x + 2, y - 2);
                            
                            // uint l01 = GetPixel(ref pixels_lightmap, x - 2, y - 1);
                            // uint l11 = GetPixel(ref pixels_lightmap, x - 1, y - 1);
                            uint l21 = GetPixel(ref pixels_lightmap, x, y - 1);
                            // uint l31 = GetPixel(ref pixels_lightmap, x + 1, y - 1);
                            // uint l41 = GetPixel(ref pixels_lightmap, x + 2, y - 1);
                            
                            uint l02 = GetPixel(ref pixels_lightmap, x - 2, y);
                            uint l12 = GetPixel(ref pixels_lightmap, x - 1, y);
                            uint l32 = GetPixel(ref pixels_lightmap, x + 1, y);
                            uint l42 = GetPixel(ref pixels_lightmap, x + 2, y);
                            
                            // uint l03 = GetPixel(ref pixels_lightmap, x - 2, y + 1);
                            // uint l13 = GetPixel(ref pixels_lightmap, x - 1, y + 1);
                            uint l23 = GetPixel(ref pixels_lightmap, x, y + 1);
                            // uint l33 = GetPixel(ref pixels_lightmap, x + 1, y + 1);
                            // uint l43 = GetPixel(ref pixels_lightmap, x + 2, y + 1);
                            
                            // uint l04 = GetPixel(ref pixels_lightmap, x - 2, y + 2);
                            // uint l14 = GetPixel(ref pixels_lightmap, x - 1, y + 2);
                            uint l24 = GetPixel(ref pixels_lightmap, x, y + 2);
                            // uint l34 = GetPixel(ref pixels_lightmap, x + 1, y + 2);
                            // uint l44 = GetPixel(ref pixels_lightmap, x + 2, y + 2);

                            // x x x x x
                            // x x x x x
                            // x x C x x
                            // x x x x x
                            // x x x x x

                            // p00 p10 p20 p30 p40
                            // p01 p11 p21 p31 p41
                            // p02 p12 p22 p32 p42
                            // p03 p13 p23 p33 p43
                            // p04 p14 p24 p34 p44

                            //
                            //     x
                            //   x C x
                            //     x
                            //

                            // left 1x
                            if (p12)
                                res |= l12;
                            // right 1x
                            if (p32)
                                res |= l32;
                            // up 1x
                            if (p21)
                                res |= l21;
                            // down 1x
                            if (p23)
                                res |= l23;

                            //     x
                            //
                            // x   C   x
                            //
                            //     x

                            // left 2x
                            if (!p12 && p02)
                                res |= l02;
                            // right 2x
                            if (!p32 && p42)
                                res |= l42;
                            // up 2x
                            if (!p21 && p20)
                                res |= l20;
                            // down 2x
                            if (!p23 && p24)
                                res |= l24;
                            
                            SetPixel(ref pixels_lightmap, x, y, res);
                        }
                    }
                }
            }
            seamTime += Time.realtimeSinceStartup - tt1;

            Lightmap lightmap;
            if (!meshFilter.TryGetComponent(out lightmap))
                lightmap = meshFilter.gameObject.AddComponent<Lightmap>();
            lightmap.resolution = lightmapSize;
            lightmap.identifier = uniqueIdentifier++;

            var sceneStorageDirectory = EditorUtilities.CreateAndGetActiveSceneStorageDirectory();
            if (sceneStorageDirectory != null)
            {
                EditorUtilities.WriteLightmapData(lightmap.identifier, pixels_lightmap);
            }
            else
            {
                Debug.LogError("Unable to find or create the active scene storage directory or write the lightmap file!");
            }
        }

        private uint GetPixel(ref uint[] pixels, int x, int y)
        {
            if (x < 0 || y < 0 || x >= lightmapSize || y >= lightmapSize) return 0;
            return pixels[y * lightmapSize + x];
        }

        private void SetPixel(ref uint[] pixels, int x, int y, uint color)
        {
            if (x < 0 || y < 0 || x >= lightmapSize || y >= lightmapSize) return;
            pixels[y * lightmapSize + x] = color;
        }

        private static Vector3 UvTo3d(Vector2 uv, Vector3 v1, Vector3 v2, Vector3 v3, Vector2 t1, Vector2 t2, Vector2 t3)
        {
            // calculate triangle area - if zero, skip it.
            var a = Area(t1, t2, t3); if (a == 0f) return Vector3.zero;

            // calculate barycentric coordinates of u1, u2 and u3.
            // if anyone is negative, point is outside the triangle: skip it.
            var a1 = Area(t2, t3, uv) / a; if (a1 < 0f) return Vector3.zero;
            var a2 = Area(t3, t1, uv) / a; if (a2 < 0f) return Vector3.zero;
            var a3 = Area(t1, t2, uv) / a; if (a3 < 0f) return Vector3.zero;

            // point inside the triangle - find mesh position by interpolation.
            return a1 * v1 + a2 * v2 + a3 * v3;
        }

        private void RaycastTriangle(ref uint[] pixels_lightmap, ref uint[] pixels_visited, Vector3 v1, Vector3 v2, Vector3 v3, Vector2 t1, Vector2 t2, Vector2 t3)
        {
            // skip degenerate triangles.
            Vector3 normal = new Plane(v1, v2, v3).normal;
            if (normal.Equals(Vector3.zero)) { return; };

            // calculate the bounding box of the polygon in UV space.
            // we only have to raycast these pixels and can skip the rest.
            var triangleBoundingBox = ComputeTriangleBoundingBox(t1, t2, t3);
            var minX = Mathf.FloorToInt(triangleBoundingBox.xMin * lightmapSizeMin1);
            var minY = Mathf.FloorToInt(triangleBoundingBox.yMin * lightmapSizeMin1);
            var maxX = Mathf.CeilToInt(triangleBoundingBox.xMax * lightmapSizeMin1);
            var maxY = Mathf.CeilToInt(triangleBoundingBox.yMax * lightmapSizeMin1);

            for (int x = minX; x < maxX; x++)
            {
                for (int y = minY; y < maxY; y++)
                {
                    float xx = x / lightmapSizeMin1;
                    float yy = y / lightmapSizeMin1;

                    var world = UvTo3d(new Vector2(xx, yy), v1, v2, v3, t1, t2, t3);
                    if (world.Equals(Vector3.zero)) continue;

                    uint px = 0;
                    for (int i = 0; i < pointLights.Length; i++)
                    {
                        var pointLight = pointLights[i];
                        px |= Raycast(pointLight, world, normal);
                    }

                    // write this pixel into the visited map.
                    SetPixel(ref pixels_visited, x, y, 1);

                    if (px > 0)
                    {
                        SetPixel(ref pixels_lightmap, x, y, px);
                    }
                }
            }
        }

        private uint Raycast(DynamicLight pointLight, Vector3 world, Vector3 normal)
        {
            var radius = pointLight.lightRadius;
            if (radius == 0.0f) return 0; // early out by radius.

            var position = pointLight.transform.position;
            float distance = Vector3.Distance(world, position);
            if (distance > radius) return 0; // early out by distance.

            var direction = (position - world).normalized;
            if (math.dot(normal, direction) < 0f) return 0; // early out by normal.

            // trace from the light to the world position and check whether we hit close to it.
            traces++;
            if (Physics.Raycast(position, -direction, out var hit, radius, raycastLayermask, QueryTriggerInteraction.Ignore))
                if (Vector3.Distance(hit.point, world) < 0.01f)
                    return (uint)1 << ((int)pointLight.lightChannel);

            return 0;
        }

        // calculate signed triangle area using a kind of "2D cross product":
        private static float Area(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            var v1 = p1 - p3;
            var v2 = p2 - p3;
            return (v1.x * v2.y - v1.y * v2.x) / 2f;
        }

        private static Rect ComputeTriangleBoundingBox(Vector2 a, Vector2 b, Vector2 c)
        {
            float sx1 = a.x;
            float sx2 = b.x;
            float sx3 = c.x;
            float sy1 = a.y;
            float sy2 = b.y;
            float sy3 = c.y;

            float xmax = sx1 > sx2 ? (sx1 > sx3 ? sx1 : sx3) : (sx2 > sx3 ? sx2 : sx3);
            float ymax = sy1 > sy2 ? (sy1 > sy3 ? sy1 : sy3) : (sy2 > sy3 ? sy2 : sy3);
            float xmin = sx1 < sx2 ? (sx1 < sx3 ? sx1 : sx3) : (sx2 < sx3 ? sx2 : sx3);
            float ymin = sy1 < sy2 ? (sy1 < sy3 ? sy1 : sy3) : (sy2 < sy3 ? sy2 : sy3);

            return new Rect(xmin, ymin, xmax - xmin, ymax - ymin);
        }
    }
}