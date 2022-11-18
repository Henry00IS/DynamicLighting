using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    public class DynamicPointLight : MonoBehaviour
    {
        public Color lightColor = Color.white;
        public float lightIntensity = 10.0f;
        public float lightRadius = 2.0f;
        public uint lightChannel = 0;

        public LightType lightType = LightType.Steady;
        public float lightTypePulseSpeed = 10.0f;
        [Range(0f, 1f)]
        public float lightTypePulseModifier = 0.25f;

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = lightColor;

            Gizmos.DrawIcon(transform.position, "Packages/de.alpacait.dynamiclighting/Gizmos/DynamicLightingPointLight.psd", true, lightColor);
        }
#endif
    }
}