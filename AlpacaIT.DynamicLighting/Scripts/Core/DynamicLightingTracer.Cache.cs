using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    // implements several structs and classes to help speed up data retrieval.

    internal partial class DynamicLightingTracer
    {
        /// <summary>
        /// Takes two <see cref="RaycastHandler[]"/> and swaps them on demand. This is used instead of
        /// copying the accumulator into a secondary array.
        /// </summary>
        private unsafe class RaycastCommandMetaSwapper
        {
            public RaycastCommandMeta[] a;
            public RaycastCommandMeta[] b;

            public RaycastCommandMetaSwapper(RaycastCommandMeta[] a, RaycastCommandMeta[] b)
            {
                this.a = a;
                this.b = b;
            }

            public void Swap()
            {
                (b, a) = (a, b);
            }
        }

        private struct RaycastCommandMeta
        {
            public int xyPtr;
            public int lightChannel;

            public RaycastCommandMeta(int xyPtr, int lightChannel)
            {
                this.xyPtr = xyPtr;
                this.lightChannel = lightChannel;
            }
        }

        private struct CachedLightData
        {
            public Vector3 position;
            public FastBounds bounds;
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