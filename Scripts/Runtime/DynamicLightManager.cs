using System.Collections.Generic;
using UnityEngine;

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

        // the NVIDIA Quadro K1000M (2012) can handle 25 lights at 30fps.
        // the NVIDIA GeForce GTX 1050 Ti (2016) can handle 125 lights between 53-68fps.
        // the NVIDIA GeForce RTX 3080 (2020) can handle 2000 lights at 70fps.

        public int dynamicLightBudget = 64;
        public int realtimeLightBudget = 32;

        /// <summary>The memory size in bytes of the <see cref="ShaderDynamicLight"/> struct.</summary>
        private int dynamicLightStride;
        private Lightmap[] lightmaps;
        private DynamicLight[] dynamicLights;
        private List<DynamicLight> realtimeLights;

        private ShaderDynamicLight[] shaderDynamicLights;
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

        [UnityEditor.MenuItem("Dynamic Lighting/Toggle Continuous Preview", false, 21)]
        public static void EnableContinuousPreview()
        {
            Instance.useContinuousPreview = !Instance.useContinuousPreview;
        }

        [UnityEditor.MenuItem("Dynamic Lighting/Toggle Unlit Surfaces", false, 22)]
        public static void ToggleUnlitSurfaces()
        {
            if (Shader.IsKeywordEnabled("DYNAMIC_LIGHTING_UNLIT"))
                Shader.DisableKeyword("DYNAMIC_LIGHTING_UNLIT");
            else
                Shader.EnableKeyword("DYNAMIC_LIGHTING_UNLIT");
        }

        [UnityEditor.MenuItem("Dynamic Lighting/PayPal Donation", false, 41)]
        public static void PayPalDonation()
        {
            Application.OpenURL("https://paypal.me/henrydejongh");
        }

#endif

        /// <summary>Finds all of the dynamic lights in the scene that are not realtime.</summary>
        /// <returns>The collection of dynamic lights in the scene.</returns>
        public static DynamicLight[] FindDynamicLightsInScene()
        {
            var dynamicPointLights = new List<DynamicLight>(FindObjectsOfType<DynamicLight>());

            // remove all of the realtime lights from our collection.
            var dynamicPointLightsCount = dynamicPointLights.Count;
            for (int i = dynamicPointLightsCount; i-- > 0;)
                if (dynamicPointLights[i].realtime)
                    dynamicPointLights.RemoveAt(i);

            return dynamicPointLights.ToArray();
        }

        /// <summary>Immediately reloads the lighting.</summary>
        public void Reload()
        {
            OnDestroy();
            Awake();
        }

        private void Awake()
        {
            dynamicLightStride = System.Runtime.InteropServices.Marshal.SizeOf(typeof(ShaderDynamicLight));

            // more dynamic point lights will never be instantiated during play so we fetch them here once.
            dynamicLights = FindDynamicLightsInScene();

            // prepare to store realtime lights that get created during gameplay.
            realtimeLights = new List<DynamicLight>(realtimeLightBudget);

            // allocate the required arrays and buffers according to our budget.
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

        public void RegisterRealtimeLight(DynamicLight light)
        {
            realtimeLights.Add(light);
        }

        public void UnregisterRealtimeLight(DynamicLight light)
        {
            realtimeLights.Remove(light);
        }

        /// <summary>This handles the CPU side lighting effects.</summary>
        private void Update()
        {
            var idx = 0;

            for (int i = 0; i < dynamicLights.Length; i++)
            {
                var light = dynamicLights[i];
                SetShaderDynamicLight(idx, light);
                UpdateLightEffects(idx, light);
                idx++;
            }

            var realtimeLightsCount = realtimeLights.Count;
            for (int i = 0; i < realtimeLightsCount; i++)
            {
                var light = realtimeLights[i];
                SetShaderDynamicLight(idx, light);
                UpdateLightEffects(idx, light);
                idx++;
            }

            dynamicLightsBuffer.SetData(shaderDynamicLights);

            Shader.SetGlobalInt("dynamic_lights_count", dynamicLights.Length + realtimeLightsCount);
        }

        private void SetShaderDynamicLight(int idx, DynamicLight light)
        {
            shaderDynamicLights[idx].position = light.transform.position;
            shaderDynamicLights[idx].color = new Vector3(light.lightColor.r, light.lightColor.g, light.lightColor.b);
            shaderDynamicLights[idx].intensity = light.lightIntensity;
            shaderDynamicLights[idx].radius = light.lightRadius;
            shaderDynamicLights[idx].channel = light.lightChannel;

            switch (light.lightType)
            {
                case DynamicLightType.Point:
                    shaderDynamicLights[idx].channel &= ~((uint)1 << 6);
                    break;

                case DynamicLightType.Spot:
                    shaderDynamicLights[idx].channel |= (uint)1 << 6;
                    break;
            }

            shaderDynamicLights[idx].forward = light.transform.forward;
            shaderDynamicLights[idx].cutoff = Mathf.Cos(light.lightCutoff * Mathf.Deg2Rad);
            shaderDynamicLights[idx].outerCutoff = Mathf.Cos(light.lightOuterCutoff * Mathf.Deg2Rad);
        }

        private void UpdateLightEffects(int idx, DynamicLight light)
        {
            switch (light.lightEffect)
            {
                case DynamicLightEffect.Steady:
                    break;

                case DynamicLightEffect.Pulse:
                    shaderDynamicLights[idx].intensity *= Mathf.Lerp(light.lightEffectPulseModifier, 1.0f, (1f + Mathf.Sin(Time.time * light.lightEffectPulseSpeed)) * 0.5f);
                    break;

                case DynamicLightEffect.Flicker:
                    shaderDynamicLights[idx].intensity *= Random.value;
                    break;

                case DynamicLightEffect.Strobe:
                    break;
            }
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