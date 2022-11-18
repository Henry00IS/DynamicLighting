using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    [ExecuteInEditMode]
    public class DynamicLightManager : MonoBehaviour
    {
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

        // the NVIDIA Quadro K1000M (2012) can handle 25 lights at 30fps.
        // the NVIDIA GeForce GTX 1050 Ti (2016) can handle 125 lights between 53-68fps.
        // the NVIDIA GeForce RTX 3080 (2020) can handle 2000 lights at 70fps.

        public int dynamicLightBudget = 64;
        public int realtimeLightBudget = 32;

        /// <summary>The memory size in bytes of the <see cref="DynamicLight"/> struct.x</summary>
        private int dynamicLightStride;
        private Lightmap[] lightmaps;
        private DynamicPointLight[] dynamicPointLights;

        private DynamicLight[] dynamicLights;
        private ComputeBuffer dynamicLightsBuffer;

#if UNITY_EDITOR

        private bool useContinuousPreview = false;

        private void OnEnable()
        {
            // handle C# reloads in the editor.
            if (!Application.isPlaying)
                Awake();
        }

        private void OnDisable()
        {
            // handle C# reloads in the editor.
            if (!Application.isPlaying)
                OnDestroy();
        }

        [UnityEditor.MenuItem("Dynamic Lighting/Toggle Continuous Preview", false, 11)]
        public static void EnableContinuousPreview()
        {
            Instance.useContinuousPreview = !Instance.useContinuousPreview;
        }

        [UnityEditor.MenuItem("Dynamic Lighting/Toggle Unlit Surfaces", false, 12)]
        public static void ToggleUnlitSurfaces()
        {
            if (Shader.IsKeywordEnabled("DYNAMIC_LIGHTING_UNLIT"))
                Shader.DisableKeyword("DYNAMIC_LIGHTING_UNLIT");
            else
                Shader.EnableKeyword("DYNAMIC_LIGHTING_UNLIT");
        }

#endif

        public void Awake()
        {
            dynamicLightStride = System.Runtime.InteropServices.Marshal.SizeOf(typeof(DynamicLight));

            // more dynamic point lights will never be instantiated during play so we fetch them here once.
            dynamicPointLights = FindObjectsOfType<DynamicPointLight>();

            // allocate the required arrays and buffers according to our budget.
            dynamicLights = new DynamicLight[dynamicLightBudget + realtimeLightBudget];
            dynamicLightsBuffer = new ComputeBuffer(dynamicLights.Length, dynamicLightStride, ComputeBufferType.Default);
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

        private void OnDestroy()
        {
            dynamicLightsBuffer.Release();

            for (int i = 0; i < lightmaps.Length; i++)
                lightmaps[i].buffer.Release();
        }

        private void Update()
        {
            for (int i = 0; i < dynamicPointLights.Length; i++)
            {
                var light = dynamicPointLights[i];
                dynamicLights[i].position = light.transform.position;
                dynamicLights[i].color = new Vector3(light.lightColor.r, light.lightColor.g, light.lightColor.b);
                dynamicLights[i].intensity = light.lightIntensity;
                dynamicLights[i].radius = light.lightRadius;
                dynamicLights[i].channel = light.lightChannel;

                switch (light.lightType)
                {
                    case LightType.Steady:
                        break;

                    case LightType.Pulse:
                        dynamicLights[i].intensity *= Mathf.Lerp(light.lightTypePulseModifier, 1.0f, (1f + Mathf.Sin(Time.time * light.lightTypePulseSpeed)) * 0.5f);
                        break;

                    case LightType.Flicker:
                        dynamicLights[i].intensity *= Random.value;
                        break;

                    case LightType.Strobe:
                        break;
                }
            }
            dynamicLightsBuffer.SetData(dynamicLights);

            Shader.SetGlobalInt("dynamic_lights_count", dynamicPointLights.Length);
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