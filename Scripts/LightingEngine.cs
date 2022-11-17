using System.Collections.Generic;
using UnityEngine;

namespace AlpacaIT.VertexTracer
{
    public class LightingEngine : MonoBehaviour
    {
        private static LightingEngine s_Instance;

        /// <summary>Gets the singleton game manager instance or creates it.</summary>
        public static LightingEngine Instance
        {
            get
            {
                // if known, immediately return the instance.
                if (s_Instance) return s_Instance;

                // C# hot reloading support: try finding an existing instance in the scene.
                s_Instance = FindObjectOfType<LightingEngine>();

                // otherwise create a new instance in scene.
                if (!s_Instance)
                    s_Instance = new GameObject("[LightingEngine]").AddComponent<LightingEngine>();

                return s_Instance;
            }
        }

        /// <summary>A point light (this struct is mirrored in the shader and can not be modified).</summary>
        private struct ShaderLight
        {
            public Vector3 position;
            public Vector3 color;
            public float intensity;
            public float radius;
            public uint channel;
        };

        private readonly int ShaderLightStride;
        private List<Material> materials = new List<Material>();
        private List<Lightmap> lightmaps = new List<Lightmap>();
        private VertexPointLight[] lights;

        private ShaderLight[] shaderLights;
        private ComputeBuffer lightsBuffer;

        private LightingEngine()
        {
            ShaderLightStride = System.Runtime.InteropServices.Marshal.SizeOf(typeof(ShaderLight));
        }

        private void OnEnable()
        {
            lights = FindObjectsOfType<VertexPointLight>();
            shaderLights = new ShaderLight[lights.Length];
            lightsBuffer = new ComputeBuffer(lights.Length, ShaderLightStride, ComputeBufferType.Default);

            var meshRenderers = FindObjectsOfType<MeshRenderer>();
            foreach (var meshRenderer in meshRenderers)
            {
#if UNITY_EDITOR
                if (!meshRenderer.gameObject.isStatic) continue;
#else
                if (!meshRenderer.isPartOfStaticBatch) continue;
#endif
                if (meshRenderer.TryGetComponent<Lightmap>(out var lightmap))
                {
                    var material = meshRenderer.sharedMaterial;
                    if (material == null)
                    {
                        Debug.LogError("Encountered a null material. Please rebake your lightmap again.");
                        continue;
                    }

                    // create an instantiated copy of the material.
                    material = new Material(material);
                    meshRenderer.sharedMaterial = material;
                    materials.Add(material);
                    lightmaps.Add(lightmap);

                    if (RuntimeUtilities.ReadLightmapData(lightmap.identifier, out uint[] pixels))
                    {
                        lightmap.buffer = new ComputeBuffer(pixels.Length, 4);
                        lightmap.buffer.SetData(pixels);
                        material.SetBuffer("lightmap", lightmap.buffer);
                        material.SetInt("lightmap_resolution", lightmap.resolution);
                    }
                    else
                    {
                        Debug.LogError("Unable to read the lightmap " + lightmap.identifier + " data file!");
                    }
                }
            }
        }

        private void OnDisable()
        {
            lightsBuffer.Release();

            foreach (var lightmap in lightmaps)
                lightmap.buffer.Release();
        }

        private void Update()
        {
            for (int i = 0; i < lights.Length; i++)
            {
                var light = lights[i];
                shaderLights[i].position = light.transform.position;
                shaderLights[i].color = new Vector3(light.lightColor.r, light.lightColor.g, light.lightColor.b);
                shaderLights[i].intensity = light.lightIntensity;
                shaderLights[i].radius = light.lightRadius;
                shaderLights[i].channel = light.lightChannel;

                switch (light.lightType)
                {
                    case LightType.Steady:
                        break;

                    case LightType.Pulse:
                        shaderLights[i].intensity *= Mathf.Lerp(light.lightTypePulseModifier, 1.0f, (1f + Mathf.Sin(Time.time * light.lightTypePulseSpeed)) * 0.5f);
                        break;

                    case LightType.Flicker:
                        shaderLights[i].intensity *= UnityEngine.Random.value;
                        break;

                    case LightType.Strobe:
                        break;
                }
            }
            lightsBuffer.SetData(shaderLights);

            UpdateLightCount();
            /*
            var materialsCount = materials.Count;
            for (int i = 0; i < materialsCount; i++)
            {
                var material = materials[i];
            }*/
        }

        private void UpdateLightCount()
        {
            var materialsCount = materials.Count;
            for (int i = 0; i < materialsCount; i++)
            {
                var material = materials[i];
                material.SetInt("lights_count", lights.Length);
                material.SetBuffer("lights", lightsBuffer);
            }
        }
    }
}