using AlpacaIT.DynamicLighting.Internal;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

// shoutouts to dazombiekiller!
[module: SkipLocalsInit]

namespace AlpacaIT.DynamicLighting
{
    /// <summary>
    /// The <see cref="DynamicLightManager"/> is responsible for updating all of the <see
    /// cref="DynamicLight"/> sources. It handles raytracing the scene, loading data from disk,
    /// updating shaders, acceleration structures, light effects, light cookies, volumetric fog
    /// post-processing, real-time shadows, material previews in the editor and much more. Use the
    /// static property <see cref="Instance"/> to access (and create) the singleton <see
    /// cref="DynamicLightManager"/> in the current scene.
    /// </summary>
    [ExecuteInEditMode]
    public partial class DynamicLightManager : MonoBehaviour
    {
        /// <summary>
        /// Called when a <see cref="DynamicLight"/> gets registered (i.e. enabled).
        /// <para>
        /// Warning: You must unsubscribe static events to prevent memory leaks during scene reloads
        ///          (unless your code is wholly static). <br/>
        ///          Consider using an [InitializeOnLoadMethod] or [RuntimeInitializeOnLoadMethod].
        /// </para>
        /// </summary>
        public static event EventHandler<DynamicLightRegisteredEventArgs> lightRegistered;

        /// <summary>
        /// Called when a <see cref="DynamicLight"/> gets unregistered (i.e. disabled).
        /// <para>
        /// Warning: You must unsubscribe static events to prevent memory leaks during scene reloads
        ///          (unless your code is wholly static). <br/>
        ///          Consider using an [InitializeOnLoadMethod] or [RuntimeInitializeOnLoadMethod].
        /// </para>
        /// </summary>
        public static event EventHandler<DynamicLightUnregisteredEventArgs> lightUnregistered;

        /// <summary>
        /// Called when computing shadows for the current scene has started.
        /// <para>
        /// Warning: You must unsubscribe static events to prevent memory leaks during scene reloads
        ///          (unless your code is wholly static). <br/>
        ///          Consider using an [InitializeOnLoadMethod] or [RuntimeInitializeOnLoadMethod].
        /// </para>
        /// </summary>
        public static event EventHandler<DynamicLightingTraceStartedEventArgs> traceStarted;

        /// <summary>
        /// Called when computing shadows for the current scene has been cancelled.
        /// <para>
        /// Warning: You must unsubscribe static events to prevent memory leaks during scene reloads
        ///          (unless your code is wholly static). <br/>
        ///          Consider using an [InitializeOnLoadMethod] or [RuntimeInitializeOnLoadMethod].
        /// </para>
        /// </summary>
        public static event EventHandler<DynamicLightingTraceCancelledEventArgs> traceCancelled;

        /// <summary>
        /// Called when computing shadows for the current scene has completed.
        /// <para>
        /// Warning: You must unsubscribe static events to prevent memory leaks during scene reloads
        ///          (unless your code is wholly static). <br/>
        ///          Consider using an [InitializeOnLoadMethod] or [RuntimeInitializeOnLoadMethod].
        /// </para>
        /// </summary>
        public static event EventHandler<DynamicLightingTraceCompletedEventArgs> traceCompleted;

        /// <summary>
        /// Called when the <see cref="DynamicLightManager"/> finishes initializing.
        /// <para>
        /// Warning: You must unsubscribe static events to prevent memory leaks during scene reloads
        ///          (unless your code is wholly static). <br/>
        ///          Consider using an [InitializeOnLoadMethod] or [RuntimeInitializeOnLoadMethod].
        /// </para>
        /// </summary>
        public static event EventHandler<DynamicLightingInitializedEventArgs> initialized;

        /// <summary>
        /// Called when the <see cref="DynamicLightManager"/> finishes uninitializing.
        /// <para>
        /// Warning: You must unsubscribe static events to prevent memory leaks during scene reloads
        ///          (unless your code is wholly static). <br/>
        ///          Consider using an [InitializeOnLoadMethod] or [RuntimeInitializeOnLoadMethod].
        /// </para>
        /// </summary>
        public static event EventHandler<DynamicLightingUninitializedEventArgs> uninitialized;

        /// <summary>
        /// Called when the <see cref="DynamicLightManager"/> begins updating.
        /// <para>
        /// Warning: You must unsubscribe static events to prevent memory leaks during scene reloads
        ///          (unless your code is wholly static). <br/>
        ///          Consider using an [InitializeOnLoadMethod] or [RuntimeInitializeOnLoadMethod].
        /// </para>
        /// </summary>
        public static event EventHandler<DynamicLightingPreUpdateEventArgs> preUpdate;

        /// <summary>
        /// Called when the <see cref="DynamicLightManager"/> finishes updating.
        /// <para>
        /// Warning: You must unsubscribe static events to prevent memory leaks during scene reloads
        ///          (unless your code is wholly static). <br/>
        ///          Consider using an [InitializeOnLoadMethod] or [RuntimeInitializeOnLoadMethod].
        /// </para>
        /// </summary>
        public static event EventHandler<DynamicLightingPostUpdateEventArgs> postUpdate;

        /// <summary>
        /// Called when the <see cref="DynamicLightManager"/> deletes the lighting data in the scene.
        /// <para>
        /// Warning: You must unsubscribe static events to prevent memory leaks during scene reloads
        ///          (unless your code is wholly static). <br/>
        ///          Consider using an [InitializeOnLoadMethod] or [RuntimeInitializeOnLoadMethod].
        /// </para>
        /// </summary>
        public static event EventHandler<DynamicLightingDeletedEventArgs> lightingDeleted;

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

        /// <summary>The version number used during the tracing of the scene (legacy: 0).</summary>
        [SerializeField]
        [HideInInspector]
        internal int version;

        /// <summary>Whether bounce lighting was used during the tracing of the scene.</summary>
        [SerializeField]
        [HideInInspector]
        internal bool activateBounceLightingInCurrentScene;

        /// <summary>
        /// The settings of the dynamic light manager can be shared across several scenes by
        /// assigning a template here. The values are copied from the template to this instance
        /// during initialization (typically when the scene is loaded). In the editor, changes to
        /// the template are automatically applied to this instance.
        /// </summary>
        [Tooltip("The settings of the dynamic light manager can be shared across several scenes by assigning a template here. The values are copied from the template to this instance during initialization (typically when the scene is loaded). In the editor, changes to the template are automatically applied to this instance.")]
        [FormerlySerializedAs("lightingSettings")]
        public DynamicLightingSettings settingsTemplate;

        /// <summary>
        /// The ambient lighting color is added to the whole scene, thus making it look like there
        /// is always some scattered light, even when there is no direct light source. This prevents
        /// absolute black, dark patches from appearing in the scene that are impossible to see
        /// through (unless this is desired). This color should be very dark to achieve the best
        /// effect. You can make use of the alpha color to finetune the brightness.
        /// </summary>
        [Tooltip("The ambient lighting color is added to the whole scene, thus making it look like there is always some scattered light, even when there is no direct light source. This prevents absolute black, dark patches from appearing in the scene that are impossible to see through (unless this is desired). This color should be very dark to achieve the best effect. You can make use of the alpha color to finetune the brightness.")]
        public Color ambientColor = new Color(1.0f, 1.0f, 1.0f, 0.1254902f);

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
        /// The layer mask used for real-time shadows to discern which objects are shadow casters.
        /// In some cases, specific objects may require raytracing for baked lighting and visual
        /// appeal, yet they shouldn't contribute to real-time shadow casting (e.g. thin objects).
        /// </summary>
        [Tooltip("The layer mask used for real-time shadows to discern which objects are shadow casters. In some cases, specific objects may require raytracing for baked lighting and visual appeal, yet they shouldn't contribute to real-time shadow casting (e.g. thin objects).")]
        public LayerMask realtimeShadowLayers = ~(4 | 16 | 32);

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
        [Min(1)]
        public int pixelDensityPerSquareMeter = 128;

        /// <summary>
        /// The default compression level for bounce lighting data. Choosing a higher compression
        /// can reduce VRAM usage, but may result in reduced visual quality. For best results,
        /// adjust based on your VRAM availability and visual preferences.
        /// </summary>
        [Tooltip("The default compression level for bounce lighting data. Choosing a higher compression can reduce VRAM usage, but may result in reduced visual quality. For best results, adjust based on your VRAM availability and visual preferences.")]
        public DynamicBounceLightingDefaultCompressionMode bounceLightingCompression = DynamicBounceLightingDefaultCompressionMode.EightBitsPerPixel;

        /// <summary>
        /// The runtime quality for Dynamic Lighting in the scene. These options should be available
        /// in your in-game settings menu to allow players to adjust lighting quality based on their
        /// hardware performance.
        /// <para>Tip: Consider using the <see cref="initialized"/> event to set this field.</para>
        /// </summary>
        [HideInInspector]
        public DynamicLightingRuntimeQuality runtimeQuality = DynamicLightingRuntimeQuality.Medium;

        /// <summary>
        /// Internally used to detect changes to <see cref="runtimeQuality"/> to prevent unnecessary
        /// calls into the Unity Shader API. This is deliberately set to an invalid value and
        /// properly set in the shaders partial class and after a successful update.
        /// </summary>
        private DynamicLightingRuntimeQuality activeRuntimeQuality = (DynamicLightingRuntimeQuality)255;

        /// <summary>The collection of raycasted mesh renderers in the scene.</summary>
        [SerializeField]
        [HideInInspector]
        [FormerlySerializedAs("lightmaps")]
        internal List<RaycastedMeshRenderer> raycastedMeshRenderers = new List<RaycastedMeshRenderer>();

        /// <summary>The collection of raycasted <see cref="DynamicLight"/> sources in the scene.</summary>
        [SerializeField]
        [HideInInspector]
        internal List<RaycastedDynamicLight> raycastedDynamicLights = new List<RaycastedDynamicLight>();

        /// <summary>The compressed raycasted scene data such as dynamic triangles and the BVH.</summary>
        [SerializeField]
        [HideInInspector]
        internal RaycastedScene raycastedScene;

        /// <summary>The memory size in bytes of the <see cref="ShaderDynamicLight"/> struct.</summary>
        private int dynamicLightStride;
        private List<DynamicLight> sceneRealtimeLights;

        private List<DynamicLight> activeRealtimeLights;
        private ShaderDynamicLight[] shaderDynamicLights;
        private ComputeBuffer dynamicLightsBuffer;

        /// <summary>The memory size in bytes of the <see cref="BvhLightNode"/> struct.</summary>
        private int dynamicLightsBvhNodeStride;
        private ComputeBuffer dynamicLightsBvhBuffer;

        /// <summary>Stores the 6 camera frustum planes, for the non-alloc version of <see cref="GeometryUtility.CalculateFrustumPlanes"/>.</summary>
        private Plane[] cameraFrustumPlanes = new Plane[6];

        /// <summary>Stores the time since the beginning of the frame (<see cref="Time.time"/>).</summary>
        private float timeTime;

        /// <summary>Stores the color space of the project at initialization.</summary>
        internal static ColorSpace colorSpace = ColorSpace.Linear;

        /// <summary>
        /// Cached <see cref="DynamicLightingPreUpdateEventArgs"/> to prevent making new instances.
        /// </summary>
        private DynamicLightingPreUpdateEventArgs dynamicLightingPreUpdateEventArgs;

        /// <summary>
        /// Cached <see cref="DynamicLightingPostUpdateEventArgs"/> to prevent making new instances.
        /// </summary>
        private DynamicLightingPostUpdateEventArgs dynamicLightingPostUpdateEventArgs;

        [System.NonSerialized]
        private bool isInitialized = false;

#if UNITY_EDITOR

        /// <summary>
        /// Checking whether meshes have been edited is slow, thus we must iterate in smaller
        /// batches. This is the offset of the last batch.
        /// </summary>
        private int editorMeshEditDetectionOffset = 0;

        /// <summary>
        /// Remembers whether <see cref="Application.isPlaying"/> to prevent native calls.
        /// </summary>
        internal bool editorIsPlaying = false;

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
            raycastedDynamicLights.Clear();

            // delete the lightmap files from disk.
            if (version == 0)
            {
                Utilities.DeleteLegacyLightmapData("Lightmap"); // legacy.
                Utilities.DeleteLegacyLightmapData("Triangles"); // legacy.
                Utilities.DeleteLegacyLightmapData("DynamicLightsBvh"); // legacy.
            }
            if (version == 2)
            {
                Utilities.DeleteLegacyLightmapData("DynamicLighting2-"); // legacy.
                Utilities.DeleteLegacyLightmapData("DynamicLightingBvh2-"); // legacy.
            }

            raycastedScene = null;
            Utilities.DeleteRaycastedScene();

            if (!editorIsPlaying)
            {
                // make sure the user gets prompted to save their scene.
                UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
            }

            // callback for third-party developers.
            lightingDeleted?.Invoke(this, new DynamicLightingDeletedEventArgs(this));
        }

        [UnityEditor.MenuItem("Dynamic Lighting/Delete Scene Lightmaps", false, 60)]
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

        private void Start()
        {
            // the editor will only have batched meshes upon calling the start() callback.
            // repair the scene for dynamic lighting on meshes with static batching.
            var raycastedMeshRenderersCount = raycastedMeshRenderers.Count;
            for (int i = 0; i < raycastedMeshRenderersCount; i++)
            {
                // for every game object that requires a lightmap:
                var lightmap = raycastedMeshRenderers[i];

                // make sure the scene reference is still valid.
                var meshRenderer = lightmap.renderer;
                if (!meshRenderer) continue;

                // the unity_lightmapst vector goes missing with static batching enabled as they
                // decided to bake the uv1 coordinates, this code is used to undo that operation.
                if (meshRenderer.isPartOfStaticBatch)
                {
                    // make sure that the reference has a mesh filter.
                    if (!meshRenderer.TryGetComponent<MeshFilter>(out var meshFilter))
                        continue;

                    // make sure that the mesh filter has a mesh assigned.
                    var mesh = meshFilter.sharedMesh;
                    if (!mesh) continue;

                    var lightmapScaleOffset = meshRenderer.lightmapScaleOffset;
                    if (lightmapScaleOffset.x == 0.0f) lightmapScaleOffset.x = 0.00001f;
                    if (lightmapScaleOffset.y == 0.0f) lightmapScaleOffset.y = 0.00001f;
                    lightmapScaleOffset = new Vector4(1.0f / lightmapScaleOffset.x * lightmap.resolution, 1.0f / lightmapScaleOffset.y * lightmap.resolution, lightmapScaleOffset.z, lightmapScaleOffset.w);

                    // assign the material property block to each submesh.
                    var hasPropertyBlock = meshRenderer.HasPropertyBlock(); // unity api oversight: cannot differentiate between submesh blocks and normal ones.
                    for (int j = 0; j < lightmap.activeSubMeshCount; j++)
                    {
                        var materialPropertyBlock = new MaterialPropertyBlock();

                        // play nice with other scripts.
                        if (hasPropertyBlock)
                            meshRenderer.GetPropertyBlock(materialPropertyBlock, j);

                        materialPropertyBlock.SetVector("dynamic_lighting_unity_LightmapST", lightmapScaleOffset);

                        meshRenderer.SetPropertyBlock(materialPropertyBlock, j);
                    }
                }
            }
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

        /// <summary>Finds all of the dynamic lights in the scene that are realtime.</summary>
        /// <returns>The collection of dynamic lights in the scene.</returns>
        internal List<DynamicLight> FindRealtimeLightsInScene()
        {
            var dynamicPointLights = new List<DynamicLight>(FindObjectsOfType<DynamicLight>());

            // remove all of the raycasted lights from our collection.
            var dynamicPointLightsCount = dynamicPointLights.Count;
            for (int i = dynamicPointLightsCount; i-- > 0;)
                if (IsRaycastedDynamicLight(dynamicPointLights[i]))
                    dynamicPointLights.RemoveAt(i);

            return dynamicPointLights;
        }

        private void Initialize(bool reload = false)
        {
            // only execute the rest if not initialized yet.
            if (isInitialized) return;
            isInitialized = true;

            // prepare event arguments for common callbacks.
            dynamicLightingPreUpdateEventArgs = new DynamicLightingPreUpdateEventArgs(this);
            dynamicLightingPostUpdateEventArgs = new DynamicLightingPostUpdateEventArgs(this);

            // check and store the color space mode of the project.
            colorSpace = QualitySettings.activeColorSpace;

            // apply the lighting settings template if set.
            if (settingsTemplate)
                settingsTemplate.Apply();
#if UNITY_EDITOR
            // check whether we are currently in play mode.
            editorIsPlaying = Application.isPlaying;

            // in the editor, subscribe to the camera rendering functions to repair preview cameras.
            Camera.onPreRender += EditorOnPreRenderCallback;
            Camera.onPostRender += EditorOnPostRenderCallback;
#endif
            // -> partial class DynamicLightManager.Shaders initialize.
            ShadersInitialize();

            dynamicLightStride = System.Runtime.InteropServices.Marshal.SizeOf(typeof(ShaderDynamicLight));
            dynamicLightsBvhNodeStride = System.Runtime.InteropServices.Marshal.SizeOf(typeof(BvhLightNode));

            // prepare to store realtime dynamic lights that will register themselves to us.
            sceneRealtimeLights = new List<DynamicLight>(realtimeLightBudget);

            if (reload)
            {
                // manually register all lights - this is used after raytracing.
                sceneRealtimeLights = FindRealtimeLightsInScene();
            }

            // allocate the required arrays and buffers according to our budget.
            activeRealtimeLights = new List<DynamicLight>(realtimeLightBudget);
            shaderDynamicLights = new ShaderDynamicLight[totalLightBudget];
            dynamicLightsBuffer = new ComputeBuffer(shaderDynamicLights.Length, dynamicLightStride, ComputeBufferType.Default);

            // -> partial class DynamicLightManager.PostProcessing initialize.
            PostProcessingInitialize();

            // -> partial class DynamicLightManager.ShadowCamera initialize.
            ShadowCameraInitialize();

            // -> partial class DynamicLightManager.LightCookie initialize.
            LightCookieInitialize();

            // -> partial class DynamicLightManager.LightPositions initialize.
            LightPositionsInitialize();

            ShadersSetGlobalDynamicLights(dynamicLightsBuffer);
            ShadersSetGlobalDynamicLightsCount(shadersLastDynamicLightsCount = 0);
            ShadersSetGlobalRealtimeLightsCount(shadersLastRealtimeLightsCount = 0);

            // unity api oversight: cannot get material count as it's a private method, so to
            // prevent allocations on the garbage collector we recycle a list here.
            var materialsScratchMemory = new List<Material>();

            // prepare the scene for dynamic lighting.
            var raycastedMeshRenderersCount = raycastedMeshRenderers.Count;
            for (int i = 0; i < raycastedMeshRenderersCount; i++)
            {
                // for every game object that requires a lightmap:
                var lightmap = raycastedMeshRenderers[i];

                // make sure the scene reference is still valid.
                var meshRenderer = lightmap.renderer;
                if (!meshRenderer) continue;

                // make sure that the reference has a mesh filter.
                if (!meshRenderer.TryGetComponent<MeshFilter>(out var meshFilter))
                    continue;

                // make sure that the mesh filter has a mesh assigned.
                var mesh = meshFilter.sharedMesh;
                if (!mesh) continue;
#if UNITY_EDITOR
                // prepare to detect mesh changes in edit mode.
                if (!editorIsPlaying)
                {
                    lightmap.lastMeshFilter = meshFilter;
                    lightmap.lastMeshHash = mesh.GetFastHash();
                }
#endif
                // assign the dynamic triangles data to the material property block.
                if (raycastedScene != null && raycastedScene.ReadDynamicTriangles(lightmap.identifier, out uint[] triangles))
                {
                    lightmap.buffer = new ComputeBuffer(triangles.Length, 4);
                    lightmap.buffer.SetData(triangles);

                    // assign the material property block to each submesh.
                    var hasPropertyBlock = meshRenderer.HasPropertyBlock(); // unity api oversight: cannot differentiate between submesh blocks and normal ones.
                    meshRenderer.GetSharedMaterials(materialsScratchMemory);
                    lightmap.activeSubMeshCount = Math.Min(mesh.subMeshCount - meshRenderer.subMeshStartIndex, materialsScratchMemory.Count);
                    for (int j = 0; j < lightmap.activeSubMeshCount; j++)
                    {
                        var materialPropertyBlock = new MaterialPropertyBlock();

                        // play nice with other scripts.
                        if (hasPropertyBlock)
                            meshRenderer.GetPropertyBlock(materialPropertyBlock, j);

                        materialPropertyBlock.SetBuffer("dynamic_triangles", lightmap.buffer);
                        materialPropertyBlock.SetInteger("lightmap_resolution", lightmap.resolution);
                        materialPropertyBlock.SetVector("dynamic_lighting_unity_LightmapST", new Vector4(lightmap.resolution, lightmap.resolution, 0f, 0f)); // identity.

                        // add a triangle offset to fix sv_primitiveid starting at zero.
                        materialPropertyBlock.SetInteger("triangle_index_submesh_offset", (int)(mesh.GetIndexStart(j) / 3));
                        meshRenderer.SetPropertyBlock(materialPropertyBlock, j);
                    }
                }
                else Debug.LogError("Unable to read the dynamic lighting data file! Probably because you upgraded from an older version. Please raytrace your scene again.");
            }

            // the scene may not have lights which would not be an error:
            if (raycastedDynamicLights.Count > 0)
            {
                // assign the dynamic lights bounding volume hierarchy to a global buffer.
                if (raycastedScene && raycastedScene.dynamicLightsBvh.Read(out uint[] bvhData))
                {
                    dynamicLightsBvhBuffer = new ComputeBuffer(bvhData.Length / 8, dynamicLightsBvhNodeStride, ComputeBufferType.Default);
                    dynamicLightsBvhBuffer.SetData(bvhData);
                    ShadersSetGlobalDynamicLightsBvh(dynamicLightsBvhBuffer);

                    // enable the bounding volume hierarchy processing in the shader.
                    shadersKeywordBvhEnabled = true;
                }
                else Debug.LogError("Unable to read the dynamic lights bounding volume hierarchy file! Probably because you upgraded from an older version. Please raytrace your scene again.");
            }

            // callback for third-party developers.
            initialized?.Invoke(this, new DynamicLightingInitializedEventArgs(this));
        }

        private void Cleanup()
        {
            if (!isInitialized) return;
            isInitialized = false;

#if UNITY_EDITOR
            // in the editor, unsubscribe from the camera rendering functions that repair preview cameras.
            Camera.onPreRender -= EditorOnPreRenderCallback;
            Camera.onPostRender -= EditorOnPostRenderCallback;
#endif

            if (dynamicLightsBuffer != null && dynamicLightsBuffer.IsValid())
            {
                dynamicLightsBuffer.Release();
                dynamicLightsBuffer = null;
            }

            if (dynamicLightsBvhBuffer != null && dynamicLightsBvhBuffer.IsValid())
            {
                dynamicLightsBvhBuffer.Release();
                dynamicLightsBvhBuffer = null;
            }

            // always disable the bounding volume hierarchy in the shader as it's dangerous now.
            shadersKeywordBvhEnabled = false;

            var raycastedMeshRenderersCount = raycastedMeshRenderers.Count;
            for (int i = 0; i < raycastedMeshRenderersCount; i++)
            {
                var raycastedMeshRenderer = raycastedMeshRenderers[i];
                if (raycastedMeshRenderer.buffer != null && raycastedMeshRenderer.buffer.IsValid())
                {
                    raycastedMeshRenderer.buffer.Release();
                    raycastedMeshRenderer.buffer = null;
                }

                // remove the lightmap data from the material property block.
                CleanupMaterialPropertyBlock(raycastedMeshRenderer);
            }

            sceneRealtimeLights = null;
            activeRealtimeLights = null;

            // -> partial class DynamicLightManager.ShadowCamera cleanup.
            ShadowCameraCleanup();

            // -> partial class DynamicLightManager.LightCookie cleanup.
            LightCookieCleanup();

            // -> partial class DynamicLightManager.LightPositions cleanup.
            LightPositionsCleanup();

            // callback for third-party developers.
            uninitialized?.Invoke(this, new DynamicLightingUninitializedEventArgs(this));
        }

        /// <summary>Removes the lightmap data from the given material property block.</summary>
        /// <param name="meshRenderer">The mesh renderer to be modified.</param>
        private static void CleanupMaterialPropertyBlock(RaycastedMeshRenderer raycastedMeshRenderer)
        {
            // make sure the scene reference is still valid.
            var meshRenderer = raycastedMeshRenderer.renderer;
            if (!meshRenderer) return;

            // assign the material property block to each submesh.
            var hasPropertyBlock = meshRenderer.HasPropertyBlock(); // unity api oversight: cannot differentiate between submesh blocks and normal ones.
            for (int j = 0; j < raycastedMeshRenderer.activeSubMeshCount; j++)
            {
                var materialPropertyBlock = new MaterialPropertyBlock();

                // play nice with other scripts.
                if (hasPropertyBlock)
                    meshRenderer.GetPropertyBlock(materialPropertyBlock, j);

                // remove the lightmap data from the material property block.
                materialPropertyBlock.SetBuffer("lightmap", (ComputeBuffer)null);
                materialPropertyBlock.SetBuffer("dynamic_triangles", (ComputeBuffer)null);
                materialPropertyBlock.SetInteger("lightmap_resolution", 0);

                meshRenderer.SetPropertyBlock(materialPropertyBlock, j);
            }
        }

        internal void RegisterDynamicLight(DynamicLight light)
        {
            Initialize();

            // we only store realtime lights.
            if (!IsRaycastedDynamicLight(light))
                sceneRealtimeLights.Add(light);

            SetRaycastedDynamicLightAvailable(light, true);

            lightRegistered?.Invoke(this, new DynamicLightRegisteredEventArgs(this, light));
        }

        internal void UnregisterDynamicLight(DynamicLight light)
        {
            if (sceneRealtimeLights != null)
            {
                sceneRealtimeLights.Remove(light);
                activeRealtimeLights.Remove(light);
                SetRaycastedDynamicLightAvailable(light, false);

                lightUnregistered?.Invoke(this, new DynamicLightUnregisteredEventArgs(this, light));
            }
        }

        /// <summary>
        /// In the <see cref="raycastedDynamicLights"/> marks a light as either available or unavailable.
        /// </summary>
        /// <param name="light">The <see cref="DynamicLight"/> to be found.</param>
        /// <param name="available">Whether the light is available or not.</param>
        private void SetRaycastedDynamicLightAvailable(DynamicLight light, bool available)
        {
            var raycastedDynamicLightsCount = raycastedDynamicLights.Count;
            for (int i = 0; i < raycastedDynamicLightsCount; i++)
            {
                var raycastedDynamicLight = raycastedDynamicLights[i];
                if (raycastedDynamicLight.light == light)
                {
                    raycastedDynamicLight.lightAvailable = available;

                    // -> partial class DynamicLightManager.LightPositions.
                    if (available)
                        LightPositionsOnRaycastedDynamicLightAvailable(i);

                    break;
                }
            }
        }

        /// <summary>
        /// Whenever the dynamic or realtime light budgets change we must update the shader buffer.
        /// </summary>
        private void ReallocateShaderLightBuffer()
        {
#if UNITY_EDITOR
            if (editorIsPlaying)
#endif
                Debug.LogWarning("Reallocation of dynamic lighting shader buffers on the graphics card due to a light budget change (slow).");

            // properly release any old buffer.
            if (dynamicLightsBuffer != null && dynamicLightsBuffer.IsValid())
                dynamicLightsBuffer.Release();

            shaderDynamicLights = new ShaderDynamicLight[totalLightBudget];
            dynamicLightsBuffer = new ComputeBuffer(shaderDynamicLights.Length, dynamicLightStride, ComputeBufferType.Default);
            ShadersSetGlobalDynamicLights(dynamicLightsBuffer);

            // -> partial class DynamicLightManager.PostProcessing.
            PostProcessingReallocateShaderLightBuffer();
        }

        /// <summary>Gets the total light budget to be reserved on the graphics card.</summary>
        private int totalLightBudget => Mathf.Max(raycastedDynamicLights.Count + realtimeLightBudget, 1);

        /// <summary>This handles the CPU side lighting effects.</summary>
        private unsafe void Update()
        {
            // callback for third-party developers.
            preUpdate?.Invoke(this, dynamicLightingPreUpdateEventArgs);

            // get the time values once as they are expensive native calls.
            var deltaTime = Time.deltaTime;
            timeTime = Time.time;

            // try to find the main camera in the scene.
            var camera = Camera.main;

#if UNITY_EDITOR
            // editor scene view support.
            if (!editorIsPlaying)
            {
                // try using the scene view or current camera.
                var sceneViewCamera = Utilities.GetSceneViewCamera();

                // if the scene view camera does not exist then fallback to the main camera.
                if (sceneViewCamera != null)
                    camera = sceneViewCamera;
            }
            else
            {
                Debug.Assert(camera != null, "Could not find a camera that is tagged \"MainCamera\" for lighting calculations.");
            }

            // if no camera exists in the editor then we stop processing.
            if (camera == null)
                return;

            // respect the scene view lighting toggle.
            {
                var sceneView = UnityEditor.SceneView.lastActiveSceneView;
                if (sceneView)
                {
                    // apply the unlit rendering property.
                    if (renderUnlit)
                        shadersKeywordLitEnabled = false;
                    else
                        shadersKeywordLitEnabled = sceneView.sceneLighting;
                }
                else
                {
                    // apply the unlit rendering property.
                    shadersKeywordLitEnabled = !renderUnlit;
                }
            }
#else
            // apply the unlit rendering property.
            shadersKeywordLitEnabled = !renderUnlit;
#endif
            var raycastedDynamicLightsCount = raycastedDynamicLights.Count;

            // if the budget changed we must recreate the shader buffers.
            var totalLightBudget = raycastedDynamicLightsCount + realtimeLightBudget;
            if (totalLightBudget == 0) return; // sanity check.
            if (shaderDynamicLights.Length != totalLightBudget)
                ReallocateShaderLightBuffer();

            // -> partial class DynamicLightManager.LightPositions.
            LightPositionsUpdate();

            // always process the raycasted dynamic lights.
            for (int i = 0; i < raycastedDynamicLightsCount; i++)
            {
                var raycastedDynamicLight = raycastedDynamicLights[i];

                // destroyed raycasted lights in the scene, must still exist in the shader.
                if (!raycastedDynamicLight.lightAvailable) continue;

                var light = raycastedDynamicLight.light;
                var lightCache = light.cache;

                // when a raycasted light position has been moved away from the origin:
                lightCache.transformPosition = lightPositionsRaycastedLightPositions[i];
                lightCache.transformScale = lightPositionsRaycastedLightScales[i];
                if (!lightCache.transformPosition.ApproximatelyEquals(raycastedDynamicLight.origin))
                {
                    if (!lightCache.movedFromOrigin)
                    {
                        // add it to the realtime lights and disable the raycasted light.
                        sceneRealtimeLights.Add(light);
                        lightCache.movedFromOrigin = true;
                    }

                    // we skip the update here as that's done for realtime lights later.
                }
                else
                {
                    // when a raycasted light position has been restored to the origin:
                    if (lightCache.movedFromOrigin)
                    {
                        // remove it from the realtime lights and enable the raycasted light.
                        sceneRealtimeLights.Remove(light);
                        lightCache.movedFromOrigin = false;
                    }

                    // we must always update the fixed timestep calculator as it relies on Time.deltaTime.
                    lightCache.fixedTimestep.timePerStep = light.lightEffectTimestepFrequency;
                    lightCache.fixedTimestep.Update(deltaTime);
                }
            }

            // clear as many active lights as possible.
            activeRealtimeLights.Clear();

            // update the cached realtime light positions (required for fast sorting).
            var sceneRealtimeLightsCount = sceneRealtimeLights.Count;
            if (sceneRealtimeLightsCount > 0)
            {
                UpdateSceneRealtimeLightPositionsInCache();
            }

            // if we exceed the realtime light budget we sort the realtime lights by distance every
            // frame, as we will assume they are moving around.
            if (sceneRealtimeLightsCount > realtimeLightBudget)
            {
                SortSceneRealtimeLights(camera.transform.position);
            }

            // we calculate the camera frustum planes to cull realtime lights and realtime shadows.
            GeometryUtility.CalculateFrustumPlanes(camera, cameraFrustumPlanes);

            // fill the active realtime lights back up with the closest lights.
            for (int i = 0; i < sceneRealtimeLightsCount; i++)
            {
                var realtimeLight = sceneRealtimeLights[i];

                // we must always update the fixed timestep calculator as it relies on Time.deltaTime.
                realtimeLight.cache.fixedTimestep.timePerStep = realtimeLight.lightEffectTimestepFrequency;
                realtimeLight.cache.fixedTimestep.Update(deltaTime);

                if (activeRealtimeLights.Count < realtimeLightBudget)
                {
#if UNITY_EDITOR    // optimization: only add lights that are within the camera frustum.
                    if (!editorIsPlaying || MathEx.CheckSphereIntersectsFrustum(cameraFrustumPlanes, realtimeLight.cache.transformPosition, realtimeLight.largestLightRadius))
#else
                    if (MathEx.CheckSphereIntersectsFrustum(cameraFrustumPlanes, realtimeLight.cache.transformPosition, realtimeLight.largestLightRadius))
#endif
                    {
                        activeRealtimeLights.Add(realtimeLight);
                    }
                }
            }

            // -> partial class DynamicLightManager.ShadowCamera.
            ShadowCameraUpdate();

            // -> partial class DynamicLightManager.LightCookie.
            LightCookieUpdate();

            // we are going to iterate over all active shader light sources below, we make use of
            // this opportunity, to filter out volumetric light sources for post processing.
            postProcessingVolumetricLightsCount = 0;

            // write the active lights into the shader data.
            var activeRealtimeLightsCount = activeRealtimeLights.Count;
            fixed (ShaderDynamicLight* shaderLightsPtr = shaderDynamicLights)
            {
                var idx = 0;
                for (int i = 0; i < raycastedDynamicLightsCount; i++)
                {
                    var raycastedDynamicLight = raycastedDynamicLights[i];
                    var shaderLight = &shaderLightsPtr[idx];
                    var light = raycastedDynamicLight.light;
                    var lightAvailable = raycastedDynamicLight.lightAvailable;
                    var lightBounceCompression = raycastedDynamicLight.bounceCompression;
                    SetShaderDynamicLight(shaderLight, light, lightBounceCompression, lightAvailable, false);
                    UpdateLightEffects(shaderLight, light, lightAvailable);
                    idx++;

                    if (lightAvailable)
                    {
                        // -> partial class DynamicLightManager.ShadowCamera.
                        ShadowCameraProcessLight(shaderLight, light);

                        // -> partial class DynamicLightManager.LightCookie.
                        LightCookieProcessLight(shaderLight, light);

                        // copy volumetric light sources into the post processing system.
                        // -> partial class DynamicLightManager.PostProcessing.
                        PostProcessingProcessLight(shaderLight, light);
                    }
                }

                for (int i = 0; i < activeRealtimeLightsCount; i++)
                {
                    var light = activeRealtimeLights[i];
                    var shaderLight = &shaderLightsPtr[idx];
                    bool lightAvailable = light;
                    SetShaderDynamicLight(shaderLight, light, DynamicBounceLightingCompressionMode.Inherit, lightAvailable, true);
                    UpdateLightEffects(shaderLight, light, lightAvailable);
                    idx++;

                    if (lightAvailable)
                    {
                        // -> partial class DynamicLightManager.ShadowCamera.
                        ShadowCameraProcessLight(shaderLight, light);

                        // -> partial class DynamicLightManager.LightCookie.
                        LightCookieProcessLight(shaderLight, light);

                        // copy volumetric light sources into the post processing system.
                        // -> partial class DynamicLightManager.PostProcessing.
                        PostProcessingProcessLight(shaderLight, light);
                    }
                }
            }

            // -> partial class DynamicLightManager.ShadowCamera.
            ShadowCameraPostUpdate();

            // upload the active light data to the graphics card.
            var activeDynamicLightsCount = raycastedDynamicLightsCount + activeRealtimeLightsCount;
            if (dynamicLightsBuffer != null && dynamicLightsBuffer.IsValid())
                dynamicLightsBuffer.SetData(shaderDynamicLights, 0, 0, activeDynamicLightsCount);
            ShadersSetGlobalDynamicLightsCount(shadersLastDynamicLightsCount = raycastedDynamicLightsCount);
            ShadersSetGlobalRealtimeLightsCount(shadersLastRealtimeLightsCount = activeRealtimeLightsCount);

            // update the ambient lighting color.
            ShadersSetGlobalDynamicAmbientColor(ambientColor);

            // update the runtime quality settings.
            ShadersSetRuntimeQuality(runtimeQuality);

#if UNITY_EDITOR
            // detect mesh changes in edit mode.
            if (!editorIsPlaying)
            {
                // check in batches of 100 mesh renderers (editor performance in large scenes).
                var raycastedMeshRenderersCount = raycastedMeshRenderers.Count;
                var loopBegin = editorMeshEditDetectionOffset;
                var loopEnd = Math.Min(loopBegin + 100, raycastedMeshRenderersCount);
                editorMeshEditDetectionOffset += 100;
                if (loopEnd == raycastedMeshRenderersCount)
                    editorMeshEditDetectionOffset = 0;

                for (int i = loopBegin; i < loopEnd; i++)
                {
                    // for every game object that requires a lightmap:
                    var lightmap = raycastedMeshRenderers[i];

                    // make sure this object has not already been cleaned.
                    if (lightmap.lastMeshModified) continue;

                    // make sure the scene reference is still valid.
                    var meshRenderer = lightmap.renderer;
                    if (!meshRenderer) continue;

                    // make sure the mesh filter is still valid.
                    var meshFilter = lightmap.lastMeshFilter;
                    if (!meshFilter)
                    {
                        cleanupMaterialPropertyBlock();
                        continue;
                    }

                    // make sure the mesh is still valid.
                    var mesh = meshFilter.sharedMesh;
                    if (!mesh)
                    {
                        cleanupMaterialPropertyBlock();
                        continue;
                    }

                    // if the hash has changed (likely a significant mesh edit):
                    if (mesh.GetFastHash() != lightmap.lastMeshHash)
                    {
                        cleanupMaterialPropertyBlock();
                        continue;
                    }

                    // temporarily disables dynamic lighting on the current renderer.
                    void cleanupMaterialPropertyBlock()
                    {
                        // remember that this mesh was modified.
                        lightmap.lastMeshModified = true;

                        // remove the lightmap data from the material property block.
                        CleanupMaterialPropertyBlock(lightmap);

                        // try to get the mesh name.
                        var meshName = meshFilter ? $" ({meshFilter.name})" : "";

                        // inform the user about the reason for the visual change.
                        Debug.LogWarning($"Temporarily disabled Dynamic Lighting for {meshRenderer.name}{meshName}.\nA change in the mesh was detected, which is expected when editing level geometry. Please raytrace your scene again once you have finished editing. If you're curious about the reason for this action, hit play now to see for yourself.", meshRenderer);
                    }
                }
            }
#endif
            // callback for third-party developers.
            postUpdate?.Invoke(this, dynamicLightingPostUpdateEventArgs);
        }

        /// <summary>
        /// Updates all of the <see cref="DynamicLight.cache"/><see
        /// cref="DynamicLightCache.transformPosition"/> for the <see cref="sceneRealtimeLights"/>.
        /// </summary>
        private void UpdateSceneRealtimeLightPositionsInCache()
        {
            var sceneRealtimeLightsCount = sceneRealtimeLights.Count;
            for (int i = 0; i < sceneRealtimeLightsCount; i++)
            {
                var sceneRealtimeLight = sceneRealtimeLights[i];
                var sceneRealtimeLightTransform = sceneRealtimeLight.transform;
                sceneRealtimeLight.cache.transformPosition = sceneRealtimeLightTransform.position;
                sceneRealtimeLight.cache.transformScale = sceneRealtimeLightTransform.localScale;
            }
        }

        /// <summary>
        /// Sorts the scene realtime light lists by the distance from the specified origin. The
        /// closests lights will appear first in the list.
        /// <para>Requires a call to <see cref="UpdateSceneRealtimeLightPositionsInCache"/>.</para>
        /// </summary>
        /// <param name="origin">The origin (usually the camera world position).</param>
        private void SortSceneRealtimeLights(Vector3 origin)
        {
            sceneRealtimeLights.Sort((a, b) => (origin - a.cache.transformPosition).sqrMagnitude
            .CompareTo((origin - b.cache.transformPosition).sqrMagnitude));
        }

        private unsafe void SetShaderDynamicLight(ShaderDynamicLight* shaderLight, DynamicLight light, DynamicBounceLightingCompressionMode lightBounceCompression, bool lightAvailable, bool realtime)
        {
            // destroyed raycasted lights in the scene, must still exist in the shader. we can make
            // the radius negative causing an early out whenever a fragment tries to use it.
            if (!lightAvailable || (!realtime && light.cache.movedFromOrigin))
            {
                shaderLight->radiusSqr = -1.0f;
                return;
            }

            // retrieving the slow information about the light.
            var lightRadius = light.lightRadius;
            var lightColor = light.lightColor;
            var lightBounceColor = light.lightBounceColor;
            var lightBounceModifier = light.lightBounceModifier;
            var lightFalloff = light.lightFalloff;
            var lightType = light.lightType;
            var lightShimmer = light.lightShimmer;

            // the first 5 bits are unused (used to be channel index).
            // bit 6 marks lights as realtime without shadows.
            shaderLight->channel = realtime ? 32 : (uint)0;
            // bit 7-10 store the light type.
            shaderLight->channel |= (uint)lightType << 6;
            // bit 11-12 store the light shimmering.
            shaderLight->channel |= (uint)lightShimmer << 10;

            // -> the light intensity is set by the effects update step.
            shaderLight->position = light.cache.transformPosition;

            shaderLight->color.x = lightColor.r;
            shaderLight->color.y = lightColor.g;
            shaderLight->color.z = lightColor.b;

            shaderLight->bounceColor.x = (lightColor.r + (lightBounceColor.r - lightColor.r) * lightBounceColor.a) * lightBounceModifier;
            shaderLight->bounceColor.y = (lightColor.g + (lightBounceColor.g - lightColor.g) * lightBounceColor.a) * lightBounceModifier;
            shaderLight->bounceColor.z = (lightColor.b + (lightBounceColor.b - lightColor.b) * lightBounceColor.a) * lightBounceModifier;

            // unity stores colors in gamma and we must convert them to linear.
            if (colorSpace == ColorSpace.Linear)
            {
                UMath.GammaToLinearSpace(&shaderLight->color);
                UMath.GammaToLinearSpace(&shaderLight->bounceColor);
            }

            shaderLight->radiusSqr = lightRadius * lightRadius;
            shaderLight->falloff = lightRadius * lightFalloff * lightFalloff;

            // bit 18-21 is the bounce lighting compression mode.
            shaderLight->channel |= (((uint)lightBounceCompression - 1) << 17) & 917504u;

            // reset to impossible values to detect assignment later below.
            shaderLight->forward.x = -200f;
            shaderLight->up.x = -200f;

            Quaternion lightRotation;
            switch (lightType)
            {
                case DynamicLightType.Spot:
                    shaderLight->gpFloat1 = Mathf.Cos(light.lightCutoff * Mathf.Deg2Rad);
                    shaderLight->gpFloat2 = Mathf.Cos(light.lightOuterCutoff * Mathf.Deg2Rad);
                    lightRotation = light.transform.rotation;
                    shaderLight->forward = lightRotation * -Vector3.forward;

                    if (light.lightCookieTexture)
                    {
                        // -> a hint for the partial class DynamicLightManager.LightCookie so that
                        //    we do not have to check for the light cookie texture again.
                        shaderLight->cookieIndex = uint.MaxValue;
                        shaderLight->up = lightRotation * Vector3.up;
                        shaderLight->gpFloat3 = 0.5f * Mathf.Tan((90.0f - light.lightOuterCutoff) * Mathf.Deg2Rad);
                    }
                    break;

                case DynamicLightType.Discoball:
                    shaderLight->gpFloat1 = Mathf.Cos(light.lightCutoff * Mathf.Deg2Rad);
                    shaderLight->gpFloat2 = Mathf.Cos(light.lightOuterCutoff * Mathf.Deg2Rad);
                    lightRotation = light.transform.rotation;
                    shaderLight->up = lightRotation * Vector3.up;
                    shaderLight->forward = lightRotation * Vector3.forward;
                    break;

                case DynamicLightType.Wave:
                    shaderLight->gpFloat1 = light.lightWaveSpeed;
                    shaderLight->gpFloat2 = light.lightWaveFrequency;
                    break;

                case DynamicLightType.Interference:
                    shaderLight->gpFloat1 = light.lightWaveSpeed;
                    shaderLight->gpFloat2 = light.lightWaveFrequency;
                    lightRotation = light.transform.rotation;
                    shaderLight->up = lightRotation * Vector3.up;
                    shaderLight->forward = lightRotation * Vector3.forward;
                    break;

                case DynamicLightType.Rotor:
                    shaderLight->gpFloat1 = light.lightWaveSpeed;
                    shaderLight->gpFloat2 = Mathf.Round(light.lightWaveFrequency);
                    shaderLight->gpFloat3 = light.lightRotorCenter;
                    lightRotation = light.transform.rotation;
                    shaderLight->up = lightRotation * Vector3.up;
                    shaderLight->forward = lightRotation * Vector3.forward;
                    break;

                case DynamicLightType.Shock:
                    shaderLight->gpFloat1 = light.lightWaveSpeed;
                    shaderLight->gpFloat2 = light.lightWaveFrequency;
                    break;

                case DynamicLightType.Disco:
                    shaderLight->gpFloat1 = light.lightWaveSpeed;
                    shaderLight->gpFloat2 = Mathf.Round(light.lightWaveFrequency);
                    shaderLight->gpFloat3 = light.lightDiscoVerticalSpeed;
                    lightRotation = light.transform.rotation;
                    shaderLight->up = lightRotation * Vector3.up;
                    shaderLight->forward = lightRotation * Vector3.forward;
                    break;
            }

            switch (lightShimmer)
            {
                case DynamicLightShimmer.Water:
                    shaderLight->shimmerScale = light.lightShimmerScale;
                    shaderLight->shimmerModifier = light.lightShimmerModifier;
                    break;

                case DynamicLightShimmer.Random:
                    shaderLight->shimmerScale = light.lightShimmerScale;
                    shaderLight->shimmerModifier = light.lightShimmerModifier;
                    break;
            }

            if (light.lightVolumetricType != DynamicLightVolumetricType.None)
            {
                // does not require a channel bit as the post-processing shader knows implicitly.
                // -> the volumetric intensity is set by the effects update step.
                // -> the volumetric radius is set by the partial class DynamicLightManager.PostProcessing.
                // -> the volumetric thickness is set by the partial class DynamicLightManager.PostProcessing.
                shaderLight->volumetricVisibility = light.lightVolumetricVisibility <= 0f ? 1.0f / 0.00001f : 1.0f / light.lightVolumetricVisibility;

                // calculate the forward vector if it has not been calculated yet.
                if (light.lightVolumetricType == DynamicLightVolumetricType.ConeZ && shaderLight->forward.x == -200f)
                {
                    lightRotation = light.transform.rotation;
                    shaderLight->forward = lightRotation * Vector3.forward;
                }
                // calculate the up vector if it has not been calculated yet.
                else if (light.lightVolumetricType == DynamicLightVolumetricType.ConeY && shaderLight->up.x == -200f)
                {
                    lightRotation = light.transform.rotation;
                    shaderLight->up = lightRotation * Vector3.up;
                }
            }
        }

        private unsafe void UpdateLightEffects(ShaderDynamicLight* shaderLight, DynamicLight light, bool lightAvailable)
        {
            // destroyed raycasted lights in the scene, must still exist in the shader.
            if (!lightAvailable) return;

            // retrieving light fields to reduce overhead.
            var lightCache = light.cache;

            // continuous light effects:

            switch (light.lightEffect)
            {
                case DynamicLightEffect.Steady:
                    lightCache.intensity = 1.0f;
                    break;

                case DynamicLightEffect.Pulse:
                    lightCache.intensity = Mathf.Lerp(light.lightEffectPulseModifier, 1.0f, (1f + Mathf.Sin(timeTime * Mathf.PI * 2f * light.lightEffectPulseSpeed)) * 0.5f);
                    break;
            }

            // fixed timestep light effects:

            if (lightCache.fixedTimestep.pendingSteps > 0 || !lightCache.initialized)
            {
                lightCache.initialized = true;

                switch (light.lightEffect)
                {
                    case DynamicLightEffect.Random:
                        lightCache.intensity = Mathf.Lerp(light.lightEffectPulseModifier, 1.0f, Random.value);
                        break;

                    case DynamicLightEffect.Flicker:
                        var random = Random.value;
                        if (random < 0.5f)
                            lightCache.intensity = 0.0f;
                        else
                            lightCache.intensity = Mathf.Lerp(light.lightEffectPulseModifier, 1.0f, Random.value);
                        break;

                    case DynamicLightEffect.Strobe:
                        lightCache.strobeActive = !lightCache.strobeActive;
                        lightCache.intensity = lightCache.strobeActive ? 1.0f : light.lightEffectPulseModifier;
                        break;
                }
            }

            // assign the cached values to the shader lights.

            shaderLight->intensity = light.lightIntensity * lightCache.intensity;

            if (light.lightVolumetricType != DynamicLightVolumetricType.None)
            {
                shaderLight->volumetricIntensity = light.lightVolumetricIntensity * lightCache.intensity;
            }
        }

        /// <summary>
        /// Gets or sets a special graphics mode for underpowered laptops with integrated graphics.
        /// You should add this option to your in-game settings menu.
        /// <para>Disables support for real-time shadows.</para>
        /// <para>Disables support for bounce lighting.</para>
        /// <para>Uses very cheap bilinear filtering for shadows.</para>
        /// </summary>
        public bool integratedGraphicsModeEnabled
        {
            get => shadersKeywordIntegratedGraphicsEnabled;
            set => shadersKeywordIntegratedGraphicsEnabled = value;
        }

        private void OnDrawGizmos()
        {
#if UNITY_EDITOR
            // the scene view continuous preview toggle.
            var sceneView = UnityEditor.SceneView.lastActiveSceneView;
            if (sceneView && sceneView.sceneViewState.fxEnabled && sceneView.sceneViewState.alwaysRefresh)
            {
                // ensure continuous update calls.
                if (!editorIsPlaying)
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
        /// <param name="dynamicLightingTracerFlags">
        /// The flags controlling aspects of the raytracing process, such as skipping certain computations.
        /// </param>
        public void Raytrace(int maximumLightmapSize = 2048, DynamicLightingTracerFlags dynamicLightingTracerFlags = DynamicLightingTracerFlags.None)
        {
#if UNITY_EDITOR
            if (!EditorEnsureUserSavedScene()) return;
#endif
            var tracer = new DynamicLightingTracer();
            tracer.maximumLightmapSize = maximumLightmapSize;
            tracer.bounceLightingDefaultCompression = bounceLightingCompression;
            tracer.tracerFlags = dynamicLightingTracerFlags;

            // callback for third-party developers.
            bool cancelled = false;
            tracer.cancelled += (s, e) => { cancelled = true; traceCancelled?.Invoke(this, new DynamicLightingTraceCancelledEventArgs(this)); };

            // callback for third-party developers.
            traceStarted?.Invoke(this, new DynamicLightingTraceStartedEventArgs(this));

            tracer.StartRaytracing();

            // callback for third-party developers.
            if (!cancelled)
                traceCompleted?.Invoke(this, new DynamicLightingTraceCompletedEventArgs(this));
        }
    }
}