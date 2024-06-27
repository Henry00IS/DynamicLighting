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
            public float lightRadius;
            public Vector3 lightPosition;
            public List<IlluminationSample> illuminationSamples;
            public BitArray3 illuminatedVoxels;

            public RaycastCommandMeta(int x, int y, Vector3 world, Vector3 normal, uint lightChannel, List<IlluminationSample> illuminationSamples, float lightRadius, Vector3 lightPosition, BitArray3 illuminatedVoxels)
            {
                this.x = x;
                this.y = y;
                this.world = world;
                this.normal = normal;
                this.illuminationSamples = illuminationSamples;
                this.lightChannel = lightChannel;
                this.lightRadius = lightRadius;
                this.lightPosition = lightPosition;
                this.illuminatedVoxels = illuminatedVoxels;
            }
        }

        private struct CachedLightData
        {
            public Vector3 position;
            public Bounds bounds;
            public List<IlluminationSample> illuminationSamples;
            public Vector3 illuminationQuadrant;
            public BitArray3 illuminatedVoxels;

            public CachedLightData(DynamicLight dynamicLight)
            {
                position = dynamicLight.transform.position;
                bounds = MathEx.GetSphereBounds(position, dynamicLight.lightRadius);

                // only collect illumination samples when the light is not direct illumination only.
                if (dynamicLight.lightIllumination != DynamicLightIlluminationMode.DirectIllumination)
                {
                    illuminationSamples = new List<IlluminationSample>();
                    illuminationQuadrant = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
                    var size = Mathf.FloorToInt(Mathf.Max(1.0f, dynamicLight.lightRadius * 4f));
                    illuminatedVoxels = new BitArray3(size, size, size);
                }
                else
                {
                    illuminationSamples = null;
                    illuminationQuadrant = Vector3.zero;
                    illuminatedVoxels = null;
                }
            }
        }
    }
}