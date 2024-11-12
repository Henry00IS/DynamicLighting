using AlpacaIT.DynamicLighting.Internal;
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
        public int maximumLightmapSize = 2048;

        /// <summary>
        /// The default compression level for bounce lighting data. Choosing a higher compression
        /// can reduce VRAM usage, but may result in reduced visual quality. For best results,
        /// adjust based on your VRAM availability and visual preferences.
        /// </summary>
        public DynamicBounceLightingDefaultCompressionMode bounceLightingDefaultCompression = DynamicBounceLightingDefaultCompressionMode.EightBitsPerPixel;

        /// <summary>
        /// The flags controlling aspects of the raytracing process, such as skipping certain computations.
        /// </summary>
        public DynamicLightingTracerFlags tracerFlags = DynamicLightingTracerFlags.None;

        /// <summary>Called when this tracer instance has been cancelled.</summary>
#pragma warning disable CS0067

        public event System.EventHandler<System.EventArgs> cancelled;

#pragma warning restore CS0067

        private uint traces = 0;
        private uint optimizationLightsRemoved = 0;
        private bool bounceLightingInScene = false;
        private BenchmarkTimer totalTime;
        private BenchmarkTimer tracingTime;
        private BenchmarkTimer bounceTime;
        private BenchmarkTimer seamTime;
        private BenchmarkTimer optimizationTime;
        private BenchmarkTimer bvhTime;
        private BenchmarkTimer buildShadowsTime;
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
        private BvhLightStructure bvhDynamicLights;
        private int[] bvhDynamicLightIndices;
        private DynamicLightManager dynamicLightManager;
        private StringBuilder log;
        private static RaycastCommand raycastCommand;

#if UNITY_EDITOR
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
            bounceTime = new BenchmarkTimer();
            seamTime = new BenchmarkTimer();
            optimizationTime = new BenchmarkTimer();
            bvhTime = new BenchmarkTimer();
            buildShadowsTime = new BenchmarkTimer();
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
                bvhDynamicLights = new BvhLightStructure(dynamicLights);
                var bvhDynamicLights32 = bvhDynamicLights.ToUInt32Array();
                vramBvhTotal += (ulong)bvhDynamicLights32.Length * 4;
                if (!dynamicLightManager.raycastedScene.dynamicLightsBvh.Write(bvhDynamicLights32))
                    Debug.LogError($"Unable to compress and store the dynamic lights bounding volume hierarchy!");

                // create the point lights array with the order the bvh tree desires.
                pointLights = new DynamicLight[dynamicLights.Length];
                for (int i = 0; i < dynamicLights.Length; i++)
                    pointLights[i] = dynamicLights[bvhDynamicLights.dynamicLightsIdx[i]]; // pigeonhole sort!

                bvhDynamicLightIndices = new int[dynamicLights.Length];
            }
            else
            {
                pointLights = dynamicLights;
                bvhDynamicLightIndices = new int[0];
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
#if UNITY_EDITOR
                var progressTitle = "Dynamic Lighting";
                var progressDescription = "Initial warmup...";
                if (UnityEditor.EditorUtility.DisplayCancelableProgressBar(progressTitle, progressDescription, 0f))
                    progressBarCancel = true;
#endif
                // assign the dynamic lights in the scene to the dynamic light manager.
                dynamicLightManager.raycastedDynamicLights.Clear();
                for (int i = 0; i < pointLights.Length; i++)
                {
                    var light = pointLights[i];
                    dynamicLightManager.raycastedDynamicLights.Add(new RaycastedDynamicLight(light, bounceLightingDefaultCompression));
                    pointLightsCache[i] = new CachedLightData(light);

                    bool requiresPhotonCube = false;
                    bool storeNormals = false;

                    // computing transparency in raycasted shadows requires a photon cube.
                    if (light.lightTransparency != DynamicLightTransparencyMode.Disabled)
                        requiresPhotonCube = true;

                    // check for the usage of bounce lighting in the scene.
                    if (light.lightIllumination == DynamicLightIlluminationMode.SingleBounce && !tracerFlags.HasFlag(DynamicLightingTracerFlags.SkipBounceLighting))
                    {
                        // bounce lighting requires a photon cube.
                        requiresPhotonCube = true;
                        storeNormals = true;

                        // remember whether bounce lighting is used in the scene, this allows us to
                        // skip steps and checks later on.
                        bounceLightingInScene = true;

                        // also save this flag into the scene.
                        dynamicLightManager.activateBounceLightingInCurrentScene = true;
                    }

                    // render and create photon cubes for all lights that require it.
                    if (requiresPhotonCube)
                    {
#if UNITY_EDITOR
                        progressDescription = "Computing Photon Cubes on the Graphics Card...";
                        if (UnityEditor.EditorUtility.DisplayCancelableProgressBar(progressTitle, progressDescription, i / (float)pointLights.Length))
                            progressBarCancel = true;
#endif
                        pointLightsCache[i].photonCube = PhotonCameraRender(pointLightsCache[i].position, light.lightRadius, storeNormals);
                    }
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
                log.Append("Raycasts: ").Append(traces).Append(" (").Append(tracingTime.ToString()).AppendLine(")");
                log.Append("Bounce Lighting: ").AppendLine(bounceTime.ToString());
                log.Append("Bounding Volume Hierarchy: ").AppendLine(bvhTime.ToString());
                log.Append("Occlusion Bits Seams Padding: ").AppendLine(seamTime.ToString());
                log.Append("Dynamic Triangles Optimization: ").Append(optimizationLightsRemoved).Append(" Light Sources Removed (").Append(optimizationTime.ToString()).AppendLine(")");
                log.Append("Building Shadows: ").AppendLine(buildShadowsTime.ToString());
                log.AppendLine("--------------------------------");
                log.Append("VRAM Dynamic Triangles: ").Append(MathEx.BytesToUnitString(vramDynamicTrianglesTotal)).Append(" (Legacy: ").Append(MathEx.BytesToUnitString(vramLegacyTotal)).AppendLine(")");
                log.Append("VRAM Bounding Volume Hierarchy: ").AppendLine(MathEx.BytesToUnitString(vramBvhTotal));
                log.Insert(0, $"The lighting requires {MathEx.BytesToUnitString(vramDynamicTrianglesTotal + vramBvhTotal)} VRAM on the graphics card to render the current scene ({totalTime}).{System.Environment.NewLine}");

                Debug.Log(log.ToString());
#if UNITY_EDITOR
                if (!dynamicLightManager.editorIsPlaying)
                {
                    UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
                }
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

                // dispose of all photon cubes as they use native memory.
                if (pointLightsCache != null)
                {
                    for (int i = 0; i < pointLightsCache.Length; i++)
                    {
                        var pointLightCache = pointLightsCache[i];
                        if (pointLightCache.photonCube != null)
                            pointLightCache.photonCube.Dispose();
                    }
                }

                // we must always reload the lighting to prevent thousands of errors.
                dynamicLightManager.Reload();
            }
        }

        private unsafe void Raytrace(MeshFilter meshFilter, float progressMin, float progressMax)
        {
            var meshBuilder = new MeshBuilder(meshFilter.transform.localToWorldMatrix, meshFilter.sharedMesh);
            lightmapSize = MathEx.SurfaceAreaToTextureSize(meshBuilder.surfaceArea, pixelDensityPerSquareMeter);
            if (lightmapSize > maximumLightmapSize)
                lightmapSize = maximumLightmapSize;
            lightmapSizeMin1 = lightmapSize - 1;

            if (!meshBuilder.hasLightmapCoordinates)
            {
                Debug.LogWarning("Raytracer skipping " + meshFilter.name + " because it does not have uv1 lightmap coordinates!", meshFilter);
                return;
            }
            else
            {
                // estimate the amount of vram required (purely statistical).
                ulong vramLightmap = (ulong)(lightmapSize * lightmapSize * 4); // uint32
                vramLegacyTotal += vramLightmap;
            }

#if UNITY_EDITOR
            var progressLightmapSizeString = lightmapSize.ToString();
            var progressTitle = "Raytracing Scene " + meshBuilder.surfaceArea.ToString("0.00") + "m² (" + progressLightmapSizeString + "x" + progressLightmapSizeString + ")";
            var progressName = meshFilter.name;
            var progressDescription = $"Raytracing Direct Lighting: {progressName}";
            if (UnityEditor.EditorUtility.DisplayCancelableProgressBar(progressTitle, progressDescription, progressMin))
                progressBarCancel = true;
#endif
            tracingTime.Begin();
            var dynamic_triangles = new DynamicTrianglesBuilder(meshBuilder, lightmapSize, pointLights, dynamicLightManager);
            var pixels_lightmap = new uint[lightmapSize * lightmapSize];
            var pixels_visited = new bool[lightmapSize * lightmapSize];
            var pixels_lightmap_gc = GCHandle.Alloc(pixels_lightmap, GCHandleType.Pinned);
            var pixels_visited_gc = GCHandle.Alloc(pixels_visited, GCHandleType.Pinned);
            var pixels_lightmap_ptr = (uint*)pixels_lightmap_gc.AddrOfPinnedObject();
            var pixels_visited_ptr = (bool*)pixels_visited_gc.AddrOfPinnedObject();

            // prepare to raycast the entire mesh using multi-threading.
            raycastProcessor.pixelsLightmap = pixels_lightmap_ptr;

            // iterate over all triangles in the mesh.
#if UNITY_EDITOR
            if (!progressBarCancel)
            {
#endif
                for (int i = 0; i < meshBuilder.triangleCount; i++)
                {
                    RaycastTriangle(i, dynamic_triangles, pixels_visited_ptr, pixels_lightmap_ptr, meshBuilder);
                }
#if UNITY_EDITOR
            }
#endif
            // finish any remaining raycasting work.
            raycastProcessor.Complete();
            tracingTime.Stop();

            // only executed when bounce lighting is detected in the scene.
            if (bounceLightingInScene)
            {
#if UNITY_EDITOR
                progressDescription = $"Raytracing Bounce Lighting: {progressName}";
                if (UnityEditor.EditorUtility.DisplayCancelableProgressBar(progressTitle, progressDescription, Mathf.Lerp(progressMin, progressMax, 0.2f)))
                    progressBarCancel = true;

                if (!progressBarCancel)
                {
#endif
                    bounceTime.Begin();
                    // iterate over all lights intersecting the mesh bounds.
                    int totalIntersectingPointLights = 0;
                    if (bvhDynamicLights != null)
                        totalIntersectingPointLights = bvhDynamicLights.FindLightsIntersecting(meshBuilder.worldBounds, bvhDynamicLightIndices);
                    for (int j = 0; j < totalIntersectingPointLights; j++)
                    {
                        var intersectingLightIndex = bvhDynamicLightIndices[j];
                        var pointLight = pointLights[intersectingLightIndex];

                        // the light must have bounce lighting enabled.
                        if (pointLight.lightIllumination != DynamicLightIlluminationMode.SingleBounce)
                            continue;

                        // create a bounce lighting texture.
                        var pixels_bounce = new float[lightmapSize * lightmapSize];
                        var pixels_bounce_gc = GCHandle.Alloc(pixels_bounce, GCHandleType.Pinned);
                        var pixels_bounce_ptr = (float*)pixels_bounce_gc.AddrOfPinnedObject();

                        // iterate over all triangles in the mesh.
                        for (int i = 0; i < meshBuilder.triangleCount; i++)
                        {
                            BounceTriangle(intersectingLightIndex, i, dynamic_triangles, pixels_bounce_ptr, meshBuilder);
                        }

                        // finish any remaining raycasting work.
                        callbackRaycastProcessor.Complete();

                        DilateBounceTexture(pixels_bounce_ptr, pixels_bounce);
                        GaussianBlur.ApplyGaussianBlur(pixels_bounce_ptr, pixels_bounce, lightmapSize, 7, 5);

                        // iterate over all triangles in the mesh.
                        for (int i = 0; i < meshBuilder.triangleCount; i++)
                        {
                            BuildBounceTextures(intersectingLightIndex, i, pixels_bounce_ptr, dynamic_triangles, meshBuilder);
                        }

                        // free the bounce lighting texture.
                        pixels_bounce_gc.Free();
                    }
                    bounceTime.Stop();
#if UNITY_EDITOR
                }
#endif
            }

            // optimize the runtime performance.
            // iterate over all triangles in the mesh.
            optimizationTime.Begin();
            for (int i = 0; i < meshBuilder.triangleCount; i++)
            {
                OptimizeTriangle(i, pixels_lightmap_ptr, dynamic_triangles, meshBuilder);
            }
            optimizationTime.Stop();

            seamTime.Begin();
            {
                SeamsPaddingStep seamsPaddingStep = new SeamsPaddingStep(pixels_lightmap_ptr, pixels_visited_ptr, lightmapSize);
                seamsPaddingStep.Execute();
            }
            seamTime.Stop();

            // iterate over all triangles in the mesh.
            buildShadowsTime.Begin();
            for (int i = 0; i < meshBuilder.triangleCount; i++)
            {
                BuildShadows(i, pixels_lightmap_ptr, dynamic_triangles, meshBuilder);
            }
            buildShadowsTime.Stop();

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

        private unsafe void RaycastTriangle(int triangle_index, DynamicTrianglesBuilder dynamic_triangles, bool* pixels_visited, uint* pixels_lightmap, MeshBuilder meshBuilder)
        {
            var (v1, v2, v3) = meshBuilder.GetTriangleVertices(triangle_index);
            var (n1, n2, n3) = meshBuilder.GetTriangleNormals(triangle_index);

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
            // we query the bounding volume hierarchy to speed up the search.
            int totalIntersectingPointLights = 0;
            if (bvhDynamicLights != null)
                totalIntersectingPointLights = bvhDynamicLights.FindLightsIntersecting(triangleBounds, bvhDynamicLightIndices);
            for (int i = 0; i < totalIntersectingPointLights; i++)
            {
                var intersectingLightIndex = bvhDynamicLightIndices[i];
                var light = pointLights[intersectingLightIndex];
                var lightCache = pointLightsCache[intersectingLightIndex];
                var lightPosition = lightCache.position;

                // indirect light can bounce onto a triangle with any normal, so the following
                // optimization only works when light bouncing is disabled for this light source.
                // when normals are all the same (flat shading) then we can exclude triangles facing
                // away from the light early on here.
                if (triangleNormalValid && (light.lightIllumination == DynamicLightIlluminationMode.DirectIllumination || tracerFlags.HasFlag(DynamicLightingTracerFlags.SkipBounceLighting)) && n1.ApproximatelyEquals(n2) && n2.ApproximatelyEquals(n3))
                {
                    // [unsafe] lightDirection = (lightPosition - triangleCenter).normalized
                    lightDirection = lightPosition;
                    UMath.Subtract(lightDirectionPtr, triangleCenterPtr);
                    UMath.Normalize(lightDirectionPtr);
                    if (UMath.Dot(triangleNormalPtr, lightDirectionPtr) <= -0.1f) // slight tolerance.
                        continue;
                }

                // ensure the triangle intersects with the light sphere.
                if (!MathEx.CheckSphereIntersectsTriangle(lightPosition, light.lightRadius, v1, v2, v3))
                    continue;

                // this light can affect the triangle.
                dynamic_triangles.AddRaycastedLightToTriangle(triangle_index, intersectingLightIndex);
            }

            // skip degenerate triangles.
            if (!triangleNormalValid) return;

            // do some initial uv to 3d work here and also determine whether we can early out.
            var (t1, t2, t3) = meshBuilder.GetTriangleUv1(triangle_index);
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
            var triangleUvTo3dStep = new TriangleUvToFull3dStep(v1, v2, v3, n1, n2, n3, t1, t2, t3, triangleSurfaceArea, triangleBoundingBox, lightmapSize);
            triangleUvTo3dStep.Execute();
            var uvWorldPositions = triangleUvTo3dStep.worldPositionsPtr;
            var uvWorldNormals = triangleUvTo3dStep.worldNormalsPtr;

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
            Vector3 normal;
            var normalPtr = &normal;
            var localTracesCounter = 0u;

            // we can do less work than the constructor and recycle this memory.
            RaycastCommandMeta raycastCommandMeta; _ = &raycastCommandMeta;

            // iterate over the lights potentially affecting this triangle.
            for (int i = 0; i < triangleLightIndicesCount; i++)
            {
                var lightIndex = triangleLightIndices[i];
                var pointLight = pointLights[lightIndex];
                var pointLightCache = pointLightsCache[lightIndex];
                var lightPosition = pointLightCache.position;
                var lightPhotonCube = pointLightCache.photonCube;
                var lightRadius = pointLight.lightRadius;
                var lightRadiusSqr = lightRadius * lightRadius;
                var lightTransparency = pointLight.lightTransparency;
                var lightChannel = (int)pointLight.lightChannel;
                raycastCommandMeta.lightChannel = lightChannel;

                int ptr = 0;
                for (int y = minY; y <= maxY; y++)
                {
                    int yPtr = y * lightmapSize;

                    for (int x = minX; x <= maxX; x++)
                    {
                        // fetch the world position and interpolated normal for the current uv coordinate.
                        world = uvWorldPositions[ptr];
                        normal = uvWorldNormals[ptr++];
                        if (UMath.IsZero(worldPtr)) continue;

                        // [unsafe] lightDirection = lightPosition - world;
                        lightDirection = lightPosition;
                        UMath.Subtract(lightDirectionPtr, worldPtr);

                        // early out by distance.
                        var lightDistanceToWorldSqr = UMath.Dot(lightDirectionPtr, lightDirectionPtr);
                        if (lightDistanceToWorldSqr > lightRadiusSqr)
                            continue;

                        // early out by interpolated normal (smooth normals).
                        // [unsafe] lightDirection = lightDirection.normalized
                        UMath.Normalize(lightDirectionPtr);
                        if (UMath.Dot(normalPtr, lightDirectionPtr) <= -0.1f) // slight tolerance.
                            continue;

                        int xyPtr = yPtr + x;
                        var lightDistanceToWorld = Mathf.Sqrt(lightDistanceToWorldSqr);

                        // when using alpha transparency we already did all the work on the graphics card.
                        if (lightTransparency != DynamicLightTransparencyMode.Disabled)
                        {
                            if (lightTransparency == DynamicLightTransparencyMode.EnabledMax)
                            {
                                if (lightPhotonCube.SampleShadowMax(lightDirection, lightDistanceToWorld, triangleNormal))
                                {
                                    pixels_lightmap[xyPtr] |= (uint)1 << lightChannel;
                                }
                            }
                            else
                            {
                                if (lightPhotonCube.SampleShadow(lightDirection, lightDistanceToWorld, triangleNormal))
                                {
                                    pixels_lightmap[xyPtr] |= (uint)1 << lightChannel;
                                }
                            }
                        }
                        else
                        {
                            // prepare to trace from the world to the light position.

                            // [unsafe] world + triangleNormalOffset
                            var worldPlusTriangleNormalOffset = world;
                            UMath.Add(&worldPlusTriangleNormalOffset, triangleNormalOffsetPtr);

                            // create a raycast command as fast as possible.
                            raycastCommand.from = worldPlusTriangleNormalOffset;
                            raycastCommand.direction = lightDirection;
                            raycastCommand.distance = lightDistanceToWorld;
                            raycastCommandMeta.xyPtr = xyPtr;

                            localTracesCounter++;
                            raycastProcessor.Add(raycastCommand, raycastCommandMeta);
                        }

                        // write this pixel into the visited map.
                        pixels_visited[xyPtr] = true;
                    }
                }
            }

            // much faster to add this outside of the loop.
            traces += localTracesCounter;

            triangleUvTo3dStep.Dispose();
        }

        private Vector3 AddRandomSpread(Vector3 direction, float t)
        {
            Vector3 randomDir = UnityEngine.Random.onUnitSphere;

            return Vector3.Slerp(direction, randomDir, t);
        }

        private unsafe class BounceTriangleRaycastMissHandler : RaycastHandler
        {
            /// <summary>
            /// For writing to <see cref="pixelsBouncePtr"/>:
            /// <code>y * lightmapSize + x</code>
            /// </summary>
            private int xyPtr;
            private float* pixelsBouncePtr;
            private Vector3 surfaceNormal;
            private float lightBounceIntensity;

            private float accumulator;

            public Vector3[] photonNormals;
            public Vector3[] directions;

            public void Setup(float* pixelsBouncePtr, int xyPtr, Vector3 surfaceNormal, int lightBounceSamples, float lightBounceIntensity)
            {
                this.xyPtr = xyPtr;
                this.pixelsBouncePtr = pixelsBouncePtr;
                this.surfaceNormal = surfaceNormal;
                this.lightBounceIntensity = lightBounceIntensity;

                accumulator = 0f;

                if (photonNormals == null || photonNormals.Length != lightBounceSamples)
                {
                    photonNormals = new Vector3[lightBounceSamples];
                    directions = new Vector3[lightBounceSamples];
                }
            }

            public override void OnRaycastMiss()
            {
                int i = raycastsIndex;

                var n_s = surfaceNormal;
                var n_p = photonNormals[i];
                var d = directions[i];

                Vector3 negative_d;
                negative_d.x = -d.x;
                negative_d.y = -d.y;
                negative_d.z = -d.z;

                float dot_ns_d = Mathf.Max(Vector3.Dot(n_s, d), 0f);
                float dot_np_minus_d = Mathf.Max(Vector3.Dot(n_p, negative_d), 0f);

                float attenuation = lightBounceIntensity;
                float E_out = dot_ns_d * dot_np_minus_d * attenuation;

                accumulator += E_out;
            }

            public override void OnHandlerFinished()
            {
                var average = accumulator / raycastsExpected;
                pixelsBouncePtr[xyPtr] = average;
            }
        }

        private unsafe void BounceTriangle(int light_index, int triangle_index, DynamicTrianglesBuilder dynamic_triangles, float* pixels_bounce_ptr, MeshBuilder meshBuilder)
        {
            // lights have already been associated with triangles that can potentially be affected
            // by them during the direct illumination step. bounce light sources also include
            // triangles facing away from the light source which is very important as bounce
            // lighting can go anywhere within the light radius.
            var (v1, v2, v3) = meshBuilder.GetTriangleVertices(triangle_index);

            // calculate the triangle normal (this may fail when degenerate or very small).
            var triangleNormal3 = math.normalizesafe(math.cross(v2 - v1, v3 - v1));
            var triangleNormalPtr = (Vector3*)&triangleNormal3;
            var triangleNormal = *triangleNormalPtr;
            var triangleNormalValid = UMath.IsNonZero(triangleNormalPtr);

            // skip degenerate triangles.
            if (!triangleNormalValid) return;

            // do some initial uv to 3d work here and also determine whether we can early out.
            var (t1, t2, t3) = meshBuilder.GetTriangleUv1(triangle_index);
            if (!MathEx.UvTo3dFastPrerequisite(t1, t2, t3, out float triangleSurfaceArea))
                return;

            // prepare to only process the current light source if it exists on the current triangle.
            // this early-out is more expensive than the work above and delaying it here is faster.
            if (!dynamic_triangles.TriangleHasRaycastedLight(triangle_index, light_index))
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
            // [unsafe] triangleNormal * 0.001f
            var triangleNormalOffset = triangleNormal;
            var triangleNormalOffsetPtr = &triangleNormalOffset;
            UMath.Scale(triangleNormalOffsetPtr, 0.001f);

            // optimize for the IL by preparing memory and pointers outside of the loop.
            Vector3 world;
            var worldPtr = &world;
            Vector3 lightDirectionNegative;
            var lightDirectionNegativePtr = &lightDirectionNegative;
            Vector3 photonWorldMinusWorldWithNormalOffset;
            var photonWorldMinusWorldWithNormalOffsetPtr = &photonWorldMinusWorldWithNormalOffset;
            Vector3 worldPlusTriangleNormalOffset;
            var worldPlusTriangleNormalOffsetPtr = &worldPlusTriangleNormalOffset;

            var pointLight = pointLights[light_index];
            var pointLightCache = pointLightsCache[light_index];
            var photonCube = pointLightCache.photonCube;
            var lightPosition = pointLightCache.position;
            var lightPositionPtr = &lightPosition;
            var lightRadius = pointLight.lightRadius;
            var lightRadiusSqr = lightRadius * lightRadius;
            var lightBounceSamples = pointLight.lightBounceSamples;
            var lightBounceIntensity = pointLight.lightBounceIntensity;

            // pre-computing part of this calculation:
            // var spreadRadius = 0.1f + i / (float)(lightBounceSamples - 1) * 0.9f;
            float spreadRadiusInverseSamples = 0.9f / (lightBounceSamples - 1);

            int ptr = 0;
            for (int y = minY; y <= maxY; y++)
            {
                int yPtr = y * lightmapSize;

                for (int x = minX; x <= maxX; x++)
                {
                    // fetch the world position for the current uv coordinate.
                    world = uvWorldPositions[ptr++];
                    if (UMath.IsZero(worldPtr)) continue;

                    // lightDirectionNegative = world - lightPosition;
                    lightDirectionNegative.x = world.x - lightPosition.x;
                    lightDirectionNegative.y = world.y - lightPosition.y;
                    lightDirectionNegative.z = world.z - lightPosition.z;

                    // early out by distance.
                    var lightDistanceToWorldSqr = UMath.Dot(lightDirectionNegativePtr, lightDirectionNegativePtr);
                    if (lightDistanceToWorldSqr > lightRadiusSqr)
                        continue;

                    // worldPlusTriangleNormalOffset = world + triangleNormalOffset
                    worldPlusTriangleNormalOffset.x = world.x + triangleNormalOffset.x;
                    worldPlusTriangleNormalOffset.y = world.y + triangleNormalOffset.y;
                    worldPlusTriangleNormalOffset.z = world.z + triangleNormalOffset.z;

                    var raycastHandler = bounceRaycastHandlerPool.GetInstance();
                    raycastHandler.Setup(pixels_bounce_ptr, yPtr + x, triangleNormal, lightBounceSamples, lightBounceIntensity);

                    // calculate the normalized direction between the light source and the fragment.
                    // [unsafe] lightDirectionNegative = lightDirectionNegative.normalized
                    UMath.Normalize(lightDirectionNegativePtr);

                    // do as much work as we can outside of the loop below.
                    raycastCommand.from = worldPlusTriangleNormalOffset;

                    for (int i = 0; i < lightBounceSamples; i++)
                    {
                        // sample around 0.1 but gradually take in the wider scene.
                        var spreadRadius = 0.1f + i * spreadRadiusInverseSamples;

                        // sample around the active working direction.
                        var randomSampleDirection = AddRandomSpread(lightDirectionNegative, spreadRadius);

                        photonCube.FastSamplePrerequisite(randomSampleDirection, out var photonCubeFace, out var photonCubeFaceIndex);
                        var photonWorld = photonCube.SampleWorldFast(randomSampleDirection, lightPosition, photonCubeFace, photonCubeFaceIndex);
                        var photonNormal = photonCube.SampleNormalFast(photonCubeFace, photonCubeFaceIndex);

                        // photonWorldMinusWorldWithNormalOffset = photonWorld - worldPlusTriangleNormalOffset;
                        photonWorldMinusWorldWithNormalOffset.x = photonWorld.x - worldPlusTriangleNormalOffset.x;
                        photonWorldMinusWorldWithNormalOffset.y = photonWorld.y - worldPlusTriangleNormalOffset.y;
                        photonWorldMinusWorldWithNormalOffset.z = photonWorld.z - worldPlusTriangleNormalOffset.z;

                        var photonToWorldDistance = Vector3.Magnitude(photonWorldMinusWorldWithNormalOffset);
                        // [unsafe] photonToWorldDirection = Vector3.Normalize(photonWorldMinusWorldWithNormalOffset);
                        UMath.Normalize(photonWorldMinusWorldWithNormalOffsetPtr);

                        raycastCommand.direction = photonWorldMinusWorldWithNormalOffset; // is photonToWorldDirection here.
                        raycastCommand.distance = photonToWorldDistance;

                        raycastHandler.photonNormals[i] = photonNormal;
                        raycastHandler.directions[i] = photonWorldMinusWorldWithNormalOffset; // is photonToWorldDirection here.

                        callbackRaycastProcessor.Add(raycastCommand, raycastHandler);
                    }

                    raycastHandler.Ready();
                }
            }

            triangleUvTo3dStep.Dispose();
        }

        private unsafe void OptimizeTriangle(int triangle_index, uint* pixels_lightmap, DynamicTrianglesBuilder dynamic_triangles, MeshBuilder meshBuilder)
        {
            // during the raycasting process, lights were associated per-triangle. This was
            // determined by the normal of the triangle (must face the light) and that the radius of
            // the light intersects the triangle, however, fully occluded walls may still have the
            // light associated with this logic alone. we use the raycasting results to determine
            // which triangles are truly affected by which lights and remove the lights that are not
            // affecting it. depending on the scene, this removes hundreds of thousands of lights
            // doubling the framerate.
            var (t1, t2, t3) = meshBuilder.GetTriangleUv1(triangle_index);

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

        private unsafe void BuildShadows(int triangle_index, uint* pixels_lightmap, DynamicTrianglesBuilder dynamic_triangles, MeshBuilder meshBuilder)
        {
            // now the lightmap pixels have also been padded and all the unused light sources have
            // been removed from the triangle, so we only have to store the 1bpp light occlusion
            // bits per light source (instead of always using 32 bits per fragment).
            var (t1, t2, t3) = meshBuilder.GetTriangleUv1(triangle_index);

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

        private unsafe void BuildBounceTextures(int light_index, int triangle_index, float* pixels_bounce, DynamicTrianglesBuilder dynamic_triangles, MeshBuilder meshBuilder)
        {
            // now the lightmap pixels have also been padded and all the unused light sources have
            // been removed from the triangle, so we only have to store the 1bpp light occlusion
            // bits per light source (instead of always using 32 bits per fragment).

            // prepare to only process the current light source if it exists on the current triangle.
            var triangleLightIndex = dynamic_triangles.TriangleGetRaycastedLightIndex(triangle_index, light_index);
            if (triangleLightIndex == -1)
                return;

            // calculate the bounding box of the polygon in UV space.
            var (t1, t2, t3) = meshBuilder.GetTriangleUv1(triangle_index);
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
            var bounceTexture = new float[(5 + maxX - minX) * (5 + maxY - minY)];
            var bounceTextureIlluminated = false;

            // todo: surely there's a clever memory copy function for this?
            var yy = 2;
            for (int y = minY; y <= maxY; y++)
            {
                int yPtr = y * lightmapSize;

                var xx = 2;
                for (int x = minX; x <= maxX; x++)
                {
                    int xyPtr = yPtr + x;

                    // check whether the bounce lighting illuminates anything.
                    float pixel = pixels_bounce[xyPtr];
                    if (pixel > 0.0f)
                        bounceTextureIlluminated = true;

                    bounceTexture[yy * (5 + maxX - minX) + xx] = pixel;

                    xx++;
                }

                yy++;
            }

            // only store the bounce lighting if it's actually illuminating the triangle.
            if (bounceTextureIlluminated)
                dynamic_triangles.SetBounceTexture(triangle_index, triangleLightIndex, bounceTexture);
        }

        private unsafe void DilateBounceTexture(float* bounceTexture, float[] original)
        {
            var copy = (float[])original.Clone();
            for (int y = 0; y < lightmapSize; y++)
            {
                for (int x = 0; x < lightmapSize; x++)
                {
                    // todo: optimize this (very wasteful iterations).
                    //if (x >= 2 && y >= 2 && x < lightmapSize - 2 && y < lightmapSize - 2)
                    //continue;

                    var c = TryGetBounceTexturePixel(copy, x, y);
                    c = c > 0.0 ? c : TryGetBounceTexturePixel(copy, x - 1, y - 1);
                    c = c > 0.0 ? c : TryGetBounceTexturePixel(copy, x, y - 1);
                    c = c > 0.0 ? c : TryGetBounceTexturePixel(copy, x + 1, y - 1);
                    c = c > 0.0 ? c : TryGetBounceTexturePixel(copy, x - 1, y);
                    c = c > 0.0 ? c : TryGetBounceTexturePixel(copy, x + 1, y);
                    c = c > 0.0 ? c : TryGetBounceTexturePixel(copy, x - 1, y + 1);
                    c = c > 0.0 ? c : TryGetBounceTexturePixel(copy, x, y + 1);
                    c = c > 0.0 ? c : TryGetBounceTexturePixel(copy, x + 1, y + 1);
                    bounceTexture[y * lightmapSize + x] = c;
                }
            }
        }

        // todo: optimize this.
        private unsafe float TryGetBounceTexturePixel(float[] bounceTexture, int x, int y)
        {
            if (x >= 0 && y >= 0 && x < lightmapSize && y < lightmapSize)
                return bounceTexture[y * lightmapSize + x];
            return 0.0f;
        }

        private unsafe void BoxBlurBounceTexture(float* bounceTexture, float[] original)
        {
            var copy = (float[])original.Clone();
            for (int y = 0; y < lightmapSize; y++)
            {
                for (int x = 0; x < lightmapSize; x++)
                {
                    float map;

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