using System;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>
    /// Contains event data for the <see cref="DynamicLightManager.objectUnregistered"/> event.
    /// </summary>
    public class DynamicObjectUnregisteredEventArgs : EventArgs
    {
        /// <summary>The dynamic object that has been unregistered (i.e. disabled).</summary>
        public DynamicLightingReceiver dynamicObject { get; }

        /// <summary>Creates a new instance of the <see cref="DynamicObjectUnregisteredEventArgs"/>.</summary>
        /// <param name="dynamicObject">The dynamic object that has been unregistered.</param>
        public DynamicObjectUnregisteredEventArgs(DynamicLightingReceiver dynamicObject)
        {
            this.dynamicObject = dynamicObject;
        }
    }
}