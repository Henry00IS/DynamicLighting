

using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    public interface IDynamicLight
    {
        /// <summary>Gets the position of the light in world space.</summary>
        public Vector3 position { get; }
        /// <summary>Gets or sets the Red, Green, Blue color of the light in that order.</summary>
        public Color color { get; set; }
        /// <summary>Gets or sets the intensity (or brightness) of the light.</summary>
        public float intensity { get; set; }
        /// <summary>Gets or sets the maximum cutoff radius where the light is guaranteed to end.</summary>
        public float radius { get; set; }
        /// <summary>Gets or sets the effect applied to the light.</summary>
        public LightType lightType { get; set; }
        /// <summary>Gets or sets the pulse light effect speed.</summary>
        public float lightTypePulseSpeed { get; set; }
        /// <summary>Gets or sets the pulse light effect modifier.</summary>
        public float lightTypePulseModifier { get; set; }
    }
}
