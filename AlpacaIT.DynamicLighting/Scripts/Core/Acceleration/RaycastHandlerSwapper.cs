namespace AlpacaIT.DynamicLighting
{
    /// <summary>
    /// Takes two <see cref="RaycastHandler[]"/> and swaps them on demand. This is used instead of
    /// copying the accumulator into a secondary array.
    /// </summary>
    internal unsafe class RaycastHandlerSwapper
    {
        public RaycastHandler[] a;
        public RaycastHandler[] b;

        public RaycastHandlerSwapper(RaycastHandler[] a, RaycastHandler[] b)
        {
            this.a = a;
            this.b = b;
        }

        public void Swap()
        {
            (b, a) = (a, b);
        }
    }
}