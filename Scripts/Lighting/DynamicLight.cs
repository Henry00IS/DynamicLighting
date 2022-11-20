using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
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

        public DynamicLightEffect lightEffect = DynamicLightEffect.Steady;
        public float lightEffectPulseSpeed = 10.0f;
        public float lightEffectPulseModifier = 0.25f;

        public bool realtime { get => lightChannel == 32; }

        /// <summary>Internal variable for <see cref="DynamicLightManager"/> do not use.</summary>
        internal float dlmTime;

        /// <summary>
        /// Internal variable for <see cref="DynamicLightManager"/> do not use.
        /// <para>
        /// The fadeout time until the light gets disabled due to budgeting. This is reset to a
        /// future time whenever it's in budget.
        /// </para>
        /// </summary>
        internal float dlmFadeoutTime;

        internal bool dlmBusy => dlmFadeoutTime - Time.time > 0f;

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