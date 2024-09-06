using System;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>
    /// Contains event data for the <see cref="DynamicLightManager.objectRegistered"/> event.
    /// </summary>
    public class DynamicObjectRegisteredEventArgs : EventArgs
    {
        /// <summary>The dynamic object that has been registered (i.e. enabled).</summary>
        public DynamicLightingReceiver dynamicObject { get; }

        /// <summary>Creates a new instance of the <see cref="DynamicObjectRegisteredEventArgs"/>.</summary>
        /// <param name="dynamicLight">The dynamic object that has been registered.</param>
        public DynamicObjectRegisteredEventArgs(DynamicLightingReceiver dynamicObject)
        {
            this.dynamicObject = dynamicObject;
        }
    }
}