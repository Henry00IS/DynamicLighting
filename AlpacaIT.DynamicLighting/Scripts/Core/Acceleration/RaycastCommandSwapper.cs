using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using UnityEngine;
using System.Runtime.CompilerServices;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>
    /// Takes two unmanaged pointers and swaps them on demand. This is used instead of copying the
    /// accumulator into a secondary array.
    /// </summary>
    internal unsafe class RaycastCommandSwapper
    {
        public NativeArray<RaycastCommand> a;
        public RaycastCommand* aPtr;
        public NativeArray<RaycastCommand> b;
        public RaycastCommand* bPtr;

        public RaycastCommandSwapper(NativeArray<RaycastCommand> a, NativeArray<RaycastCommand> b)
        {
            this.a = a;
            aPtr = (RaycastCommand*)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(a);
            this.b = b;
            bPtr = (RaycastCommand*)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(b);
        }

        public void Swap()
        {
            (b, a) = (a, b);

            RaycastCommand* cPtr = aPtr;
            aPtr = bPtr;
            bPtr = cPtr;
        }
    }
}