using UnityEngine;

namespace AlpacaIT.VertexTracer
{
    public class VertexPointLight : MonoBehaviour
    {
        public Color lightColor = Color.white;
        public float lightIntensity = 10.0f;

        public float constant = 1.0f;
        public float linear = 0.14f;
        public float quadratic = 0.07f;
    }
}