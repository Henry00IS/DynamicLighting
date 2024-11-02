using System;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>
    /// Contains event data for the <see cref="DynamicLightManager.lightingDeleted"/> event.
    /// </summary>
    public class DynamicLightingDeletedEventArgs : EventArgs
    {
        /// <summary>The active dynamic light manager instance in the scene.</summary>
        public DynamicLightManager dynamicLightManager { get; }

        /// <summary>Creates a new instance of the <see cref="DynamicLightingDeletedEventArgs"/>.</summary>
        /// <param name="dynamicLightManager">The dynamic light manager in the scene.</param>
        public DynamicLightingDeletedEventArgs(DynamicLightManager dynamicLightManager)
        {
            this.dynamicLightManager = dynamicLightManager;
        }
    }
}