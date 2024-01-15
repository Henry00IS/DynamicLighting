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
            public uint lightChannel;

            public RaycastCommandMeta(int x, int y, Vector3 world, uint lightChannel)
            {
                this.x = x;
                this.y = y;
                this.world = world;
                this.lightChannel = lightChannel;
            }
        }

        private struct CachedLightData
        {
            public Vector3 position;
            public Bounds bounds;

            public CachedLightData(DynamicLight dynamicLight)
            {
                position = dynamicLight.transform.position;
                bounds = MathEx.GetSphereBounds(position, dynamicLight.lightRadius);
            }
        }
    }
}