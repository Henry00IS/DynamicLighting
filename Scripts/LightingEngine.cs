
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace AlpacaIT.VertexTracer
{
    public class LightingEngine : MonoBehaviour
    {
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
        private VertexPointLight[] lights;

        private ShaderLight[] shaderLights;
        private ComputeBuffer lightsBuffer;

        LightingEngine()
        {
            ShaderLightStride = System.Runtime.InteropServices.Marshal.SizeOf(typeof(ShaderLight));
        }

        private void Start()
        {
            lights = FindObjectsOfType<VertexPointLight>();
            shaderLights = new ShaderLight[lights.Length];
            lightsBuffer = new ComputeBuffer(lights.Length, ShaderLightStride, ComputeBufferType.Default);

            var meshRenderers = FindObjectsOfType<MeshRenderer>();
            foreach (var meshRenderer in meshRenderers)
            {
                if (!meshRenderer.gameObject.isStatic) continue;

                var material = meshRenderer.sharedMaterial;
                if (material.name == "Vertex Tracer Material")
                    materials.Add(material);
            }

            UpdateLightCount();
        }

        private void OnDestroy()
        {
            lightsBuffer.Release();
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

                if (i == 0) shaderLights[i].intensity = UnityEngine.Random.value * 4f;
                if (i == 3) shaderLights[i].intensity = 1f + Mathf.Sin(Time.realtimeSinceStartup * 20f) * 2f;
                if (i == 2) shaderLights[i].intensity = 1f + Mathf.Cos(Time.realtimeSinceStartup * 4f) * 2f;
                if (i == 1) shaderLights[i].radius = 5f + Mathf.Sin(Time.realtimeSinceStartup * 7f);
                if (i == 1) shaderLights[i].color.x = (1f + Mathf.Sin(Time.realtimeSinceStartup * 4f)) * 0.5f;
                if (i == 1) shaderLights[i].color.y = (1f + Mathf.Cos(Time.realtimeSinceStartup * 4f)) * 0.5f;
            }
            lightsBuffer.SetData(shaderLights);

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
