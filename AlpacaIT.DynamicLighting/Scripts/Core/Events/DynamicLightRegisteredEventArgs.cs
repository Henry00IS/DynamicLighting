using System;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>
    /// Contains event data for the <see cref="DynamicLightManager.lightRegistered"/> event.
    /// </summary>
    public class DynamicLightRegisteredEventArgs : EventArgs
    {
        /// <summary>The dynamic light that has been registered (i.e. enabled).</summary>
        public DynamicLight dynamicLight { get; }

        /// <summary>Creates a new instance of the <see cref="DynamicLightRegisteredEventArgs"/>.</summary>
        /// <param name="dynamicLight">The dynamic light that has been registered.</param>
        public DynamicLightRegisteredEventArgs(DynamicLight dynamicLight)
        {
            this.dynamicLight = dynamicLight;
        }
    }
}