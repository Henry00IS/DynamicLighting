using System;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>
    /// Contains event data for the <see cref="DynamicLightManager.traceStarted"/> event.
    /// </summary>
    public class DynamicLightingTraceStartedEventArgs : EventArgs
    {
        /// <summary>The active dynamic light manager instance in the scene.</summary>
        public DynamicLightManager dynamicLightManager { get; }

        /// <summary>Creates a new instance of the <see cref="DynamicLightingTraceStartedEventArgs"/>.</summary>
        /// <param name="dynamicLightManager">The dynamic light manager in the scene.</param>
        public DynamicLightingTraceStartedEventArgs(DynamicLightManager dynamicLightManager)
        {
            this.dynamicLightManager = dynamicLightManager;
        }
    }
}