using System.Collections.Generic;
using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    // implements several structs and classes to help speed up data retrieval.

    internal partial class DynamicLightingTracer
    {
        private struct RaycastCommandMeta
        {
            public int x;
            public int y;
            public Vector3 world;
            public Vector3 normal;
            public uint lightChannel;
            public float lightDistance;
            public float lightRadius;
            public List<IlluminationSample> illuminationSamples;

            public RaycastCommandMeta(int x, int y, Vector3 world, Vector3 normal, uint lightChannel, List<IlluminationSample> illuminationSamples, float lightDistance, float lightRadius)
            {
                this.x = x;
                this.y = y;
                this.world = world;
                this.normal = normal;
                this.illuminationSamples = illuminationSamples;
                this.lightChannel = lightChannel;
                this.lightDistance = lightDistance;
                this.lightRadius = lightRadius;
            }
        }

        private struct CachedLightData
        {
            public Vector3 position;
            public Bounds bounds;
            public List<IlluminationSample> illuminationSamples;

            public CachedLightData(DynamicLight dynamicLight)
            {
                position = dynamicLight.transform.position;
                bounds = MathEx.GetSphereBounds(position, dynamicLight.lightRadius);
                illuminationSamples = new List<IlluminationSample>();
            }
        }
    }
}