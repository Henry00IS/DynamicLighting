using System;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>
    /// Controls aspects of the raytracing process, such as skipping certain computations.
    /// </summary>
    [Flags]
    public enum DynamicLightingTracerFlags
    {
        /// <summary>Compute everything (default behavior).</summary>
        None = 0,

        /// <summary>Skip bounce lighting during the tracing of the scene.</summary>
        SkipBounceLighting = 1 << 0, // 1

        /// <summary>Skip all optional computations.</summary>
        SkipAll = SkipBounceLighting,
    }
}