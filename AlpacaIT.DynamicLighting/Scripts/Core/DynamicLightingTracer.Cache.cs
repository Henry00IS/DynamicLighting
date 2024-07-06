using System;
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

        private struct RaycastOriginMeta
        {
            public int x;
            public int y;

            public RaycastOriginMeta(int x, int y)
            {
                this.x = x;
                this.y = y;
            }
        }

        private interface IRaycastMissHandler
        {
            void OnRaycastMiss(int x, int y);

            void OnRaycastHit(int x, int y);
        }

        private struct CachedLightData
        {
            public Vector3 position;
            public Bounds bounds;
            public PhotonCube photonCube;

            public CachedLightData(DynamicLight dynamicLight)
            {
                position = dynamicLight.transform.position;
                bounds = MathEx.GetSphereBounds(position, dynamicLight.lightRadius);
                photonCube = null;
            }
        }
    }
}