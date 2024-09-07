using System;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>
    /// Contains event data for the <see cref="DynamicLightManager.traceCancelled"/> event.
    /// </summary>
    public class DynamicLightingTraceCancelledEventArgs : EventArgs
    {
        /// <summary>The active dynamic light manager instance in the scene.</summary>
        public DynamicLightManager dynamicLightManager { get; }

        /// <summary>Creates a new instance of the <see cref="DynamicLightingTraceCancelledEventArgs"/>.</summary>
        /// <param name="dynamicLightManager">The dynamic light manager in the scene.</param>
        public DynamicLightingTraceCancelledEventArgs(DynamicLightManager dynamicLightManager)
        {
            this.dynamicLightManager = dynamicLightManager;
        }
    }
}