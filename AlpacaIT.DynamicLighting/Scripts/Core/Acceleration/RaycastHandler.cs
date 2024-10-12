namespace AlpacaIT.DynamicLighting
{
    /// <summary>Base class for a <see cref="RaycastProcessor"/> handler.</summary>
    internal abstract class RaycastHandler
    {
        /// <summary>The <see cref="RaycastHandlerPool{T}"/> that owns this <see cref="RaycastHandler"/>.</summary>
        internal RaycastHandlerPoolBase pool;

        /// <summary>Whether this handler is still in the process of being setup (read only).</summary>
        protected bool setup = true;

        /// <summary>The amount of raycasts that this handler is expecting to process (read only).</summary>
        internal int raycastsExpected;

        /// <summary>
        /// The amount of raycasts that this handler has processed (read only).
        /// <para>Simultaneously the index of the current raycast.</para>
        /// </summary>
        protected int raycastsIndex;

        /// <summary>
        /// Gets called whenever a raycast has been processed (after <see cref="OnRaycastMiss"/> and
        /// <see cref="OnRaycastHit"/>.
        /// </summary>
        internal void OnRaycastProcessed()
        {
            raycastsIndex++;
            if (setup) return;
            if (raycastsIndex == raycastsExpected)
            {
                OnHandlerFinished();
                pool?.ReturnToPool(this);
            }
        }

        /// <summary>Gets called when the raycast did not hit anything.</summary>
        public abstract void OnRaycastMiss();

        /// <summary>Gets called when the raycast handler finished.</summary>
        public abstract void OnHandlerFinished();

        /// <summary>Must be called when this handler is ready to begin.</summary>
        public void Ready()
        {
            setup = false;
            if (raycastsIndex == raycastsExpected)
            {
                OnHandlerFinished();
                pool?.ReturnToPool(this);
            }
        }

        /// <summary>Resets the raycast handler so that it can be recycled.</summary>
        internal void Reset()
        {
            setup = true;
            raycastsExpected = 0;
            raycastsIndex = 0;
        }
    }
}