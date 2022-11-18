using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    public class Lightmap : MonoBehaviour
    {
        [HideInInspector]
        [SerializeField]
        public int identifier;

        [HideInInspector]
        [SerializeField]
        public int resolution;

        public ComputeBuffer buffer;
    }
}