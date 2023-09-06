using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    [System.Serializable]
    internal class Lightmap
    {
        public MeshRenderer renderer;

        public int identifier;

        public int resolution;

        public ComputeBuffer buffer;
    }
}