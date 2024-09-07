using System;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>
    /// Contains event data for the <see cref="DynamicLightManager.lightRegistered"/> event.
    /// </summary>
    public class DynamicLightRegisteredEventArgs : EventArgs
    {
        /// <summary>The active dynamic light manager instance in the scene.</summary>
        public DynamicLightManager dynamicLightManager { get; }

        /// <summary>The dynamic light that has been registered (i.e. enabled).</summary>
        public DynamicLight dynamicLight { get; }

        /// <summary>Creates a new instance of the <see cref="DynamicLightRegisteredEventArgs"/>.</summary>
        /// <param name="dynamicLightManager">The dynamic light manager in the scene.</param>
        /// <param name="dynamicLight">The dynamic light that has been registered.</param>
        public DynamicLightRegisteredEventArgs(DynamicLightManager dynamicLightManager, DynamicLight dynamicLight)
        {
            this.dynamicLightManager = dynamicLightManager;
            this.dynamicLight = dynamicLight;
        }
    }
}