using System;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>
    /// Contains event data for the <see cref="DynamicLightManager.lightUnregistered"/> event.
    /// </summary>
    public class DynamicLightUnregisteredEventArgs : EventArgs
    {
        /// <summary>The dynamic light that has been unregistered (i.e. disabled).</summary>
        public DynamicLight dynamicLight { get; }

        /// <summary>Creates a new instance of the <see cref="DynamicLightUnregisteredEventArgs"/>.</summary>
        /// <param name="dynamicLight">The dynamic light that has been unregistered.</param>
        public DynamicLightUnregisteredEventArgs(DynamicLight dynamicLight)
        {
            this.dynamicLight = dynamicLight;
        }
    }
}