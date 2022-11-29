using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    [ExecuteInEditMode]
    public class DynamicLight : MonoBehaviour
    {
        public DynamicLightType lightType = DynamicLightType.Point;

        public Color lightColor = Color.white;
        public float lightIntensity = 2.0f;
        public float lightRadius = 4.0f;
        public uint lightChannel = 0;
        [Range(0f, 180f)]
        public float lightCutoff = 26.0f;
        [Range(0f, 180f)]
        public float lightOuterCutoff = 30.0f;

        public bool lightWaterShimmer = false;

        public DynamicLightEffect lightEffect = DynamicLightEffect.Steady;
        public float lightEffectPulseSpeed = 10.0f;
        public float lightEffectPulseModifier = 0.25f;

        public bool realtime { get => lightChannel == 32; }

        private void OnEnable()
        {
            DynamicLightManager.Instance.RegisterDynamicLight(this);
        }

        private void OnDisable()
        {
            if (DynamicLightManager.hasInstance)
                DynamicLightManager.Instance.UnregisterDynamicLight(this);
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