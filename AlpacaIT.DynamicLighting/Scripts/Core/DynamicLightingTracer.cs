using AlpacaIT.DynamicLighting.Internal;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
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
        private bool bounceLightingInScene = false;
        private BenchmarkTimer totalTime;
        private BenchmarkTimer tracingTime;
        private BenchmarkTimer seamTime;
        private BenchmarkTimer optimizationTime;
        private BenchmarkTimer bvhTime;
        private ulong vramLegacyTotal = 0;
        private ulong vramDynamicTrianglesTotal = 0;
        private ulong vramBvhTotal = 0;
        private DynamicLight[] pointLights;
        private CachedLightData[] pointLightsCache;
        private ShadowRaycastProcessor raycastProcessor;
        private RaycastProcessor callbackRaycastProcessor;
        private RaycastHandlerPool<BounceTriangleRaycastMissHandler> bounceRaycastHandlerPool;
        private int lightmapSize = 2048;
        private float lightmapSizeMin1;
        private LayerMask raycastLayermask = ~0;
        private int pixelDensityPerSquareMeter = 128;
        private DynamicLightManager dynamicLightManager;
        private StringBuilder log;
        private static RaycastCommand raycastCommand;

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
            raycastProcessor = new ShadowRaycastProcessor();
            callbackRaycastProcessor = new RaycastProcessor();
            bounceRaycastHandlerPool = new RaycastHandlerPool<BounceTriangleRaycastMissHandler>(256 * 256);

            // -> partial class DynamicLightingTracer.TemporaryScene initialize.
            TemporarySceneInitialize();

            // -> partial class DynamicLightingTracer.PhotonCamera initialize.
            PhotonCameraInitialize();

            log = new StringBuilder();
            log.AppendLine("CLICK FOR SCENE STATISTICS");
            log.AppendLine("--------------------------------");

            traces = 0;
            optimizationLightsRemoved = 0;
            bounceLightingInScene = false;
            totalTime = new BenchmarkTimer();
            tracingTime = new BenchmarkTimer();
            seamTime = new BenchmarkTimer();
            optimizationTime = new BenchmarkTimer();
            bvhTime = new BenchmarkTimer();
            vramLegacyTotal = 0;
            vramDynamicTrianglesTotal = 0;
            vramBvhTotal = 0;
            lightmapSizeMin1 = lightmapSize - 1;
            raycastLayermask = dynamicLightManager.raytraceLayers;
            pixelDensityPerSquareMeter = dynamicLightManager.pixelDensityPerSquareMeter;

            // prepare a raycast command that is recycled for raytracing.
#if UNITY_2021_2_OR_NEWER && !UNITY_2021_2_17 && !UNITY_2021_2_16 && !UNITY_2021_2_15 && !UNITY_2021_2_14 && !UNITY_2021_2_13 && !UNITY_2021_2_12 && !UNITY_2021_2_11 && !UNITY_2021_2_10 && !UNITY_2021_2_9 && !UNITY_2021_2_8 && !UNITY_2021_2_7 && !UNITY_2021_2_6 && !UNITY_2021_2_5 && !UNITY_2021_2_4 && !UNITY_2021_2_3 && !UNITY_2021_2_2 && !UNITY_2021_2_1 && !UNITY_2021_2_0
#if UNITY_EDITOR
            raycastCommand.physicsScene = temporaryScenePhysics;
#else
            raycastCommand.physicsScene = Physics.defaultPhysicsScene;
#endif
#else
            Debug.LogWarning("Dynamic Lighting only officially supports Unity Editor 2021.2.18f1 and beyond. Please try to upgrade your project for the best experience.");
#endif
#if UNITY_2022_2_OR_NEWER
            raycastCommand.queryParameters = new QueryParameters(raycastLayermask, false, QueryTriggerInteraction.Ignore, true);
#else
            raycastCommand.layerMask = raycastLayermask;
            raycastCommand.maxHits = 1;
#endif
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
            bvhTime.Begin();

            // find all of the dynamic lights in the scene.
            var dynamicLights = DynamicLightManager.FindDynamicLightsInScene().ToArray();

            // there must be at least one light in order to create the bounding volume hierarchy.
            if (dynamicLights.Length > 0)
            {
                // create the dynamic lights bounding volume hierarchy and store it.
                var bvhDynamicLights = new BvhLightStructure(dynamicLights);
                var bvhDynamicLights32 = bvhDynamicLights.ToUInt32Array();
                vramBvhTotal += (ulong)bvhDynamicLights32.Length * 4;
                if (!dynamicLightManager.raycastedScene.dynamicLightsBvh.Write(bvhDynamicLights32))
                    Debug.LogError($"Unable to compress and store the dynamic lights bounding volume hierarchy!");

                // create the point lights array with the order the bvh tree desires.
                pointLights = new DynamicLight[dynamicLights.Length];
                for (int i = 0; i < dynamicLights.Length; i++)
                    pointLights[i] = dynamicLights[bvhDynamicLights.dynamicLightsIdx[i]]; // pigeonhole sort!
            }
            else
            {
                pointLights = dynamicLights;
            }

            bvhTime.Stop();
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
                // store everything inside of a scripted object.
                dynamicLightManager.raycastedScene = ScriptableObject.CreateInstance<RaycastedScene>();

                // from here we begin measuring the total time spent.
                totalTime.Begin();

                // try to remember the version used.
                dynamicLightManager.version = 3;
                dynamicLightManager.activateBounceLightingInCurrentScene = false;

                // find all light sources and calculate the bounding volume hierarchy.
                CreateDynamicLightsBvh();

                // assign channels to all dynamic lights in the scene.
                ChannelsUpdatePointLightsInScene();

                // cache the light properties.
                pointLightsCache = new CachedLightData[pointLights.Length];

                // iterate over all compatible mesh filters and build a temporary scene with them.
                var meshFilters = Object.FindObjectsOfType<MeshFilter>();
                var goodFilters = new bool[meshFilters.Length];
                for (int i = 0; i < meshFilters.Length; i++)
                    goodFilters[i] = TemporarySceneAdd(meshFilters[i]);

                // assign the dynamic lights in the scene to the dynamic light manager.
                dynamicLightManager.raycastedDynamicLights.Clear();
                for (int i = 0; i < pointLights.Length; i++)
                {
                    var light = pointLights[i];
                    dynamicLightManager.raycastedDynamicLights.Add(new RaycastedDynamicLight(light));
                    pointLightsCache[i] = new CachedLightData(light);

                    bool requiresPhotonCube = false;
                    bool requiresDistanceOnly = true;

                    // computing transparency in raycasted shadows requires a photon cube.
                    if (light.lightTransparency == DynamicLightTransparencyMode.Enabled)
                        requiresPhotonCube = true;

                    // check for the usage of bounce lighting in the scene.
                    if (light.lightIllumination == DynamicLightIlluminationMode.SingleBounce)
                    {
                        // bounce lighting requires a photon cube.
                        requiresPhotonCube = true;
                        requiresDistanceOnly = false;

                        // remember whether bounce lighting is used in the scene, this allows us to
                        // skip steps and checks later on.
                        bounceLightingInScene = true;

                        // also save this flag into the scene.
                        dynamicLightManager.activateBounceLightingInCurrentScene = true;
                    }

                    // render and create photon cubes for all lights that require it.
                    if (requiresPhotonCube)
                        pointLightsCache[i].photonCube = PhotonCameraRender(pointLightsCache[i].position, light.lightRadius, requiresDistanceOnly);
                }

                // iterate over all compatible mesh filters and raytrace their lighting.
                for (int i = 0; i < meshFilters.Length; i++)
                {
                    if (!goodFilters[i]) continue;

                    var meshFilter = meshFilters[i];
                    float progressMin = i / (float)meshFilters.Length;
                    float progressMax = (i + 1) / (float)meshFilters.Length;

                    Raytrace(meshFilter, progressMin, progressMax);
#if UNITY_EDITOR
                    if (progressBarCancel) { cancelled?.Invoke(this, null); break; }
#endif
                }

                // stop measuring the total time here as the asset database refresh is slow.
                totalTime.Stop();

                // write the raycasted scene data to disk.
                Utilities.WriteRaycastedScene(dynamicLightManager.raycastedScene);

#if UNITY_EDITOR
                // have unity editor reload the modified assets.
                UnityEditor.AssetDatabase.Refresh();
#endif
                log.AppendLine("--------------------------------");
                log.Append("Raycasts: ").Append(traces).Append(" (").Append(tracingTime.ToString()).AppendLine(")");
                log.Append("Bounding Volume Hierarchy: ").AppendLine(bvhTime.ToString());
                log.Append("Occlusion Bits Seams Padding: ").AppendLine(seamTime.ToString());
                log.Append("Dynamic Triangles Optimization: ").Append(optimizationLightsRemoved).Append(" Light Sources Removed (").Append(optimizationTime.ToString()).AppendLine(")");
                log.AppendLine("--------------------------------");
                log.Append("VRAM Dynamic Triangles: ").Append(MathEx.BytesToUnitString(vramDynamicTrianglesTotal)).Append(" (Legacy: ").Append(MathEx.BytesToUnitString(vramLegacyTotal)).AppendLine(")");
                log.Append("VRAM Bounding Volume Hierarchy: ").AppendLine(MathEx.BytesToUnitString(vramBvhTotal));
                log.Insert(0, $"The lighting requires {MathEx.BytesToUnitString(vramDynamicTrianglesTotal + vramBvhTotal)} VRAM on the graphics card to render the current scene ({totalTime}).{System.Environment.NewLine}");

                Debug.Log(log.ToString());
                dynamicLightManager.Reload();
#if UNITY_EDITOR
                UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
#endif
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
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
                callbackRaycastProcessor.Dispose();

                // -> partial class DynamicLightManager.PhotonCamera cleanup.
                PhotonCameraCleanup();

                // -> partial class DynamicLightManager.TemporaryScene cleanup.
                TemporarySceneCleanup();
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
                vramLegacyTotal += vramLightmap;

                log.AppendLine(meshFilter.name + " surface area: " + meshBuilder.surfaceArea.ToString("0.00") + "m² lightmap size: " + lightmapSize + "x" + lightmapSize);
            }

            tracingTime.Begin();
            var dynamic_triangles = new DynamicTrianglesBuilder(meshBuilder, lightmapSize);
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

                RaycastTriangle(i, dynamic_triangles, pixels_visited_ptr, pixels_lightmap_ptr, v1, v2, v3, t1, t2, t3);
            }

            // finish any remaining raycasting work.
            raycastProcessor.Complete();
            tracingTime.Stop();

            // only executed when bounce lighting is detected in the scene.
            if (bounceLightingInScene)
            {
                // iterate over all lights in the scene.
                for (int j = 0; j < pointLights.Length; j++)
                {
                    var pointLight = pointLights[j];
                    var pointLightCache = pointLightsCache[j];

                    // the light must have bounce lighting enabled.
                    if (pointLight.lightIllumination != DynamicLightIlluminationMode.SingleBounce)
                        continue;

                    // cheap test using bounding boxes whether the light intersects the mesh.
                    var pointLightBounds = pointLightCache.bounds;
                    if (!pointLightBounds.Intersects(meshBuilder.worldBounds))
                        continue;

                    // create a bounce lighting texture.
                    var pixels_bounce = new Color[lightmapSize * lightmapSize];
                    var pixels_bounce_gc = GCHandle.Alloc(pixels_bounce, GCHandleType.Pinned);
                    var pixels_bounce_ptr = (Color*)pixels_bounce_gc.AddrOfPinnedObject();

                    // iterate over all triangles in the mesh.
                    for (int i = 0; i < meshBuilder.triangleCount; i++)
                    {
                        var (v1, v2, v3) = meshBuilder.GetTriangleVertices(i);
                        var (t1, t2, t3) = meshBuilder.GetTriangleUv1(i);

                        BounceTriangle(j, i, dynamic_triangles, pixels_bounce_ptr, v1, v2, v3, t1, t2, t3);
                    }

                    // finish any remaining raycasting work.
                    callbackRaycastProcessor.Complete();

                    DilateBounceTexture(pixels_bounce_ptr, pixels_bounce);
                    GaussianBlur.ApplyGaussianBlur(pixels_bounce_ptr, pixels_bounce, lightmapSize, 7, 5);

                    // iterate over all triangles in the mesh.
                    for (int i = 0; i < meshBuilder.triangleCount; i++)
                    {
                        var (t1, t2, t3) = meshBuilder.GetTriangleUv1(i);

                        BuildBounceTextures(j, i, pixels_bounce_ptr, dynamic_triangles, t1, t2, t3);
                    }

                    // free the bounce lighting texture.
                    pixels_bounce_gc.Free();
                }
            }

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

            // iterate over all triangles in the mesh.
            for (int i = 0; i < meshBuilder.triangleCount; i++)
            {
                var (t1, t2, t3) = meshBuilder.GetTriangleUv1(i);

                BuildShadows(i, pixels_lightmap_ptr, dynamic_triangles, t1, t2, t3);
            }

            pixels_lightmap_gc.Free();
            pixels_visited_gc.Free();

            // compress the dynamic triangles data and store it inside of the raycasted scene.
            var dynamic_triangles32 = dynamic_triangles.BuildDynamicTrianglesData(bounceLightingInScene).ToArray();
            vramDynamicTrianglesTotal += (ulong)dynamic_triangles32.Length * 4;

            // store the scene reference renderer in the dynamic light manager with lightmap metadata.
            var lightmap = new RaycastedMeshRenderer();
            lightmap.renderer = meshFilter.GetComponent<MeshRenderer>();
            lightmap.resolution = lightmapSize;
            lightmap.identifier = dynamicLightManager.raycastedScene.StoreDynamicTriangles(dynamic_triangles32);
            if (lightmap.identifier == -1)
                Debug.LogError($"Unable to compress the dynamic lighting data for '" + meshFilter.name + "'!");
            else
                dynamicLightManager.raycastedMeshRenderers.Add(lightmap);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe uint GetPixel(uint* pixels, int x, int y)
        {
            int offset = y * lightmapSize + x;
            if ((uint)offset >= (uint)(lightmapSize * lightmapSize)) return 0;
            return pixels[offset];
        }

        private unsafe void RaycastTriangle(int triangle_index, DynamicTrianglesBuilder dynamic_triangles, uint* pixels_visited, uint* pixels_lightmap, Vector3 v1, Vector3 v2, Vector3 v3, Vector2 t1, Vector2 t2, Vector2 t3)
        {
            // calculate the triangle normal (this may fail when degenerate or very small).
            var triangleNormal3 = math.normalizesafe(math.cross(v2 - v1, v3 - v1));
            var triangleNormalPtr = (Vector3*)&triangleNormal3;
            var triangleNormal = *triangleNormalPtr;

            // [unsafe] (v1 + v2 + v3) / 3.0f;
            var triangleCenter = v1;
            var triangleCenterPtr = &triangleCenter;
            UMath.Add(triangleCenterPtr, &v2);
            UMath.Add(triangleCenterPtr, &v3);
            UMath.Scale(triangleCenterPtr, 1.0f / 3.0f);

            var triangleNormalValid = UMath.IsNonZero(triangleNormalPtr);
            var triangleBounds = MathEx.GetTriangleBounds(v1, v2, v3);

            // optimize for the IL by preparing memory and pointers outside of the loop.
            Vector3 lightDirection;
            var lightDirectionPtr = &lightDirection;

            // first we associate lights to triangles that can potentially be affected by them. the
            // uv space may skip triangles when there's no direct point on the triangle and it may
            // also have a point outside the range of the light leaving jagged edges, thus we only
            // go by triangle normal and light radius to determine whether the light can be affected.
            for (int i = 0; i < pointLights.Length; i++)
            {
                var light = pointLights[i];
                var lightPosition = pointLightsCache[i].position;

                // if we have the triangle normal then exclude triangles facing away from the light.
                // indirect light can bounce onto a triangle with any normal, so the following
                // optimization only works when light bouncing is disabled for this light source.
                if (triangleNormalValid && light.lightIllumination == DynamicLightIlluminationMode.DirectIllumination)
                {
                    // [unsafe] lightDirection = (lightPosition - triangleCenter).normalized
                    lightDirection = lightPosition;
                    UMath.Subtract(lightDirectionPtr, triangleCenterPtr);
                    UMath.Normalize(lightDirectionPtr);
                    if (UMath.Dot(triangleNormalPtr, lightDirectionPtr) <= -0.1f)
                        continue;
                }

                // cheap test using bounding boxes whether the light intersects the triangle.
                var lightBounds = pointLightsCache[i].bounds;
                if (!lightBounds.Intersects(triangleBounds))
                    continue;

                // ensure the triangle intersects with the light sphere.
                if (!MathEx.CheckSphereIntersectsTriangle(lightPosition, light.lightRadius, v1, v2, v3))
                    continue;

                // this light can affect the triangle.
                dynamic_triangles.AddRaycastedLightToTriangle(triangle_index, i);
            }

            // skip degenerate triangles.
            if (!triangleNormalValid) return;

            // do some initial uv to 3d work here and also determine whether we can early out.
            if (!MathEx.UvTo3dFastPrerequisite(t1, t2, t3, out float triangleSurfaceArea))
                return;

            // calculate the bounding box of the polygon in UV space.
            // we only have to raycast these pixels and can skip the rest.
            var triangleBoundingBox = new PixelTriangleRect(lightmapSize, MathEx.ComputeTriangleBoundingBox(t1, t2, t3));
            var minX = triangleBoundingBox.xMin;
            var minY = triangleBoundingBox.yMin;
            var maxX = triangleBoundingBox.xMax;
            var maxY = triangleBoundingBox.yMax;

            // calculate the world position for every UV position on a triangle.
            var triangleUvTo3dStep = new TriangleUvTo3dStep(v1, v2, v3, t1, t2, t3, triangleSurfaceArea, triangleBoundingBox, lightmapSize);
            triangleUvTo3dStep.Execute();
            var uvWorldPositions = triangleUvTo3dStep.worldPositionsPtr;

            // prepare to only iterate over lights potentially affecting the current triangle.
            var triangleLightIndices = dynamic_triangles.GetRaycastedLightIndices(triangle_index);
            var triangleLightIndicesCount = triangleLightIndices.Count;

            // calculate some values in advance.
            // [unsafe] triangleNormal * 0.001f
            var triangleNormalOffset = triangleNormal;
            var triangleNormalOffsetPtr = &triangleNormalOffset;
            UMath.Scale(triangleNormalOffsetPtr, 0.001f);

            // optimize for the IL by preparing memory and pointers outside of the loop.
            Vector3 world;
            var worldPtr = &world;
            var localTracesCounter = 0;

            int ptr = 0;
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    // fetch the world position for the current uv coordinate.
                    world = uvWorldPositions[ptr++];
                    if (UMath.IsZero(worldPtr)) continue;

                    // [unsafe] world + triangleNormalOffset
                    var worldPlusTriangleNormalOffset = world;
                    UMath.Add(&worldPlusTriangleNormalOffset, triangleNormalOffsetPtr);

                    // iterate over the lights potentially affecting this triangle.
                    for (int i = 0; i < triangleLightIndicesCount; i++)
                    {
                        var lightIndex = triangleLightIndices[i];
                        var pointLight = pointLights[lightIndex];
                        var pointLightCache = pointLightsCache[lightIndex];
                        var lightPosition = pointLightCache.position;
                        var lightRadius = pointLight.lightRadius;
                        var lightDistanceToWorld = Vector3.Distance(lightPosition, world);

                        // early out by distance.
                        if (lightDistanceToWorld > lightRadius)
                            continue;

                        // early out by normal.
                        // [unsafe] lightDirection = (lightPosition - world).normalized
                        lightDirection = lightPosition;
                        UMath.Subtract(lightDirectionPtr, worldPtr);
                        UMath.Normalize(lightDirectionPtr);
                        if (UMath.Dot(triangleNormalPtr, lightDirectionPtr) < -0.1f)
                            continue;

                        // when using alpha transparency we already did all the work on the graphics card.
                        if (pointLight.lightTransparency == DynamicLightTransparencyMode.Enabled)
                        {
                            if (pointLightCache.photonCube.SampleShadow(lightDirection, lightDistanceToWorld, triangleNormal3))
                            {
                                pixels_lightmap[y * lightmapSize + x] |= (uint)1 << ((int)pointLight.lightChannel);
                            }
                        }
                        else
                        {
                            // prepare to trace from the world to the light position.

                            // create a raycast command as fast as possible.
                            raycastCommand.from = worldPlusTriangleNormalOffset;
                            raycastCommand.direction = lightDirection;
                            raycastCommand.distance = lightDistanceToWorld;

                            localTracesCounter++;
                            raycastProcessor.Add(
                                raycastCommand,
                                new RaycastCommandMeta(x, y, world, pointLight.lightChannel)
                            );
                        }
                    }

                    // write this pixel into the visited map.
                    pixels_visited[y * lightmapSize + x] = 1;
                }
            }

            // much faster to add this outside of the loop.
            traces += localTracesCounter;

            triangleUvTo3dStep.Dispose();
        }

        public unsafe float3 AddRandomSpread(float3 direction, float spreadRadius)
        {
            // choose a random direction in 3d space.
            Vector3 randomDirectionV3 = UnityEngine.Random.onUnitSphere;
            var randomDirection = (float3*)&randomDirectionV3; // reinterpret cast.

            // choose a random length for the spread between 0 and the spread radius.
            float randomLength = UnityEngine.Random.Range(0f, spreadRadius);

            // scale the normalized direction vector by the chosen length.
            // spreadVector = randomDirection * randomLength;
            UMath.Scale(randomDirection, randomLength);

            // add the spread vector to the original directional vector.
            // math.normalize(direction + spreadVector);
            UMath.Add(randomDirection, &direction);
            UMath.Normalize(randomDirection);

            return *randomDirection;
        }

        private const int bounceSamples = 32;

        private unsafe class BounceTriangleRaycastMissHandler : RaycastHandler
        {
            private int x;
            private int y;

            /// <summary>The full lightmap size of the current mesh.</summary>
            private int lightmapSize;

            /// <summary>Pointer to the bounce texture pixel data.</summary>
            private Color* pixels_bounce_ptr;

            /// <summary>The accumulated bounce lighting for taking an average.</summary>
            private float3 accumulator;

            public float3[] fresnels = new float3[bounceSamples];

            public void Setup(Color* pixels_bounce_ptr, int lightmapSize, int x, int y)
            {
                // reset:
                accumulator.x = 0f;
                accumulator.y = 0f;
                accumulator.z = 0f;

                // setup:
                this.pixels_bounce_ptr = pixels_bounce_ptr;
                this.lightmapSize = lightmapSize;
                this.x = x;
                this.y = y;
            }

            public override void OnRaycastMiss()
            {
                accumulator += fresnels[raycastsIndex];
            }

            public override unsafe void OnRaycastHit(RaycastHit* hit)
            {
            }

            public override void OnHandlerFinished()
            {
                var average = accumulator / raycastsExpected;
                pixels_bounce_ptr[y * lightmapSize + x] = new Color(average.x, average.y, average.z);
            }
        }

        private unsafe void BounceTriangle(int light_index, int triangle_index, DynamicTrianglesBuilder dynamic_triangles, Color* pixels_bounce_ptr, Vector3 v1, Vector3 v2, Vector3 v3, Vector2 t1, Vector2 t2, Vector2 t3)
        {
            // lights have already been associated with triangles that can potentially be affected
            // by them during the direct illumination step. bounce light sources also include
            // triangles facing away from the light source which is very important as bounce
            // lighting can go anywhere within the light radius.

            // prepare to only process the current light source if it exists on the current triangle.
            if (!dynamic_triangles.TriangleHasRaycastedLight(triangle_index, light_index))
                return;

            // calculate the triangle normal (this may fail when degenerate or very small).
            var triangleNormal = (Vector3)math.normalizesafe(math.cross(v2 - v1, v3 - v1));
            var triangleNormalValid = !triangleNormal.Equals(Vector3.zero);

            // skip degenerate triangles.
            if (!triangleNormalValid) return;

            // do some initial uv to 3d work here and also determine whether we can early out.
            if (!MathEx.UvTo3dFastPrerequisite(t1, t2, t3, out float triangleSurfaceArea))
                return;

            // calculate the bounding box of the polygon in UV space.
            // we only have to raycast these pixels and can skip the rest.
            var triangleBoundingBox = new PixelTriangleRect(lightmapSize, MathEx.ComputeTriangleBoundingBox(t1, t2, t3));
            var minX = triangleBoundingBox.xMin;
            var minY = triangleBoundingBox.yMin;
            var maxX = triangleBoundingBox.xMax;
            var maxY = triangleBoundingBox.yMax;

            // calculate the world position for every UV position on a triangle.
            var triangleUvTo3dStep = new TriangleUvTo3dStep(v1, v2, v3, t1, t2, t3, triangleSurfaceArea, triangleBoundingBox, lightmapSize);
            triangleUvTo3dStep.Execute();
            var uvWorldPositions = triangleUvTo3dStep.worldPositionsPtr;

            // calculate some values in advance.
            var triangleNormalOffset = triangleNormal * 0.001f;

            var pointLight = pointLights[light_index];
            var pointLightCache = pointLightsCache[light_index];
            var photonCube = pointLightCache.photonCube;
            var lightPosition = pointLightCache.position;
            var lightPosition3 = *(float3*)&lightPosition;
            var lightRadius = pointLight.lightRadius;

            // schlick's approximation.
            float materialSpecular = 1.0f;

            int ptr = 0;
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    // fetch the world position for the current uv coordinate.
                    var world = uvWorldPositions[ptr++];
                    if (world.Equals(Vector3.zero)) continue;

                    var lightDistanceToWorld = Vector3.Distance(lightPosition, world);

                    // early out by distance.
                    if (lightDistanceToWorld > lightRadius)
                        continue;

                    var worldWithNormalOffset = world + triangleNormalOffset;
                    var worldWithNormalOffset3 = *(float3*)&worldWithNormalOffset;

                    // calculate the unnormalized direction between the light source and the fragment.
                    float3 light_direction = math.normalize(lightPosition - world);
                    float3 light_direction_negative = -light_direction;

                    var raycastHandler = bounceRaycastHandlerPool.GetInstance();
                    raycastHandler.Setup(pixels_bounce_ptr, lightmapSize, x, y);

                    // take 32 bounce samples around this world position.
                    for (int i = 0; i < bounceSamples; i++)
                    {
                        // sample around 0.3 a lot but eventually take in the wider scene.
                        //var spreadRadius = 0.3f + math.pow(i / (float)(bounceSamples - 1), 2.0f) * 0.7f;
                        //var spreadRadius = 0.3f + math.pow(i / (float)(bounceSamples - 1), 5.0f) * 0.7f;
                        //var spreadRadius = 0.3f + math.pow(i / 31.0f, 5.0f) * 0.7f;

                        // sample around the active working direction.
                        float3 cube_direction = AddRandomSpread(light_direction_negative, spreadRadius: 0.3f);

                        // do photon cube prerequisite computations to access data faster.
                        photonCube.FastSamplePrerequisite(cube_direction, out var photonCubeFace, out var photonCubeFaceIndex);

                        // check whether the world position received direct illumination.
                        var photonWorld = photonCube.SampleWorldFast(cube_direction, lightPosition3, photonCubeFace, photonCubeFaceIndex);
                        var photonNormal = photonCube.SampleNormalFast(photonCubeFace, photonCubeFaceIndex);
                        var photonDiffuse = photonCube.SampleDiffuseFast(photonCubeFace, photonCubeFaceIndex);

                        // ---
                        // source code from https://github.com/Arlorean/UnityComputeShaderTest (see Licenses/UnityComputeShaderTest.txt).
                        // shoutouts to Adam Davidson and Kevin Fung!
                        //
                        // schlick's approximation.
                        float3 r0 = photonDiffuse * materialSpecular;
                        float hv = Mathf.Clamp(math.dot(photonNormal, light_direction), 0.0f, 1.0f);
                        float3 fresnel = r0 + (1.0f - r0) * math.pow(1.0f - hv, 5.0f);
                        raycastHandler.fresnels[i] = fresnel;
                        // ---

                        var photonWorldMinusWorldWithNormalOffset = photonWorld - worldWithNormalOffset3;
                        var photonToWorldDirection = math.normalize(photonWorldMinusWorldWithNormalOffset);
                        var photonToWorldDistance = math.length(photonWorldMinusWorldWithNormalOffset);

                        // create a raycast command as fast as possible.
                        raycastCommand.from = worldWithNormalOffset;
                        raycastCommand.direction = *(Vector3*)&photonToWorldDirection;
                        raycastCommand.distance = photonToWorldDistance;

                        callbackRaycastProcessor.Add(
                            raycastCommand,
                            raycastHandler
                        );
                    }
                    raycastHandler.Ready();
                }
            }

            triangleUvTo3dStep.Dispose();
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
            var triangleRaycastedLightIndices = dynamic_triangles.GetRaycastedLightIndices(triangle_index);
            var triangleRaycastedLightIndicesCount = triangleRaycastedLightIndices.Count;

            // only iterate over lights associated with the current triangle.
            for (int i = triangleRaycastedLightIndicesCount; i-- > 0;)
            {
                // the bounce texture is only set on a triangle when actually receiving bounced lighting.
                if (bounceLightingInScene && dynamic_triangles.GetBounceTexture(triangle_index, i) != null)
                    continue;

                var pointLight = pointLights[triangleRaycastedLightIndices[i]];
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

                // the light never illuminated this triangle:
                if (!lightFound)
                {
                    optimizationLightsRemoved++;
                    dynamic_triangles.RemoveLightFromTriangle(triangle_index, i);
                }
            }
        }

        private unsafe void BuildShadows(int triangle_index, uint* pixels_lightmap, DynamicTrianglesBuilder dynamic_triangles, Vector2 t1, Vector2 t2, Vector2 t3)
        {
            // now the lightmap pixels have also been padded and all the unused light sources have
            // been removed from the triangle, so we only have to store the 1bpp light occlusion
            // bits per light source (instead of always using 32 bits per fragment).

            // calculate the bounding box of the polygon in UV space.
            var triangleBoundingBox = MathEx.ComputeTriangleBoundingBox(t1, t2, t3);

            // triangles may be so thin and small that they do not have their own UV texels, so we
            // expand the bounding box by one pixel on the shadow occlusion map to include the
            // neighbouring texels, as failure to do so will leave them without all of their light
            // sources (i.e. fully black).
            var minX = Mathf.FloorToInt(triangleBoundingBox.xMin * lightmapSize) - 2;
            var minY = Mathf.FloorToInt(triangleBoundingBox.yMin * lightmapSize) - 2;
            var maxX = Mathf.CeilToInt(triangleBoundingBox.xMax * lightmapSize) + 2;
            var maxY = Mathf.CeilToInt(triangleBoundingBox.yMax * lightmapSize) + 2;

            // clamp the pixel coordinates so that we can safely read from our arrays.
            minX = Mathf.Clamp(minX, 0, lightmapSize - 1);
            minY = Mathf.Clamp(minY, 0, lightmapSize - 1);
            maxX = Mathf.Clamp(maxX, 0, lightmapSize - 1);
            maxY = Mathf.Clamp(maxY, 0, lightmapSize - 1);

            // calculate the shadow bits size and intentionally add 2px padding, as the shader with
            // bilinear filtering will otherwise read outside the bounds on the UV borders, causing
            // visual artifacts to appear as lines of shadow.
            var shadowBitsWidth = 5 + maxX - minX;
            var shadowBitsHeight = 5 + maxY - minY;

            // prepare scratch memory to hold a copy of the shadow bits used during dilation.
            var shadowBitsScratchMemory = new BitArray2(shadowBitsWidth, shadowBitsHeight);

            // prepare to iterate over lights associated with the current triangle.
            var triangleRaycastedLightIndices = dynamic_triangles.GetRaycastedLightIndices(triangle_index);
            var triangleRaycastedLightIndicesCount = triangleRaycastedLightIndices.Count;

            // only iterate over lights associated with the current triangle.
            for (int i = 0; i < triangleRaycastedLightIndicesCount; i++)
            {
                var pointLight = pointLights[triangleRaycastedLightIndices[i]];
                var lightChannelBit = (uint)1 << ((int)pointLight.lightChannel);

                // intentionally add 2px padding, as the shader with bilinear filtering will
                // otherwise read outside the bounds on the UV borders, causing visual artifacts to
                // appear as lines of shadow.
                var shadowBits = new BitArray2(shadowBitsWidth, shadowBitsHeight);

                var yy = 2;
                for (int y = minY; y <= maxY; y++)
                {
                    int yPtr = y * lightmapSize;

                    var xx = 2;
                    for (int x = minX; x <= maxX; x++)
                    {
                        int xyPtr = yPtr + x;

                        if ((pixels_lightmap[xyPtr] & lightChannelBit) > 0)
                        {
                            shadowBits[xx, yy] = true;
                        }

                        xx++;
                    }

                    yy++;
                }

                // dilate the border pixels to fill the 2px padding.
                DilateShadowBits(shadowBits, shadowBitsScratchMemory);

                dynamic_triangles.SetShadowOcclusionBits(triangle_index, i, shadowBits);
            }
        }

        private void DilateShadowBits(BitArray2 shadowBits, BitArray2 copy)
        {
            var width = shadowBits.Width;
            var height = shadowBits.Height;

            shadowBits.CopyTo(copy);

            if (height >= 2)
                DilateShadowBitsRow(shadowBits, copy, width, 1);
            if (height >= 4)
                DilateShadowBitsRow(shadowBits, copy, width, height - 2);
            if (width >= 2)
                DilateShadowBitsColumn(shadowBits, copy, height, 1);
            if (width >= 4)
                DilateShadowBitsColumn(shadowBits, copy, height, width - 2);

            shadowBits.CopyTo(copy);

            if (height >= 1)
                DilateShadowBitsRow(shadowBits, copy, width, 0);
            if (height >= 3)
                DilateShadowBitsRow(shadowBits, copy, width, height - 1);
            if (width >= 1)
                DilateShadowBitsColumn(shadowBits, copy, height, 0);
            if (width >= 3)
                DilateShadowBitsColumn(shadowBits, copy, height, width - 1);
        }

        private void DilateShadowBitsRow(BitArray2 shadowBits, BitArray2 copy, int width, int y)
        {
            for (int x = 0; x < width; x++)
            {
                bool c = copy[x, y];
                c = c ? c : TryGetShadowBit(copy, x - 1, y - 1);
                c = c ? c : TryGetShadowBit(copy, x, y - 1);
                c = c ? c : TryGetShadowBit(copy, x + 1, y - 1);
                c = c ? c : TryGetShadowBit(copy, x - 1, y);
                c = c ? c : TryGetShadowBit(copy, x + 1, y);
                c = c ? c : TryGetShadowBit(copy, x - 1, y + 1);
                c = c ? c : TryGetShadowBit(copy, x, y + 1);
                c = c ? c : TryGetShadowBit(copy, x + 1, y + 1);
                shadowBits[x, y] = c;
            }
        }

        private void DilateShadowBitsColumn(BitArray2 shadowBits, BitArray2 copy, int height, int x)
        {
            for (int y = 0; y < height; y++)
            {
                bool c = copy[x, y];
                c = c ? c : TryGetShadowBit(copy, x - 1, y - 1);
                c = c ? c : TryGetShadowBit(copy, x, y - 1);
                c = c ? c : TryGetShadowBit(copy, x + 1, y - 1);
                c = c ? c : TryGetShadowBit(copy, x - 1, y);
                c = c ? c : TryGetShadowBit(copy, x + 1, y);
                c = c ? c : TryGetShadowBit(copy, x - 1, y + 1);
                c = c ? c : TryGetShadowBit(copy, x, y + 1);
                c = c ? c : TryGetShadowBit(copy, x + 1, y + 1);
                shadowBits[x, y] = c;
            }
        }

        // todo: optimize this.
        private bool TryGetShadowBit(BitArray2 shadowBits, int x, int y)
        {
            if (x >= 0 && y >= 0 && x < shadowBits.Width && y < shadowBits.Height)
                return shadowBits[x, y];
            return false;
        }

        private unsafe void BuildBounceTextures(int light_index, int triangle_index, Color* pixels_bounce, DynamicTrianglesBuilder dynamic_triangles, Vector2 t1, Vector2 t2, Vector2 t3)
        {
            // now the lightmap pixels have also been padded and all the unused light sources have
            // been removed from the triangle, so we only have to store the 1bpp light occlusion
            // bits per light source (instead of always using 32 bits per fragment).

            // prepare to only process the current light source if it exists on the current triangle.
            var triangleLightIndex = dynamic_triangles.TriangleGetRaycastedLightIndex(triangle_index, light_index);
            if (triangleLightIndex == -1)
                return;

            // calculate the bounding box of the polygon in UV space.
            var triangleBoundingBox = MathEx.ComputeTriangleBoundingBox(t1, t2, t3);

            // triangles may be so thin and small that they do not have their own UV texels, so we
            // expand the bounding box by one pixel on the shadow occlusion map to include the
            // neighbouring texels, as failure to do so will leave them without all of their light
            // sources (i.e. fully black).
            var minX = Mathf.FloorToInt(triangleBoundingBox.xMin * lightmapSize) - 2;
            var minY = Mathf.FloorToInt(triangleBoundingBox.yMin * lightmapSize) - 2;
            var maxX = Mathf.CeilToInt(triangleBoundingBox.xMax * lightmapSize) + 2;
            var maxY = Mathf.CeilToInt(triangleBoundingBox.yMax * lightmapSize) + 2;

            // clamp the pixel coordinates so that we can safely read from our arrays.
            minX = Mathf.Clamp(minX, 0, lightmapSize - 1);
            minY = Mathf.Clamp(minY, 0, lightmapSize - 1);
            maxX = Mathf.Clamp(maxX, 0, lightmapSize - 1);
            maxY = Mathf.Clamp(maxY, 0, lightmapSize - 1);

            // intentionally add 2px padding, as the shader with bilinear filtering will
            // otherwise read outside the bounds on the UV borders, causing visual artifacts to
            // appear as lines of shadow.
            var bounceTexture = new Color[(5 + maxX - minX) * (5 + maxY - minY)];

            // todo: surely there's a clever memory copy function for this?
            var yy = 2;
            for (int y = minY; y <= maxY; y++)
            {
                int yPtr = y * lightmapSize;

                var xx = 2;
                for (int x = minX; x <= maxX; x++)
                {
                    int xyPtr = yPtr + x;

                    bounceTexture[yy * (5 + maxX - minX) + xx] = pixels_bounce[xyPtr];

                    xx++;
                }

                yy++;
            }

            dynamic_triangles.SetBounceTexture(triangle_index, triangleLightIndex, bounceTexture);
        }

        private unsafe void DilateBounceTexture(Color* bounceTexture, Color[] original)
        {
            var copy = (Color[])original.Clone();
            for (int y = 0; y < lightmapSize; y++)
            {
                for (int x = 0; x < lightmapSize; x++)
                {
                    // todo: optimize this (very wasteful iterations).
                    //if (x >= 2 && y >= 2 && x < lightmapSize - 2 && y < lightmapSize - 2)
                    //continue;

                    var c = TryGetBounceTexturePixel(copy, x, y);
                    c = c.a > 0.0 ? c : TryGetBounceTexturePixel(copy, x - 1, y - 1);
                    c = c.a > 0.0 ? c : TryGetBounceTexturePixel(copy, x, y - 1);
                    c = c.a > 0.0 ? c : TryGetBounceTexturePixel(copy, x + 1, y - 1);
                    c = c.a > 0.0 ? c : TryGetBounceTexturePixel(copy, x - 1, y);
                    c = c.a > 0.0 ? c : TryGetBounceTexturePixel(copy, x + 1, y);
                    c = c.a > 0.0 ? c : TryGetBounceTexturePixel(copy, x - 1, y + 1);
                    c = c.a > 0.0 ? c : TryGetBounceTexturePixel(copy, x, y + 1);
                    c = c.a > 0.0 ? c : TryGetBounceTexturePixel(copy, x + 1, y + 1);
                    bounceTexture[y * lightmapSize + x] = c;
                }
            }
        }

        // todo: optimize this.
        private unsafe Color TryGetBounceTexturePixel(Color[] bounceTexture, int x, int y)
        {
            if (x >= 0 && y >= 0 && x < lightmapSize && y < lightmapSize)
                return bounceTexture[y * lightmapSize + x];
            return new Color(0.0f, 0.0f, 0.0f, 0.0f);
        }

        private unsafe void BoxBlurBounceTexture(Color* bounceTexture, Color[] original)
        {
            var copy = (Color[])original.Clone();
            for (int y = 0; y < lightmapSize; y++)
            {
                for (int x = 0; x < lightmapSize; x++)
                {
                    Color map;

                    map = TryGetBounceTexturePixel(copy, x - 1, y - 1);
                    map += TryGetBounceTexturePixel(copy, x, y - 1);
                    map += TryGetBounceTexturePixel(copy, x + 1, y - 1);

                    map += TryGetBounceTexturePixel(copy, x - 1, y);
                    map += TryGetBounceTexturePixel(copy, x, y);
                    map += TryGetBounceTexturePixel(copy, x + 1, y);

                    map += TryGetBounceTexturePixel(copy, x - 1, y + 1);
                    map += TryGetBounceTexturePixel(copy, x, y + 1);
                    map += TryGetBounceTexturePixel(copy, x + 1, y + 1);

                    bounceTexture[y * lightmapSize + x] = map / 9.0f;
                }
            }
        }
    }
}