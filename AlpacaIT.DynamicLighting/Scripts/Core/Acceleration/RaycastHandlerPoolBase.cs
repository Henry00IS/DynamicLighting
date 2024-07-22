namespace AlpacaIT.DynamicLighting
{
    /// <summary>
    /// Object pool for <see cref="RaycastHandler"/> to recycle instances and prevent the garbage
    /// collector from cleaning up a lot of instances.
    /// </summary>
    internal abstract class RaycastHandlerPoolBase
    {
        /// <summary>Returns the <paramref name="handler"/> back to the <see cref="RaycastHandlerPool{T}"/>.</summary>
        /// <param name="handler">The handler to be returned to the pool.</param>
        internal abstract void ReturnToPool(RaycastHandler handler);
    }
}