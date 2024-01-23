using AlpacaIT.DynamicLighting.Internal;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>The raytracer that calculates shadows for all dynamic lights.</summary>
    internal partial class DynamicLightingTracer
    {
        /// <summary>The maximum size of the lightmap to be baked (defaults to 2048x2048).</summary>
        public int maximumLightmapSize { get; set; } = 2048;

        /// <summary>Called when this tracer instance has been cancelled.</summary>
#pragma warning disable CS0067

        public event System.EventHandler<System.EventArgs> cancelled;

#pragma warning restore CS0067

        private int traces = 0;
        private int optimizationLightsRemoved = 0;
        private BenchmarkTimer tracingTime;
        private BenchmarkTimer seamTime;
        private BenchmarkTimer optimizationTime;
        private ulong vramTotal = 0;
        private DynamicLight[] pointLights;
        private CachedLightData[] pointLightsCache;
        private RaycastProcessor raycastProcessor;
        private int lightmapSize = 2048;
        private float lightmapSizeMin1;
        private int uniqueIdentifier = 0;
        private LayerMask raycastLayermask = ~0;
        private int pixelDensityPerSquareMeter = 128;
        private DynamicLightManager dynamicLightManager;

#if UNITY_EDITOR
        private float progressBarLastUpdate = 0f;
        private bool progressBarCancel = false;
#endif

        /// <summary>Resets the internal state so that it's ready for raytracing.</summary>
        private void Prepare()
        {
            // fetch the dynamic light manager instance once.
            dynamicLightManager = DynamicLightManager.Instance;

            // prepare to process raycasts on the job system.
            raycastProcessor = new RaycastProcessor();

            traces = 0;
            optimizationLightsRemoved = 0;
            tracingTime = new BenchmarkTimer();
            seamTime = new BenchmarkTimer();
            optimizationTime = new BenchmarkTimer();
            vramTotal = 0;
            lightmapSizeMin1 = lightmapSize - 1;
            uniqueIdentifier = 0;
            raycastLayermask = dynamicLightManager.raytraceLayers;
            pixelDensityPerSquareMeter = dynamicLightManager.pixelDensityPerSquareMeter;
#if UNITY_EDITOR
            progressBarLastUpdate = 0f;
            progressBarCancel = false;
#endif
        }

        /// <summary>
        /// Finds all dynamic light sources in the scene and calculates the Bounding Volume Hierarchy.
        /// </summary>
        private void CreateDynamicLightsBvh()
        {
            // find all of the dynamic lights in the scene.
            var dynamicLights = DynamicLightManager.FindDynamicLightsInScene().ToArray();

            // there must be at least one light in order to create the bounding volume hierarchy.
            if (dynamicLights.Length > 0)
            {
                // create the dynamic lights bounding volume hierarchy and write it to disk.
                var bvhDynamicLights = new BvhLightStructure(dynamicLights);
                if (!Utilities.WriteLightmapData(0, "DynamicLightsBvh", bvhDynamicLights.ToUInt32Array()))
                    Debug.LogError($"Unable to write the dynamic lights bounding volume hierarchy file in the active scene resources directory!");

                // create the point lights array with the order the bvh tree desires.
                pointLights = new DynamicLight[dynamicLights.Length];
                for (int i = 0; i < dynamicLights.Length; i++)
                    pointLights[i] = dynamicLights[bvhDynamicLights.dynamicLightsIdx[i]]; // pigeonhole sort!
            }
            else
            {
                pointLights = dynamicLights;
            }
        }

        /// <summary>Starts raytracing the world.</summary>
        public void StartRaytracing()
        {
            var queriesHitBackfacesPrevious = Physics.queriesHitBackfaces;
            var queriesHitTriggersPrevious = Physics.queriesHitTriggers;

            try
            {
                // we require this for linecasts starting inside of objects (e.g. the floor under a box).
                // it also prevents light from shining through one-sided walls (inverted world workflow).
                Physics.queriesHitBackfaces = true;

                // due to a missing parameter in the unity job system we must ignore triggers globally.
                Physics.queriesHitTriggers = false;

                // reset the internal state and collect required scene information.
                Prepare();

#if UNITY_EDITOR
                // delete all of the old lightmap data in the scene and on disk.
                dynamicLightManager.EditorDeleteLightmaps();
#endif
                // find all light sources and calculate the bounding volume hierarchy.
                CreateDynamicLightsBvh();

                // assign channels to all dynamic lights in the scene.
                ChannelsUpdatePointLightsInScene();

                // cache the light properties.
                pointLightsCache = new CachedLightData[pointLights.Length];

                // assign the dynamic lights in the scene to the dynamic light manager.
                dynamicLightManager.raycastedDynamicLights.Clear();
                for (int i = 0; i < pointLights.Length; i++)
                {
                    var light = pointLights[i];
                    dynamicLightManager.raycastedDynamicLights.Add(new RaycastedDynamicLight(light));
                    pointLightsCache[i] = new CachedLightData(light);
                }

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
                        if (progressBarCancel) { cancelled?.Invoke(this, null); break; }
#endif
                    }
                }

#if UNITY_EDITOR
                // have unity editor reload the modified assets.
                UnityEditor.AssetDatabase.Refresh();
#endif
                Debug.Log("Raytracing Finished: " + traces + " traces in " + tracingTime + "! Seams padding in " + seamTime + "! Runtime optimizations in " + optimizationTime + " (removed " + optimizationLightsRemoved + " light sources from triangles)! VRAM estimation: " + MathEx.BytesToUnitString(vramTotal));
                dynamicLightManager.Reload();
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
                // always make sure we reset these settings.
                Physics.queriesHitBackfaces = queriesHitBackfacesPrevious;
                Physics.queriesHitTriggers = queriesHitTriggersPrevious;
#if UNITY_EDITOR
                UnityEditor.EditorUtility.ClearProgressBar();
#endif
                raycastProcessor.Dispose();
            }
        }

        private unsafe void Raytrace(MeshFilter meshFilter, float progressMin, float progressMax)
        {
            var meshBuilder = new MeshBuilder(meshFilter.transform.localToWorldMatrix, meshFilter.sharedMesh);
            lightmapSize = MathEx.SurfaceAreaToTextureSize(meshBuilder.surfaceArea, pixelDensityPerSquareMeter);
            if (lightmapSize > maximumLightmapSize)
                lightmapSize = maximumLightmapSize;
            lightmapSizeMin1 = lightmapSize - 1;

#if UNITY_EDITOR
            var progressTitle = "Raytracing Scene " + meshBuilder.surfaceArea.ToString("0.00") + "m² (" + lightmapSize + "x" + lightmapSize + ")";
            var progressDescription = "Raytracing " + meshFilter.name;
#endif
            if (!meshBuilder.hasLightmapCoordinates)
            {
                Debug.LogWarning("Raytracer skipping " + meshFilter.name + " because it does not have uv1 lightmap coordinates!");
                return;
            }
            else
            {
                // estimate the amount of vram required (purely statistical).
                ulong vramLightmap = (ulong)(lightmapSize * lightmapSize * 4); // uint32
                vramTotal += vramLightmap;

                Debug.Log(meshFilter.name + " surface area: " + meshBuilder.surfaceArea.ToString("0.00") + "m² lightmap size: " + lightmapSize + "x" + lightmapSize + " VRAM: " + MathEx.BytesToUnitString(vramLightmap), meshFilter);
            }

            tracingTime.Begin();
            var dynamic_triangles = new DynamicTrianglesBuilder(meshBuilder.triangleCount);
            var pixels_lightmap = new uint[lightmapSize * lightmapSize];
            var pixels_visited = new uint[lightmapSize * lightmapSize];
            var pixels_lightmap_gc = GCHandle.Alloc(pixels_lightmap, GCHandleType.Pinned);
            var pixels_visited_gc = GCHandle.Alloc(pixels_visited, GCHandleType.Pinned);
            var pixels_lightmap_ptr = (uint*)pixels_lightmap_gc.AddrOfPinnedObject();
            var pixels_visited_ptr = (uint*)pixels_visited_gc.AddrOfPinnedObject();

            // prepare to raycast the entire mesh using multi-threading.
            raycastProcessor.pixelsLightmap = pixels_lightmap_ptr;
            raycastProcessor.lightmapSize = lightmapSize;

            // iterate over all triangles in the mesh.
            for (int i = 0; i < meshBuilder.triangleCount; i++)
            {
#if UNITY_EDITOR
                if (Time.realtimeSinceStartup - progressBarLastUpdate > 0.25f)
                {
                    progressBarLastUpdate = Time.realtimeSinceStartup;
                    if (UnityEditor.EditorUtility.DisplayCancelableProgressBar(progressTitle, progressDescription, Mathf.Lerp(progressMin, progressMax, i / (float)meshBuilder.triangleCount)))
                    {
                        progressBarCancel = true;
                        break;
                    }
                }
#endif
                var (v1, v2, v3) = meshBuilder.GetTriangleVertices(i);
                var (t1, t2, t3) = meshBuilder.GetTriangleUv1(i);

                RaycastTriangle(i, dynamic_triangles, pixels_visited_ptr, v1, v2, v3, t1, t2, t3);
            }

            // finish any remaining raycasting work.
            raycastProcessor.Complete();
            tracingTime.Stop();

            // optimize the runtime performance.
            // iterate over all triangles in the mesh.
            optimizationTime.Begin();
            for (int i = 0; i < meshBuilder.triangleCount; i++)
            {
                var (t1, t2, t3) = meshBuilder.GetTriangleUv1(i);

                OptimizeTriangle(i, pixels_lightmap_ptr, dynamic_triangles, t1, t2, t3);
            }
            optimizationTime.Stop();

            seamTime.Begin();
            {
                for (int y = 0; y < lightmapSize; y++)
                {
                    int yPtr = y * lightmapSize;

                    for (int x = 0; x < lightmapSize; x++)
                    {
                        int xyPtr = yPtr + x;

                        // if we find an unvisited pixel it will appear as a black seam in the scene.
                        uint visited = pixels_visited_ptr[xyPtr];
                        if (visited == 0)
                        {
                            uint res = 0;

                            // fetch 5x5 "visited" pixels (where p22 is the center).

                            // bool p00 = GetPixel(ref pixels_visited, x - 2, y - 2) == 1;
                            // bool p10 = GetPixel(ref pixels_visited, x - 1, y - 2) == 1;
                            bool p20 = GetPixel(pixels_visited_ptr, x, y - 2) == 1;
                            // bool p30 = GetPixel(ref pixels_visited, x + 1, y - 2) == 1;
                            // bool p40 = GetPixel(ref pixels_visited, x + 2, y - 2) == 1;

                            // bool p01 = GetPixel(ref pixels_visited, x - 2, y - 1) == 1;
                            // bool p11 = GetPixel(ref pixels_visited, x - 1, y - 1) == 1;
                            bool p21 = GetPixel(pixels_visited_ptr, x, y - 1) == 1;
                            // bool p31 = GetPixel(ref pixels_visited, x + 1, y - 1) == 1;
                            // bool p41 = GetPixel(ref pixels_visited, x + 2, y - 1) == 1;

                            bool p02 = GetPixel(pixels_visited_ptr, x - 2, y) == 1;
                            bool p12 = GetPixel(pixels_visited_ptr, x - 1, y) == 1;
                            bool p32 = GetPixel(pixels_visited_ptr, x + 1, y) == 1;
                            bool p42 = GetPixel(pixels_visited_ptr, x + 2, y) == 1;

                            // bool p03 = GetPixel(ref pixels_visited, x - 2, y + 1) == 1;
                            // bool p13 = GetPixel(ref pixels_visited, x - 1, y + 1) == 1;
                            bool p23 = GetPixel(pixels_visited_ptr, x, y + 1) == 1;
                            // bool p33 = GetPixel(ref pixels_visited, x + 1, y + 1) == 1;
                            // bool p43 = GetPixel(ref pixels_visited, x + 2, y + 1) == 1;

                            // bool p04 = GetPixel(ref pixels_visited, x - 2, y + 2) == 1;
                            // bool p14 = GetPixel(ref pixels_visited, x - 1, y + 2) == 1;
                            bool p24 = GetPixel(pixels_visited_ptr, x, y + 2) == 1;
                            // bool p34 = GetPixel(ref pixels_visited, x + 1, y + 2) == 1;
                            // bool p44 = GetPixel(ref pixels_visited, x + 2, y + 2) == 1;

                            // fetch 5x5 "lightmap" pixels (where p22 is the center).

                            // uint l00 = GetPixel(ref pixels_lightmap, x - 2, y - 2);
                            // uint l10 = GetPixel(ref pixels_lightmap, x - 1, y - 2);
                            uint l20 = GetPixel(pixels_lightmap_ptr, x, y - 2);
                            // uint l30 = GetPixel(ref pixels_lightmap, x + 1, y - 2);
                            // uint l40 = GetPixel(ref pixels_lightmap, x + 2, y - 2);

                            // uint l01 = GetPixel(ref pixels_lightmap, x - 2, y - 1);
                            // uint l11 = GetPixel(ref pixels_lightmap, x - 1, y - 1);
                            uint l21 = GetPixel(pixels_lightmap_ptr, x, y - 1);
                            // uint l31 = GetPixel(ref pixels_lightmap, x + 1, y - 1);
                            // uint l41 = GetPixel(ref pixels_lightmap, x + 2, y - 1);

                            uint l02 = GetPixel(pixels_lightmap_ptr, x - 2, y);
                            uint l12 = GetPixel(pixels_lightmap_ptr, x - 1, y);
                            uint l32 = GetPixel(pixels_lightmap_ptr, x + 1, y);
                            uint l42 = GetPixel(pixels_lightmap_ptr, x + 2, y);

                            // uint l03 = GetPixel(ref pixels_lightmap, x - 2, y + 1);
                            // uint l13 = GetPixel(ref pixels_lightmap, x - 1, y + 1);
                            uint l23 = GetPixel(pixels_lightmap_ptr, x, y + 1);
                            // uint l33 = GetPixel(ref pixels_lightmap, x + 1, y + 1);
                            // uint l43 = GetPixel(ref pixels_lightmap, x + 2, y + 1);

                            // uint l04 = GetPixel(ref pixels_lightmap, x - 2, y + 2);
                            // uint l14 = GetPixel(ref pixels_lightmap, x - 1, y + 2);
                            uint l24 = GetPixel(pixels_lightmap_ptr, x, y + 2);
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

                            pixels_lightmap_ptr[xyPtr] = res;
                        }
                    }
                }
            }
            seamTime.Stop();

            pixels_lightmap_gc.Free();
            pixels_visited_gc.Free();

            // store the scene reference renderer in the dynamic light manager with lightmap metadata.
            var lightmap = new RaycastedMeshRenderer();
            lightmap.renderer = meshFilter.GetComponent<MeshRenderer>();
            lightmap.resolution = lightmapSize;
            lightmap.identifier = uniqueIdentifier++;
            dynamicLightManager.raycastedMeshRenderers.Add(lightmap);

            // write the lightmap shadow bits to disk.
            if (!Utilities.WriteLightmapData(lightmap.identifier, "Lightmap", pixels_lightmap))
                Debug.LogError($"Unable to write the lightmap {lightmap.identifier} file in the active scene resources directory!");

            // write the dynamic triangles to disk.
            if (!Utilities.WriteLightmapData(lightmap.identifier, "Triangles", dynamic_triangles.BuildDynamicTrianglesData().ToArray()))
                Debug.LogError($"Unable to write the triangles {lightmap.identifier} file in the active scene resources directory!");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe uint GetPixel(uint* pixels, int x, int y)
        {
            int offset = y * lightmapSize + x;
            if ((uint)offset >= (uint)(lightmapSize * lightmapSize)) return 0;
            return pixels[offset];
        }

        private unsafe void RaycastTriangle(int triangle_index, DynamicTrianglesBuilder dynamic_triangles, uint* pixels_visited, Vector3 v1, Vector3 v2, Vector3 v3, Vector2 t1, Vector2 t2, Vector2 t3)
        {
            // calculate the triangle normal (this may fail when degenerate or very small).
            var trianglePlane = new Plane(v1, v2, v3);
            var triangleNormal = trianglePlane.normal;
            var triangleCenter = (v1 + v2 + v3) / 3.0f;
            var triangleNormalValid = !triangleNormal.Equals(Vector3.zero);
            var triangleBounds = MathEx.GetTriangleBounds(v1, v2, v3);

            // first we associate lights to triangles that can potentially be affected by them. the
            // uv space may skip triangles when there's no direct point on the triangle and it may
            // also have a point outside the range of the light leaving jagged edges, thus we only
            // go by triangle normal and light radius to determine whether the light can be affected.
            for (int i = 0; i < pointLights.Length; i++)
            {
                var light = pointLights[i];
                var lightPosition = pointLightsCache[i].position;

                // if we have the triangle normal then exclude triangles facing away from the light.
                if (triangleNormalValid)
                    if (math.dot(triangleNormal, (lightPosition - triangleCenter).normalized) <= -0.1f)
                        continue;

                // cheap test using bounding boxes whether the light intersects the triangle.
                var lightBounds = pointLightsCache[i].bounds;
                if (!lightBounds.Intersects(triangleBounds))
                    continue;

                // ensure the triangle intersects with the light sphere.
                if (!MathEx.CheckSphereIntersectsTriangle(lightPosition, light.lightRadius, v1, v2, v3))
                    continue;

                // this light can affect the triangle.
                dynamic_triangles.AssociateLightWithTriangleFast(triangle_index, i);
            }

            // skip degenerate triangles.
            if (!triangleNormalValid) return;

            // do some initial uv to 3d work here and also determine whether we can early out.
            if (!MathEx.UvTo3dFastPrerequisite(t1, t2, t3, out float triangleSurfaceArea))
                return;

            // calculate the bounding box of the polygon in UV space.
            // we only have to raycast these pixels and can skip the rest.
            var triangleBoundingBox = MathEx.ComputeTriangleBoundingBox(t1, t2, t3);
            var minX = Mathf.FloorToInt(triangleBoundingBox.xMin * lightmapSizeMin1);
            var minY = Mathf.FloorToInt(triangleBoundingBox.yMin * lightmapSizeMin1);
            var maxX = Mathf.CeilToInt(triangleBoundingBox.xMax * lightmapSizeMin1);
            var maxY = Mathf.CeilToInt(triangleBoundingBox.yMax * lightmapSizeMin1);

            // clamp the pixel coordinates so that we can safely write to our arrays.
            minX = Mathf.Clamp(minX, 0, (int)lightmapSizeMin1);
            minY = Mathf.Clamp(minY, 0, (int)lightmapSizeMin1);
            maxX = Mathf.Clamp(maxX, 0, (int)lightmapSizeMin1);
            maxY = Mathf.Clamp(maxY, 0, (int)lightmapSizeMin1);

            // prepare to only iterate over lights potentially affecting the current triangle.
            var triangleLightIndices = dynamic_triangles.GetAssociatedLightIndices(triangle_index);
            var triangleLightIndicesCount = triangleLightIndices.Count;

            // calculate some values in advance.
            var triangleNormalOffset = triangleNormal * 0.001f;

            for (int y = minY; y < maxY; y++)
            {
                float yy = y / lightmapSizeMin1;

                for (int x = minX; x < maxX; x++)
                {
                    float xx = x / lightmapSizeMin1;

                    var world = MathEx.UvTo3dFast(triangleSurfaceArea, new Vector2(xx, yy), v1, v2, v3, t1, t2, t3);
                    if (world.Equals(Vector3.zero)) continue;

                    // iterate over the lights potentially affecting this triangle.
                    for (int i = 0; i < triangleLightIndicesCount; i++)
                    {
                        var pointLight = pointLights[triangleLightIndices[i]];
                        var pointLightCache = pointLightsCache[triangleLightIndices[i]];
                        var lightPosition = pointLightCache.position;
                        var lightRadius = pointLight.lightRadius;
                        var lightDistanceToWorld = Vector3.Distance(lightPosition, world);

                        // early out by distance.
                        if (lightDistanceToWorld > lightRadius)
                            continue;

                        // early out by normal.
                        var lightDirection = (lightPosition - world).normalized;
                        if (math.dot(triangleNormal, lightDirection) < -0.1f)
                            continue;

                        // prepare to trace from the world to the light position.
                        traces++;
                        raycastProcessor.Add(
                            new RaycastCommand(world + triangleNormalOffset, lightDirection, lightDistanceToWorld, raycastLayermask),
                            new RaycastCommandMeta(x, y, world, pointLight.lightChannel)
                        );
                    }

                    // write this pixel into the visited map.
                    pixels_visited[y * lightmapSize + x] = 1;
                }
            }
        }

        private unsafe void OptimizeTriangle(int triangle_index, uint* pixels_lightmap, DynamicTrianglesBuilder dynamic_triangles, Vector2 t1, Vector2 t2, Vector2 t3)
        {
            // during the raycasting process, lights were associated per-triangle. This was
            // determined by the normal of the triangle (must face the light) and that the radius of
            // the light intersects the triangle, however, fully occluded walls may still have the
            // light associated with this logic alone. we use the raycasting results to determine
            // which triangles are truly affected by which lights and remove the lights that are not
            // affecting it. depending on the scene, this removes hundreds of thousands of lights
            // doubling the framerate.

            // calculate the bounding box of the polygon in UV space.
            var triangleBoundingBox = MathEx.ComputeTriangleBoundingBox(t1, t2, t3);

            // triangles may be so thin and small that they do not have their own UV texels, so we
            // expand the bounding box by one pixel on the shadow occlusion map to include the
            // neighbouring texels, as failure to do so will leave them without all of their light
            // sources (i.e. fully black).
            var minX = Mathf.FloorToInt(triangleBoundingBox.xMin * lightmapSizeMin1) - 1;
            var minY = Mathf.FloorToInt(triangleBoundingBox.yMin * lightmapSizeMin1) - 1;
            var maxX = Mathf.CeilToInt(triangleBoundingBox.xMax * lightmapSizeMin1) + 1;
            var maxY = Mathf.CeilToInt(triangleBoundingBox.yMax * lightmapSizeMin1) + 1;

            // clamp the pixel coordinates so that we can safely read from our arrays.
            minX = Mathf.Clamp(minX, 0, (int)lightmapSizeMin1);
            minY = Mathf.Clamp(minY, 0, (int)lightmapSizeMin1);
            maxX = Mathf.Clamp(maxX, 0, (int)lightmapSizeMin1);
            maxY = Mathf.Clamp(maxY, 0, (int)lightmapSizeMin1);

            // prepare to iterate over lights associated with the current triangle.
            var triangleLightIndices = dynamic_triangles.GetAssociatedLightIndices(triangle_index);
            var triangleLightIndicesCount = triangleLightIndices.Count;

            // only iterate over lights associated with the current triangle.
            for (int i = triangleLightIndicesCount; i-- > 0;)
            {
                var pointLight = pointLights[triangleLightIndices[i]];
                var lightChannelBit = (uint)1 << ((int)pointLight.lightChannel);
                var lightFound = false;

                for (int y = minY; y < maxY; y++)
                {
                    int yPtr = y * lightmapSize;

                    for (int x = minX; x < maxX; x++)
                    {
                        int xyPtr = yPtr + x;

                        if ((pixels_lightmap[xyPtr] & lightChannelBit) > 0)
                        {
                            lightFound = true;
                            break;
                        }
                    }

                    if (lightFound)
                        break;
                }

                if (!lightFound)
                {
                    optimizationLightsRemoved++;
                    triangleLightIndices.RemoveAt(i);
                }
            }
        }
    }
}