using AlpacaIT.DynamicLighting.Internal;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace AlpacaIT.DynamicLighting
{
    [ExecuteInEditMode]
    public class DynamicLightManager : MonoBehaviour
    {
        /// <summary>Called when a <see cref="DynamicLight"/> gets registered (i.e. enabled).</summary>
        public event EventHandler<DynamicLightRegisteredEventArgs> lightRegistered;

        /// <summary>Called when a <see cref="DynamicLight"/> gets unregistered (i.e. disabled).</summary>
        public event EventHandler<DynamicLightUnregisteredEventArgs> lightUnregistered;

        /// <summary>Called when computing shadows for the current scene has started.</summary>
        public event EventHandler<EventArgs> traceStarted;

        /// <summary>Called when computing shadows for the current scene has been cancelled.</summary>
        public event EventHandler<EventArgs> traceCancelled;

        /// <summary>Called when computing shadows for the current scene has completed.</summary>
        public event EventHandler<EventArgs> traceCompleted;

        /// <summary>The light channel number used by realtime lights without baked shadows.</summary>
        public const int realtimeLightChannel = 32;

        /// <summary>
        /// The singleton dynamic lighting manager instance in the scene. Use <see cref="Instance"/>
        /// to access it.
        /// </summary>
        private static DynamicLightManager s_Instance;

        /// <summary>Gets the singleton dynamic lighting manager instance or creates it.</summary>
        public static DynamicLightManager Instance
        {
            get
            {
                // if known, immediately return the instance.
                if (s_Instance) return s_Instance;

                // C# hot reloading support: try finding an existing instance in the scene.
                s_Instance = FindObjectOfType<DynamicLightManager>();

                // otherwise create a new instance in scene.
                if (!s_Instance)
                    s_Instance = new GameObject("[Dynamic Light Manager]").AddComponent<DynamicLightManager>();

                return s_Instance;
            }
        }

        /// <summary>Whether an instance of the dynamic lighting manager has been created.</summary>
        public static bool hasInstance => s_Instance;

        /// <summary>
        /// The ambient lighting color is added to the whole scene, thus making it look like there
        /// is always some scattered light, even when there no direct light source. This prevents
        /// absolute black, dark patches from appearing in the scene that are impossible to see
        /// through (unless this is desired). This color should be very dark to achieve the best effect.
        /// </summary>
        [Tooltip("The ambient lighting color is added to the whole scene, thus making it look like there is always some scattered light, even when there no direct light source. This prevents absolute black, dark patches from appearing in the scene that are impossible to see through (unless this is desired). This color should be very dark to achieve the best effect.")]
        [ColorUsage(showAlpha: false)]
        public Color ambientColor = Color.black;

        /// <summary>
        /// The number of dynamic lights that can be active at the same time. If this budget is
        /// exceeded, lights that are out of view or furthest away from the camera will
        /// automatically fade out in a way that the player will hopefully not notice. A
        /// conservative budget as required by the level design will help older graphics hardware
        /// when there are hundreds of lights in the scene. Budgeting does not begin until the
        /// number of active dynamic lights actually exceeds this number.
        /// <para>The NVIDIA Quadro K1000M (2012) can handle 25 lights at 30fps.</para>
        /// <para>The NVIDIA GeForce GTX 1050 Ti (2016) can handle 125 lights between 53-68fps.</para>
        /// <para>The NVIDIA GeForce RTX 3080 (2020) can handle 2000 lights at 70fps.</para>
        /// </summary>
        [Tooltip("The number of dynamic lights that can be active at the same time. If this budget is exceeded, lights that are out of view or furthest away from the camera will automatically fade out in a way that the player will hopefully not notice. A conservative budget as required by the level design will help older graphics hardware when there are hundreds of lights in the scene. Budgeting does not begin until the number of active dynamic lights actually exceeds this number.\n\nThe NVIDIA Quadro K1000M (2012) can handle 25 lights at 30fps.\n\nThe NVIDIA GeForce GTX 1050 Ti (2016) can handle 125 lights between 53-68fps.\n\nThe NVIDIA GeForce RTX 3080 (2020) can handle 2000 lights at 70fps.")]
        [Min(0)]
        public int dynamicLightBudget = 64;

        /// <summary>
        /// The number of realtime dynamic lights that can be active at the same time. Realtime
        /// lights have no shadows and can move around the scene. They are useful for glowing
        /// particles, car headlights, etc. If this budget is exceeded, lights that are out of view
        /// or furthest away from the camera will automatically fade out in a way that the player
        /// will hopefully not notice. A conservative budget as per the game requirements will help
        /// older graphics hardware when there are many realtime lights in the scene. Budgeting does
        /// not begin until the number of active realtime dynamic lights actually exceeds this number.
        /// </summary>
        [Tooltip("The number of realtime dynamic lights that can be active at the same time. Realtime lights have no shadows and can move around the scene. They are useful for glowing particles, car headlights, etc. If this budget is exceeded, lights that are out of view or furthest away from the camera will automatically fade out in a way that the player will hopefully not notice. A conservative budget as per the game requirements will help older graphics hardware when there are many realtime lights in the scene. Budgeting does not begin until the number of active realtime dynamic lights actually exceeds this number.")]
        [Min(0)]
        public int realtimeLightBudget = 32;

        /// <summary>
        /// The layer mask used while raytracing to determine which hits to ignore. There are many
        /// scenarios where you have objects that should collide with everything in the scene, but
        /// not cause shadows. You should consider creating a physics layer (click on 'Layer:
        /// Default' at the top of the Game Object Inspector -&gt; Add Layer...) and naming it
        /// 'Collision'. You can then remove this layer from the list so it will be ignored by the
        /// raytracer. The rest of the scene will still have regular collisions with it. You can
        /// also do the opposite by creating a physics layer called 'Lighting' and disabling regular
        /// collisions with other colliders (in Edit -&gt; Project Settings -&gt; Physics), but
        /// leaving the layer checked in this list. Now you have a special shadow casting collision
        /// that nothing else can touch or interact with. These were just two example names, you can
        /// freely choose the names of the physics layers.
        /// </summary>
        [Tooltip("The layer mask used while raytracing to determine which hits to ignore. There are many scenarios where you have objects that should collide with everything in the scene, but not cause shadows. You should consider creating a physics layer (click on 'Layer: Default' at the top of the Game Object Inspector -> Add Layer...) and naming it 'Collision'. You can then remove this layer from the list so it will be ignored by the raytracer. The rest of the scene will still have regular collisions with it. You can also do the opposite by creating a physics layer called 'Lighting' and disabling regular collisions with other colliders (in Edit -> Project Settings -> Physics), but leaving the layer checked in this list. Now you have a special shadow casting collision that nothing else can touch or interact with. These were just two example names, you can freely choose the names of the physics layers.")]
        public LayerMask raytraceLayers = ~0;

        /// <summary>
        /// The desired pixel density (e.g. 128 for 128x128 per meter squared). This lighting system
        /// does not require "power of two" textures. You may have heard this term before because
        /// graphics cards can render textures in such sizes much faster. This system relies on
        /// binary data on the GPU using compute buffers and it's quite different. Without going
        /// into too much detail, this simply means that we can choose any texture size. An
        /// intelligent algorithm calculates the surface area of the meshes and determines exactly
        /// how many pixels are needed to cover them evenly with shadow pixels, regardless of the
        /// ray tracing resolution (unless it exceeds that maximum ray tracing resolution, of
        /// course, then those shadow pixels will start to increase in size). Here you can set how
        /// many pixels should cover a square meter. It can result in a 47x47 texture or 328x328,
        /// exactly the amount needed to cover all polygons with the same amount of shadow pixels.
        /// Higher details require more VRAM (exponentially)!
        /// </summary>
        [Tooltip("The desired pixel density (e.g. 128 for 128x128 per meter squared). This lighting system does not require \"power of two\" textures. You may have heard this term before because graphics cards can render textures in such sizes much faster. This system relies on binary data on the GPU using compute buffers and it's quite different. Without going into too much detail, this simply means that we can choose any texture size. An intelligent algorithm calculates the surface area of the meshes and determines exactly how many pixels are needed to cover them evenly with shadow pixels, regardless of the ray tracing resolution (unless it exceeds that maximum ray tracing resolution, of course, then those shadow pixels will start to increase in size). Here you can set how many pixels should cover a square meter. It can result in a 47x47 texture or 328x328, exactly the amount needed to cover all polygons with the same amount of shadow pixels. Higher details require more VRAM (exponentially)!")]
        public int pixelDensityPerSquareMeter = 128;

        /// <summary>The collection of raycasted mesh renderers in the scene.</summary>
        [SerializeField]
        [HideInInspector]
        [FormerlySerializedAs("lightmaps")]
        internal List<RaycastedMeshRenderer> raycastedMeshRenderers = new List<RaycastedMeshRenderer>();

        /// <summary>The collection of raycasted <see cref="DynamicLight"/> sources in the scene.</summary>
        [SerializeField]
        [HideInInspector]
        internal List<RaycastedDynamicLight> raycastedDynamicLights = new List<RaycastedDynamicLight>();

        /// <summary>The memory size in bytes of the <see cref="ShaderDynamicLight"/> struct.</summary>
        private int dynamicLightStride;
        private List<DynamicLight> sceneDynamicLights;
        private List<DynamicLight> sceneRealtimeLights;
        private bool sceneDynamicLightsAddedDirty = false;

        private List<DynamicLight> activeDynamicLights;
        private List<DynamicLight> activeRealtimeLights;
        private ShaderDynamicLight[] shaderDynamicLights;
        private ComputeBuffer dynamicLightsBuffer;

        private Vector3 lastCameraMetricGridPosition = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
        [System.NonSerialized]
        private bool isInitialized = false;

#if UNITY_EDITOR

        /// <summary>
        /// Called by <see cref="DynamicLightingTracer"/> to properly free up the compute buffers
        /// before clearing the lightmaps collection. Then deletes all lightmap files from disk.
        /// This method call must be followed up by a call to <see cref="Reload"/>
        /// </summary>
        internal void EditorDeleteLightmaps()
        {
            // free up the compute buffers.
            Cleanup();

            // clear the lightmap scene data.
            raycastedMeshRenderers.Clear();

            // delete the lightmap files from disk.
            Utilities.DeleteLightmapData("Lightmap");
            Utilities.DeleteLightmapData("Triangles");

            // make sure the user gets prompted to save their scene.
            UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        }

        [UnityEditor.MenuItem("Dynamic Lighting/Delete Scene Lightmaps", false, 20)]
        private static void EditorDeleteLightmapsNow()
        {
            Instance.EditorDeleteLightmaps();
            Instance.Reload();
        }

#endif

        private void OnEnable()
        {
            Initialize();
        }

        private void OnDisable()
        {
            Cleanup();
        }

        /// <summary>Immediately reloads the lighting.</summary>
        public void Reload()
        {
            Cleanup();
            Initialize(true);
        }

        /// <summary>Gets whether the specified light source has been raycasted in the scene.</summary>
        /// <param name="light">The dynamic light to check.</param>
        /// <returns>True when the dynamic light has been raycasted else false.</returns>
        internal bool IsRaycastedDynamicLight(DynamicLight light)
        {
            var raycastedDynamicLightsCount = raycastedDynamicLights.Count;
            for (int i = 0; i < raycastedDynamicLightsCount; i++)
                if (raycastedDynamicLights[i].light == light)
                    return true;
            return false;
        }

        /// <summary>Finds all of the dynamic lights in the scene that are not realtime.</summary>
        /// <returns>The collection of dynamic lights in the scene.</returns>
        internal static List<DynamicLight> FindDynamicLightsInScene()
        {
            var dynamicPointLights = new List<DynamicLight>(FindObjectsOfType<DynamicLight>());

            // remove all of the realtime lights from our collection.
            var dynamicPointLightsCount = dynamicPointLights.Count;
            for (int i = dynamicPointLightsCount; i-- > 0;)
                if (dynamicPointLights[i].realtime)
                    dynamicPointLights.RemoveAt(i);

            return dynamicPointLights;
        }

        private void Initialize(bool reload = false)
        {
            // always immediately force an update in case we are budgeting.
            lastCameraMetricGridPosition = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

            // only execute the rest if not initialized yet.
            if (isInitialized) return;
            isInitialized = true;

            dynamicLightStride = System.Runtime.InteropServices.Marshal.SizeOf(typeof(ShaderDynamicLight));

            // prepare to store dynamic lights that will register themselves to us.
            sceneDynamicLights = new List<DynamicLight>(dynamicLightBudget);
            sceneRealtimeLights = new List<DynamicLight>(realtimeLightBudget);

            if (reload)
            {
                // manually register all lights - this is used after raytracing.
                sceneDynamicLights = new List<DynamicLight>(FindObjectsOfType<DynamicLight>());
                sceneDynamicLightsAddedDirty = true;
            }

            // allocate the required arrays and buffers according to our budget.
            activeDynamicLights = new List<DynamicLight>(dynamicLightBudget);
            activeRealtimeLights = new List<DynamicLight>(realtimeLightBudget);
            shaderDynamicLights = new ShaderDynamicLight[dynamicLightBudget + realtimeLightBudget];
            dynamicLightsBuffer = new ComputeBuffer(shaderDynamicLights.Length, dynamicLightStride, ComputeBufferType.Default);
            Shader.SetGlobalBuffer("dynamic_lights", dynamicLightsBuffer);
            Shader.SetGlobalInt("dynamic_lights_count", 0);

            // prepare the scene for dynamic lighting.
            var raycastedMeshRenderersCount = raycastedMeshRenderers.Count;
            for (int i = 0; i < raycastedMeshRenderersCount; i++)
            {
                // for every game object that requires a lightmap:
                var lightmap = raycastedMeshRenderers[i];

                // make sure the scene reference is still valid.
                var meshRenderer = lightmap.renderer;
                if (!meshRenderer) continue;

                // fetch the active material on the mesh renderer.
                var materialPropertyBlock = new MaterialPropertyBlock();

                // play nice with other scripts.
                if (meshRenderer.HasPropertyBlock())
                    meshRenderer.GetPropertyBlock(materialPropertyBlock);

                // assign the lightmap data to the material property block.
                if (Utilities.ReadLightmapData(lightmap.identifier, "Lightmap", out uint[] pixels))
                {
                    lightmap.buffer = new ComputeBuffer(pixels.Length, 4);
                    lightmap.buffer.SetData(pixels);
                    materialPropertyBlock.SetBuffer("lightmap", lightmap.buffer);
                    materialPropertyBlock.SetInt("lightmap_resolution", lightmap.resolution);
                    meshRenderer.SetPropertyBlock(materialPropertyBlock);
                }
                else Debug.LogError("Unable to read the lightmap " + lightmap.identifier + " data file!");

                // assign the dynamic triangles data to the material property block.
                if (Utilities.ReadLightmapData(lightmap.identifier, "Triangles", out uint[] triangles))
                {
                    lightmap.trianglebuffer = new ComputeBuffer(triangles.Length, 4);
                    lightmap.trianglebuffer.SetData(triangles);
                    materialPropertyBlock.SetBuffer("dynamic_triangles", lightmap.trianglebuffer);
                    meshRenderer.SetPropertyBlock(materialPropertyBlock);
                }
                else Debug.LogError("Unable to read the triangles " + lightmap.identifier + " data file!");
            }
        }

        private void Cleanup()
        {
            if (!isInitialized) return;
            isInitialized = false;

            if (dynamicLightsBuffer != null && dynamicLightsBuffer.IsValid())
            {
                dynamicLightsBuffer.Release();
                dynamicLightsBuffer = null;
            }

            var raycastedMeshRenderersCount = raycastedMeshRenderers.Count;
            for (int i = 0; i < raycastedMeshRenderersCount; i++)
            {
                var raycastedMeshRenderer = raycastedMeshRenderers[i];
                if (raycastedMeshRenderer.buffer != null && raycastedMeshRenderer.buffer.IsValid())
                {
                    raycastedMeshRenderer.buffer.Release();
                    raycastedMeshRenderer.buffer = null;
                }
                if (raycastedMeshRenderer.trianglebuffer != null && raycastedMeshRenderer.trianglebuffer.IsValid())
                {
                    raycastedMeshRenderer.trianglebuffer.Release();
                    raycastedMeshRenderer.trianglebuffer = null;
                }

                // make sure the scene reference is still valid.
                var meshRenderer = raycastedMeshRenderer.renderer;
                if (!meshRenderer) continue;

                // fetch the active material on the mesh renderer.
                var materialPropertyBlock = new MaterialPropertyBlock();

                // play nice with other scripts.
                if (meshRenderer.HasPropertyBlock())
                    meshRenderer.GetPropertyBlock(materialPropertyBlock);

                // remove the lightmap data from the material property block.
                materialPropertyBlock.SetBuffer("lightmap", (ComputeBuffer)null);
                materialPropertyBlock.SetBuffer("dynamic_triangles", (ComputeBuffer)null);
                materialPropertyBlock.SetInt("lightmap_resolution", 0);
                meshRenderer.SetPropertyBlock(materialPropertyBlock);
            }

            sceneDynamicLights = null;
            sceneRealtimeLights = null;
            activeDynamicLights = null;
            activeRealtimeLights = null;
        }

        internal void RegisterDynamicLight(DynamicLight light)
        {
            Initialize();
            sceneDynamicLights.Add(light);
            sceneDynamicLightsAddedDirty = true;

            lightRegistered?.Invoke(this, new DynamicLightRegisteredEventArgs(light));
        }

        internal void UnregisterDynamicLight(DynamicLight light)
        {
            if (sceneDynamicLights != null)
            {
                sceneDynamicLights.Remove(light);
                sceneRealtimeLights.Remove(light);
                activeDynamicLights.Remove(light);
                activeRealtimeLights.Remove(light);

                lightUnregistered?.Invoke(this, new DynamicLightUnregisteredEventArgs(light));
            }
        }

        /// <summary>
        /// Whenever the dynamic or realtime light budgets change we must update the shader buffer.
        /// </summary>
        private void ReallocateShaderLightBuffer()
        {
            if (Application.isPlaying)
                Debug.LogWarning("Reallocation of dynamic lighting shader buffers on the graphics card due to a light budget change (slow).");

            // properly release any old buffer.
            if (dynamicLightsBuffer != null && dynamicLightsBuffer.IsValid())
                dynamicLightsBuffer.Release();

            var totalLightBudget = dynamicLightBudget + realtimeLightBudget;
            shaderDynamicLights = new ShaderDynamicLight[totalLightBudget];
            dynamicLightsBuffer = new ComputeBuffer(shaderDynamicLights.Length, dynamicLightStride, ComputeBufferType.Default);
            Shader.SetGlobalBuffer("dynamic_lights", dynamicLightsBuffer);
        }

        /// <summary>This handles the CPU side lighting effects.</summary>
        private void Update()
        {
            var camera = Camera.main;

#if UNITY_EDITOR
            // editor scene view support.
            if (!Application.isPlaying)
            {
                camera = Utilities.GetSceneViewCamera();
            }
            else
            {
                Debug.Assert(camera != null, "Could not find a camera that is tagged \"MainCamera\" for lighting calculations.");
            }

            // respect the scene view lighting toggle.
            {
                var sceneView = UnityEditor.SceneView.lastActiveSceneView;
                if (sceneView)
                {
                    var sceneLighting = sceneView.sceneLighting;
                    var shaderUnlit = Shader.IsKeywordEnabled("DYNAMIC_LIGHTING_UNLIT");

                    if (sceneLighting && shaderUnlit)
                        Shader.DisableKeyword("DYNAMIC_LIGHTING_UNLIT");
                    else if (!sceneLighting && !shaderUnlit)
                        Shader.EnableKeyword("DYNAMIC_LIGHTING_UNLIT");
                }
            }
#endif

            // if the budget changed we must recreate the shader buffers.
            var totalLightBudget = dynamicLightBudget + realtimeLightBudget;
            if (totalLightBudget == 0) return; // sanity check.
            if (shaderDynamicLights.Length != totalLightBudget)
                ReallocateShaderLightBuffer();

            // if a dynamic light got added to the scene:
            if (sceneDynamicLightsAddedDirty)
            {
                sceneDynamicLightsAddedDirty = false;

                // move all of the realtime lights into a separate list.
                var sceneDynamicLightsCount1 = sceneDynamicLights.Count;
                for (int i = sceneDynamicLightsCount1; i-- > 0;)
                {
                    var light = sceneDynamicLights[i];
                    if (light.realtime)
                    {
                        sceneDynamicLights.RemoveAt(i);
                        sceneRealtimeLights.Add(light);
                    }
                }
            }

            // calculate the camera frustum planes.
            Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(camera);

            // if we exceed the dynamic light budget then whenever the camera moves more than a
            // meter in the scene we sort all dynamic lights by distance from the camera and the
            // closest lights will appear first in the lists.
            var cameraPosition = camera.transform.position;
            //if (sceneDynamicLights.Count > dynamicLightBudget && Vector3.Distance(lastCameraMetricGridPosition, cameraPosition) > 1f)
            //{
            //    lastCameraMetricGridPosition = cameraPosition;
            //    SortSceneDynamicLights(lastCameraMetricGridPosition);
            //}

            // if we exceed the realtime light budget we sort the realtime lights by distance every
            // frame, as we will assume they are moving around.
            if (sceneRealtimeLights.Count > realtimeLightBudget)
            {
                SortSceneRealtimeLights(cameraPosition);
            }

            /*

            // clear as many active lights as possible.

            var activeDynamicLightsCount = activeDynamicLights.Count;
            for (int i = activeDynamicLightsCount; i-- > 0;)
                activeDynamicLights.RemoveAt(i);

            var activeRealtimeLightsCount = activeRealtimeLights.Count;
            for (int i = activeRealtimeLightsCount; i-- > 0;)
                activeRealtimeLights.RemoveAt(i);
            */
            // fill the active lights back up with the closest lights.

            activeDynamicLights.Clear();
            activeRealtimeLights.Clear();

            var raycastedDynamicLightsCount = raycastedDynamicLights.Count;
            for (int i = 0; i < raycastedDynamicLightsCount; i++)
            {
                // we must always update the fixed timestep calculator as it relies on Time.deltaTime.
                raycastedDynamicLights[i].light.cache.fixedTimestep.timePerStep = raycastedDynamicLights[i].light.lightEffectTimestepFrequency;
                raycastedDynamicLights[i].light.cache.fixedTimestep.Update();

                activeDynamicLights.Add(raycastedDynamicLights[i].light);
            }

            var sceneDynamicLightsCount = sceneDynamicLights.Count;
            for (int i = 0; i < sceneDynamicLightsCount; i++)
            {
                if (IsRaycastedDynamicLight(sceneDynamicLights[i]))
                    continue;

                // we must always update the fixed timestep calculator as it relies on Time.deltaTime.
                sceneDynamicLights[i].cache.fixedTimestep.timePerStep = sceneDynamicLights[i].lightEffectTimestepFrequency;
                sceneDynamicLights[i].cache.fixedTimestep.Update();

                if (activeDynamicLights.Count < dynamicLightBudget)
                {
#if UNITY_EDITOR    // optimization: only add lights that are within the camera frustum.
                    if (!Application.isPlaying || MathEx.CheckSphereIntersectsFrustum(frustumPlanes, sceneDynamicLights[i].transform.position, sceneDynamicLights[i].largestLightRadius))
#else
                    if (MathEx.CheckSphereIntersectsFrustum(frustumPlanes, sceneDynamicLights[i].transform.position, sceneDynamicLights[i].largestLightRadius))
#endif
                    {
                        activeDynamicLights.Add(sceneDynamicLights[i]);
                    }
                }
            }

            var sceneRealtimeLightsCount = sceneRealtimeLights.Count;
            for (int i = 0; i < sceneRealtimeLightsCount; i++)
            {
                // we must always update the fixed timestep calculator as it relies on Time.deltaTime.
                sceneRealtimeLights[i].cache.fixedTimestep.timePerStep = sceneRealtimeLights[i].lightEffectTimestepFrequency;
                sceneRealtimeLights[i].cache.fixedTimestep.Update();

                if (activeRealtimeLights.Count < realtimeLightBudget)
                {
#if UNITY_EDITOR    // optimization: only add lights that are within the camera frustum.
                    if (!Application.isPlaying || MathEx.CheckSphereIntersectsFrustum(frustumPlanes, sceneRealtimeLights[i].transform.position, sceneRealtimeLights[i].lightRadius))
#else
                    if (MathEx.CheckSphereIntersectsFrustum(frustumPlanes, sceneRealtimeLights[i].transform.position, sceneRealtimeLights[i].lightRadius))
#endif
                    {
                        activeRealtimeLights.Add(sceneRealtimeLights[i]);
                    }
                }
            }

            // write the active lights into the shader data.

            var idx = 0;
            var activeDynamicLightsCount = activeDynamicLights.Count;
            for (int i = 0; i < activeDynamicLightsCount; i++)
            {
                var light = activeDynamicLights[i];
                SetShaderDynamicLight(idx, light);
                UpdateLightEffects(idx, light);
                idx++;
            }

            var activeRealtimeLightsCount = activeRealtimeLights.Count;
            for (int i = 0; i < activeRealtimeLightsCount; i++)
            {
                var light = activeRealtimeLights[i];
                SetShaderDynamicLight(idx, light);
                UpdateLightEffects(idx, light);
                idx++;
            }

            // upload the active light data to the graphics card.
            if (dynamicLightsBuffer != null && dynamicLightsBuffer.IsValid())
                dynamicLightsBuffer.SetData(shaderDynamicLights, 0, 0, activeDynamicLightsCount + activeRealtimeLightsCount);
            Shader.SetGlobalInt("dynamic_lights_count", activeDynamicLightsCount + activeRealtimeLightsCount);

            // update the ambient lighting color.
            Shader.SetGlobalColor("dynamic_ambient_color", ambientColor);

            // update the shadow filtering algorithm.
            switch (QualitySettings.shadows)
            {
                case ShadowQuality.Disable:
                case ShadowQuality.HardOnly:
                    Shader.DisableKeyword("DYNAMIC_LIGHTING_SHADOW_SOFT");
                    Shader.EnableKeyword("DYNAMIC_LIGHTING_SHADOW_HARD");
                    break;

                case ShadowQuality.All:
                    Shader.EnableKeyword("DYNAMIC_LIGHTING_SHADOW_SOFT");
                    Shader.DisableKeyword("DYNAMIC_LIGHTING_SHADOW_HARD");
                    break;
            }
        }

        /// <summary>
        /// Sorts the scene dynamic light lists by the distance from the specified origin. The
        /// closests lights will appear first in the list.
        /// </summary>
        /// <param name="origin">The origin (usually the camera world position).</param>
        private void SortSceneDynamicLights(Vector3 origin)
        {
            sceneDynamicLights.Sort((a, b) => (origin - a.transform.position).sqrMagnitude
            .CompareTo((origin - b.transform.position).sqrMagnitude));
        }

        /// <summary>
        /// Sorts the scene realtime light lists by the distance from the specified origin. The
        /// closests lights will appear first in the list.
        /// </summary>
        /// <param name="origin">The origin (usually the camera world position).</param>
        private void SortSceneRealtimeLights(Vector3 origin)
        {
            sceneRealtimeLights.Sort((a, b) => (origin - a.transform.position).sqrMagnitude
            .CompareTo((origin - b.transform.position).sqrMagnitude));
        }

        private void SetShaderDynamicLight(int idx, DynamicLight light)
        {
            // the light intensity is set by the effects update step.
            shaderDynamicLights[idx].position = light.transform.position;
            shaderDynamicLights[idx].color = new Vector3(light.lightColor.r, light.lightColor.g, light.lightColor.b);
            shaderDynamicLights[idx].radiusSqr = light.lightRadius * light.lightRadius;
            shaderDynamicLights[idx].channel = light.lightChannel;

            shaderDynamicLights[idx].up = light.transform.up;
            shaderDynamicLights[idx].forward = light.transform.forward;
            shaderDynamicLights[idx].shimmerScale = light.lightShimmerScale;
            shaderDynamicLights[idx].shimmerModifier = light.lightShimmerModifier;

            // the volumetric intensity is set by the effects update step.
            shaderDynamicLights[idx].volumetricRadiusSqr = light.lightVolumetricRadius * light.lightVolumetricRadius;
            shaderDynamicLights[idx].volumetricThickness = light.lightVolumetricThickness;
            shaderDynamicLights[idx].volumetricVisibility = light.lightVolumetricVisibility <= 0f ? 0.00001f : light.lightVolumetricVisibility;

            switch (light.lightType)
            {
                case DynamicLightType.Spot:
                    shaderDynamicLights[idx].channel |= (uint)1 << 6; // spot light bit
                    shaderDynamicLights[idx].gpFloat1 = Mathf.Cos(light.lightCutoff * Mathf.Deg2Rad);
                    shaderDynamicLights[idx].gpFloat2 = Mathf.Cos(light.lightOuterCutoff * Mathf.Deg2Rad);
                    break;

                case DynamicLightType.Discoball:
                    shaderDynamicLights[idx].channel |= (uint)1 << 7; // discoball light bit
                    shaderDynamicLights[idx].gpFloat1 = Mathf.Cos(light.lightCutoff * Mathf.Deg2Rad);
                    shaderDynamicLights[idx].gpFloat2 = Mathf.Cos(light.lightOuterCutoff * Mathf.Deg2Rad);
                    break;

                case DynamicLightType.Wave:
                    shaderDynamicLights[idx].channel |= (uint)1 << 10; // wave light bit
                    shaderDynamicLights[idx].gpFloat1 = light.lightWaveSpeed;
                    shaderDynamicLights[idx].gpFloat2 = light.lightWaveFrequency;
                    break;

                case DynamicLightType.Interference:
                    shaderDynamicLights[idx].channel |= (uint)1 << 11; // interference light bit
                    shaderDynamicLights[idx].gpFloat1 = light.lightWaveSpeed;
                    shaderDynamicLights[idx].gpFloat2 = light.lightWaveFrequency;
                    break;

                case DynamicLightType.Rotor:
                    shaderDynamicLights[idx].channel |= (uint)1 << 12; // rotor light bit
                    shaderDynamicLights[idx].gpFloat1 = light.lightWaveSpeed;
                    shaderDynamicLights[idx].gpFloat2 = Mathf.Round(light.lightWaveFrequency);
                    shaderDynamicLights[idx].gpFloat3 = light.lightRotorCenter;
                    break;

                case DynamicLightType.Shock:
                    shaderDynamicLights[idx].channel |= (uint)1 << 13; // shock light bit
                    shaderDynamicLights[idx].gpFloat1 = light.lightWaveSpeed;
                    shaderDynamicLights[idx].gpFloat2 = light.lightWaveFrequency;
                    break;

                case DynamicLightType.Disco:
                    shaderDynamicLights[idx].channel |= (uint)1 << 14; // disco light bit
                    shaderDynamicLights[idx].gpFloat1 = light.lightWaveSpeed;
                    shaderDynamicLights[idx].gpFloat2 = Mathf.Round(light.lightWaveFrequency);
                    shaderDynamicLights[idx].gpFloat3 = light.lightDiscoVerticalSpeed;
                    break;
            }

            switch (light.lightShimmer)
            {
                case DynamicLightShimmer.Water:
                    shaderDynamicLights[idx].channel |= (uint)1 << 8; // water shimmer light bit
                    break;

                case DynamicLightShimmer.Random:
                    shaderDynamicLights[idx].channel |= (uint)1 << 9; // random shimmer light bit
                    break;
            }

            switch (light.lightVolumetricType)
            {
                case DynamicLightVolumetricType.Sphere:
                    shaderDynamicLights[idx].channel |= (uint)1 << 15; // volumetric light bit
                    break;
            }
        }

        private void UpdateLightEffects(int idx, DynamicLight light)
        {
            // continuous light effects:

            switch (light.lightEffect)
            {
                case DynamicLightEffect.Steady:
                    light.cache.intensity = 1.0f;
                    break;

                case DynamicLightEffect.Pulse:
                    light.cache.intensity = Mathf.Lerp(light.lightEffectPulseModifier, 1.0f, (1f + Mathf.Sin(Time.time * Mathf.PI * 2f * light.lightEffectPulseSpeed)) * 0.5f);
                    break;
            }

            // fixed timestep light effects:

            if (light.cache.fixedTimestep.pendingSteps > 0 || !light.cache.initialized)
            {
                light.cache.initialized = true;

                switch (light.lightEffect)
                {
                    case DynamicLightEffect.Random:
                        light.cache.intensity = Mathf.Lerp(light.lightEffectPulseModifier, 1.0f, Random.value);
                        break;

                    case DynamicLightEffect.Flicker:
                        var random = Random.value;
                        if (random < 0.5f)
                            light.cache.intensity = 0.0f;
                        else
                            light.cache.intensity = Mathf.Lerp(light.lightEffectPulseModifier, 1.0f, Random.value);
                        break;

                    case DynamicLightEffect.Strobe:
                        light.cache.strobeActive = !light.cache.strobeActive;
                        light.cache.intensity = light.cache.strobeActive ? 1.0f : light.lightEffectPulseModifier;
                        break;
                }
            }

            // assign the cached values to the shader lights.

            shaderDynamicLights[idx].intensity = light.lightIntensity * light.cache.intensity;
            shaderDynamicLights[idx].volumetricIntensity = light.lightVolumetricIntensity * light.cache.intensity;
        }

        private void OnDrawGizmos()
        {
#if UNITY_EDITOR
            // the scene view continuous preview toggle.
            var sceneView = UnityEditor.SceneView.lastActiveSceneView;
            if (sceneView && sceneView.sceneViewState.fxEnabled && sceneView.sceneViewState.alwaysRefresh)
            {
                // ensure continuous update calls.
                if (!Application.isPlaying)
                {
                    UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
                    UnityEditor.SceneView.RepaintAll();
                }
            }
#endif
        }

#if UNITY_EDITOR

        private static bool EditorEnsureUserSavedScene()
        {
            if (!Utilities.IsActiveSceneSavedToDisk)
            {
                UnityEditor.EditorUtility.DisplayDialog("Dynamic Lighting", "Please save your scene to disk before raytracing.", "Okay");
                return false;
            }
            return true;
        }

#endif

        /// <summary>Raytraces the current scene, calculating the shadows for all dynamic lights.</summary>
        /// <param name="maximumLightmapSize">
        /// The maximum size of the lightmap to be baked (defaults to 2048x2048).
        /// </param>
        public void Raytrace(int maximumLightmapSize = 2048)
        {
#if UNITY_EDITOR
            if (!EditorEnsureUserSavedScene()) return;
#endif
            var tracer = new DynamicLightingTracer();
            tracer.maximumLightmapSize = maximumLightmapSize;

            bool cancelled = false;
            tracer.cancelled += (s, e) => { cancelled = true; traceCancelled?.Invoke(this, null); };

            traceStarted?.Invoke(this, null);

            tracer.StartRaytracing();

            if (!cancelled)
                traceCompleted?.Invoke(this, null);
        }
    }
}