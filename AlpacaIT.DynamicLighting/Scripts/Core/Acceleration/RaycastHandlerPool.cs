using System.Collections.Generic;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>
    /// Object pool for <see cref="RaycastHandler"/> to recycle the <typeparamref name="T"/>
    /// instances and prevent the garbage collector from cleaning up a lot of instances.
    /// </summary>
    internal class RaycastHandlerPool<T> : RaycastHandlerPoolBase where T : RaycastHandler, new()
    {
        /// <summary>The queue of available objects that make up the pool.</summary>
        private readonly Queue<T> available;

        /// <summary>Creates a new <typeparamref name="T"/> object pool.</summary>
        /// <param name="capacity">
        /// The total amount of <typeparamref name="T"/> instances to expect in the pool.
        /// </param>
        public RaycastHandlerPool(int capacity)
        {
            available = new Queue<T>(capacity);
        }

        /// <summary>Gets a <typeparamref name="T"/> from the pool in a reset state.</summary>
        /// <returns>The <typeparamref name="T"/> ready for usage.</returns>
        public T GetInstance()
        {
            // try to get an item from the pool.
            if (available.TryDequeue(out var result))
            {
                result.Reset();
                return result;
            }

            // we ran out of items so create a new one.
            var instance = new T();
            instance.pool = this;
            instance.Reset();
            return instance;
        }

        internal override void ReturnToPool(RaycastHandler handler)
        {
            available.Enqueue((T)handler);
        }
    }
}