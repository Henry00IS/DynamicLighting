using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    public class RealtimePointLight : MonoBehaviour, IDynamicLight
    {
        public Color lightColor = Color.white;
        public float lightIntensity = 4.0f;
        public float lightRadius = 2.0f;

        public LightType lightType = LightType.Steady;
        public float lightTypePulseSpeed = 10.0f;
        [Range(0f, 1f)]
        public float lightTypePulseModifier = 0.25f;

        Vector3 IDynamicLight.position { get => transform.position; }
        Color IDynamicLight.color { get => lightColor; set => lightColor = value; }
        float IDynamicLight.intensity { get => lightIntensity; set => lightIntensity = value; }
        float IDynamicLight.radius { get => lightRadius; set => lightRadius = value; }
        LightType IDynamicLight.lightType { get => lightType; set => lightType = value; }
        float IDynamicLight.lightTypePulseSpeed { get => lightTypePulseSpeed; set => lightTypePulseSpeed = value; }
        float IDynamicLight.lightTypePulseModifier { get => lightTypePulseModifier; set => lightTypePulseModifier = value; }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = lightColor;

            Gizmos.DrawIcon(transform.position, "Packages/de.alpacait.dynamiclighting/Gizmos/DynamicLightingPointLight.psd", true, lightColor);
        }
#endif
    }
}