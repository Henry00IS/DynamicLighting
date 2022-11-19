using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    public class DynamicLight : MonoBehaviour
    {
        public Color lightColor = Color.white;
        public float lightIntensity = 2.0f;
        public float lightRadius = 4.0f;
        public uint lightChannel = 0;

        public LightType lightType = LightType.Steady;
        public float lightTypePulseSpeed = 10.0f;
        [Range(0f, 1f)]
        public float lightTypePulseModifier = 0.25f;

        public bool realtime { get => lightChannel == 32; }

        private void Start()
        {
            if (realtime)
            {
                DynamicLightManager.Instance.RegisterRealtimeLight(this);
            }
        }

        private void OnDestroy()
        {
            if (realtime)
            {
                DynamicLightManager.Instance.UnregisterRealtimeLight(this);
            }
        }

#if UNITY_EDITOR

        private void OnDrawGizmos()
        {
            Gizmos.color = lightColor;

            Gizmos.DrawIcon(transform.position, "Packages/de.alpacait.dynamiclighting/Gizmos/DynamicLightingPointLight.psd", true, lightColor);
        }

#endif
    }
}