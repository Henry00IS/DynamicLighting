using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    [System.Serializable]
    public class Lightmap
    {
        public MeshRenderer renderer;

        public int identifier;

        public int resolution;

        public ComputeBuffer buffer;
    }
}