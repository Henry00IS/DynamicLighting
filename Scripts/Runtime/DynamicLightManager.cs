using System.Collections.Generic;
using UnityEngine;
using static AlpacaIT.DynamicLighting.MathEx;

namespace AlpacaIT.DynamicLighting
{
    [ExecuteInEditMode]
    public class DynamicLightManager : MonoBehaviour
    {
        public const int realtimeLightChannel = 32;

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

        /// <summary>The memory size in bytes of the <see cref="ShaderDynamicLight"/> struct.</summary>
        private int dynamicLightStride;
        private Lightmap[] lightmaps;
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

        private bool useContinuousPreview = false;

        [UnityEditor.MenuItem("Dynamic Lighting/Toggle Continuous Preview", false, 21)]
        private static void EditorToggleContinuousPreview()
        {
            Instance.useContinuousPreview = !Instance.useContinuousPreview;
        }

        [UnityEditor.MenuItem("Dynamic Lighting/Toggle Unlit Surfaces", false, 22)]
        private static void EditorToggleUnlitSurfaces()
        {
            if (Shader.IsKeywordEnabled("DYNAMIC_LIGHTING_UNLIT"))
                Shader.DisableKeyword("DYNAMIC_LIGHTING_UNLIT");
            else
                Shader.EnableKeyword("DYNAMIC_LIGHTING_UNLIT");
        }

        [UnityEditor.MenuItem("Dynamic Lighting/PayPal Donation", false, 41)]
        private static void EditorPayPalDonation()
        {
            Application.OpenURL("https://paypal.me/henrydejongh");
        }

        [UnityEditor.MenuItem("GameObject/Light/Dynamic Point Light", false, 40)]
        private static void EditorCreateDynamicPointLight()
        {
            EditorCreateDynamicLight("Dynamic Light");
        }

        [UnityEditor.MenuItem("GameObject/Light/Dynamic Spot Light", false, 40)]
        private static void EditorCreateDynamicSpotLight()
        {
            var light = EditorCreateDynamicLight("Dynamic Spot Light");
            light.lightType = DynamicLightType.Spot;
        }

        [UnityEditor.MenuItem("GameObject/Light/Dynamic Discoball Light", false, 40)]
        private static void EditorCreateDynamicDiscoballLight()
        {
            var light = EditorCreateDynamicLight("Dynamic Discoball Light");
            light.lightType = DynamicLightType.Discoball;
            light.lightCutoff = 12.5f;
            light.lightOuterCutoff = 14.0f;
        }

        /// <summary>Adds a new dynamic light game object to the scene.</summary>
        /// <param name="name">The name of the game object that will be created.</param>
        /// <returns>The dynamic light component.</returns>
        private static DynamicLight EditorCreateDynamicLight(string name)
        {
            GameObject go = new GameObject(name);
            UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Create " + name);

            // place the new game object as a child of the current selection in the editor.
            var parent = UnityEditor.Selection.activeTransform;
            if (parent)
            {
                // keep the game object transform identity.
                go.transform.SetParent(parent, false);
            }
            else
            {
                // move it in front of the current camera.
                var camera = EditorUtilities.GetSceneViewCamera();
                if (camera)
                {
                    go.transform.position = camera.transform.TransformPoint(Vector3.forward * 2f);
                }
            }

            // add the dynamic light component.
            var light = go.AddComponent<DynamicLight>();

            // make sure it's selected and unity editor will let the user rename the game object.
            UnityEditor.Selection.activeGameObject = go;
            return light;
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

        /// <summary>Finds all of the dynamic lights in the scene that are not realtime.</summary>
        /// <returns>The collection of dynamic lights in the scene.</returns>
        public static List<DynamicLight> FindDynamicLightsInScene()
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
            lightmaps = FindObjectsOfType<Lightmap>();
            for (int i = 0; i < lightmaps.Length; i++)
            {
                // for every game object that requires a lightmap:
                var lightmap = lightmaps[i];

                // fetch the active material on the mesh renderer.
                var meshRenderer = lightmap.GetComponent<MeshRenderer>();
                var materialPropertyBlock = new MaterialPropertyBlock();

                // play nice with other scripts.
                if (meshRenderer.HasPropertyBlock())
                    meshRenderer.GetPropertyBlock(materialPropertyBlock);

                // assign the lightmap data to the material property block.
                if (RuntimeUtilities.ReadLightmapData(lightmap.identifier, out uint[] pixels))
                {
                    lightmap.buffer = new ComputeBuffer(pixels.Length, 4);
                    lightmap.buffer.SetData(pixels);
                    materialPropertyBlock.SetBuffer("lightmap", lightmap.buffer);
                    materialPropertyBlock.SetInt("lightmap_resolution", lightmap.resolution);
                    meshRenderer.SetPropertyBlock(materialPropertyBlock);
                }
                else Debug.LogError("Unable to read the lightmap " + lightmap.identifier + " data file!");
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

            for (int i = 0; i < lightmaps.Length; i++)
            {
                var lightmap = lightmaps[i];
                if (lightmap.buffer != null && lightmap.buffer.IsValid())
                {
                    lightmap.buffer.Release();
                    lightmap.buffer = null;
                }
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
        }

        internal void UnregisterDynamicLight(DynamicLight light)
        {
            if (sceneDynamicLights != null)
            {
                sceneDynamicLights.Remove(light);
                sceneRealtimeLights.Remove(light);
                activeDynamicLights.Remove(light);
                activeDynamicLights.Remove(light);
            }
        }

        /// <summary>
        /// Whenever the dynamic or realtime light budgets change we must update the shader buffer.
        /// </summary>
        private void ReallocateShaderLightBuffer()
        {
            Debug.Log("REALLOC");

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
                camera = EditorUtilities.GetSceneViewCamera();
            }
            else
            {
                Debug.Assert(camera != null, "Could not find a camera that is tagged \"MainCamera\" for lighting calculations.");
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
            if (sceneDynamicLights.Count > dynamicLightBudget && Vector3.Distance(lastCameraMetricGridPosition, cameraPosition) > 1f)
            {
                lastCameraMetricGridPosition = cameraPosition;
                SortSceneDynamicLights(lastCameraMetricGridPosition);
            }

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

            var sceneDynamicLightsCount = sceneDynamicLights.Count;
            for (int i = 0; i < sceneDynamicLightsCount; i++)
            {
                // we must always update the fixed timestep calculator as it relies on Time.deltaTime.
                sceneDynamicLights[i].cache.fixedTimestep.timePerStep = sceneDynamicLights[i].lightEffectTimestepFrequency;
                sceneDynamicLights[i].cache.fixedTimestep.Update();

                if (activeDynamicLights.Count < dynamicLightBudget)
                {
#if UNITY_EDITOR    // optimization: only add lights that are within the camera frustum.
                    if (!Application.isPlaying || MathEx.CheckSphereIntersectsFrustum(frustumPlanes, sceneDynamicLights[i].transform.position, sceneDynamicLights[i].lightRadius))
#else
                    if (MathEx.CheckSphereIntersectsFrustum(frustumPlanes, sceneDynamicLights[i].transform.position, sceneDynamicLights[i].lightRadius))
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
                dynamicLightsBuffer.SetData(shaderDynamicLights);
            Shader.SetGlobalInt("dynamic_lights_count", activeDynamicLightsCount + activeRealtimeLightsCount);
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
            shaderDynamicLights[idx].position = light.transform.position;
            shaderDynamicLights[idx].color = new Vector3(light.lightColor.r, light.lightColor.g, light.lightColor.b);
            // shaderDynamicLights[idx].intensity = light.lightIntensity;
            shaderDynamicLights[idx].radiusSqr = light.lightRadius * light.lightRadius;
            shaderDynamicLights[idx].channel = light.lightChannel;

            shaderDynamicLights[idx].channel &= ~((uint)1 << 6); // spot light bit
            shaderDynamicLights[idx].channel &= ~((uint)1 << 7); // discoball light bit
            shaderDynamicLights[idx].channel &= ~((uint)1 << 8); // water shimmer light bit

            switch (light.lightType)
            {
                case DynamicLightType.Spot:
                    shaderDynamicLights[idx].channel |= (uint)1 << 6; // spot light bit
                    break;

                case DynamicLightType.Discoball:
                    shaderDynamicLights[idx].channel |= (uint)1 << 7; // discoball light bit
                    break;
            }

            if (light.lightWaterShimmer)
                shaderDynamicLights[idx].channel |= (uint)1 << 8; // water shimmer light bit

            shaderDynamicLights[idx].forward = light.transform.forward;
            shaderDynamicLights[idx].cutoff = Mathf.Cos(light.lightCutoff * Mathf.Deg2Rad);
            shaderDynamicLights[idx].outerCutoff = Mathf.Cos(light.lightOuterCutoff * Mathf.Deg2Rad);
        }

        private void UpdateLightEffects(int idx, DynamicLight light)
        {
            // continuous light effects:

            switch (light.lightEffect)
            {
                case DynamicLightEffect.Steady:
                    light.cache.intensity = light.lightIntensity;
                    break;

                case DynamicLightEffect.Pulse:
                    light.cache.intensity = light.lightIntensity * Mathf.Lerp(light.lightEffectPulseModifier, 1.0f, (1f + Mathf.Sin(Time.time * Mathf.PI * 2f * light.lightEffectPulseSpeed)) * 0.5f);
                    break;
            }

            // fixed timestep light effects:

            if (light.cache.fixedTimestep.pendingSteps > 0 || !light.cache.initialized)
            {
                light.cache.initialized = true;

                switch (light.lightEffect)
                {
                    case DynamicLightEffect.Flicker:
                        light.cache.intensity = light.lightIntensity * Random.value;
                        break;

                    case DynamicLightEffect.Strobe:
                        light.cache.strobeActive = !light.cache.strobeActive;
                        light.cache.intensity = light.cache.strobeActive ? light.lightIntensity : 0f;
                        break;
                }
            }

            // assign the cached values to the shader lights.

            shaderDynamicLights[idx].intensity = light.cache.intensity;
        }

        private void OnDrawGizmos()
        {
#if UNITY_EDITOR
            if (useContinuousPreview)
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
    }
}