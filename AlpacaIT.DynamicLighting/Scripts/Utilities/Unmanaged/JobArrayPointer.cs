///////////////////////////////////////////////////////////////////////////////////////////////////
// MIT License
//
// Copyright(c) 2024 Henry de Jongh
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
////////////////////// https://github.com/Henry00IS/CSharp ////////// http://00laboratories.com/ //

using System;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>
    /// Pointer to an array (managed or unmanaged) along with the length, that can be passed into
    /// jobs. Managed arrays are automatically pinned until this struct is disposed.
    /// <para>Warning: Must be disposed with <see cref="Dispose"/> or 'using' statement.</para>
    /// </summary>
    internal unsafe struct JobArrayPointer : IDisposable
    {
        /// <summary>The pointer to the array data (read only).</summary>
        [NativeDisableUnsafePtrRestriction]
        public void* pointer;

        /// <summary>The length of the array (read only).</summary>
        public int length;

        /// <summary>Whether <see cref="gcHandle"/> is used to pin managed memory.</summary>
        private bool gcHandleUsed;

        /// <summary>The <see cref="GCHandle"/> pinning managed memory.</summary>
        private GCHandle gcHandle;

        /// <summary>Creates a new instance of <see cref="JobArrayPointer"/>.</summary>
        /// <param name="pointer">The pointer to the array data.</param>
        /// <param name="length">The length of the array.</param>
        public JobArrayPointer(void* pointer, int length)
        {
            this.pointer = pointer;
            this.length = length;
            gcHandleUsed = default;
            gcHandle = default;
        }

        /// <summary>
        /// Creates a new <see cref="JobArrayPointer"/> for the given array.
        /// <para>Warning: Must be disposed with <see cref="Dispose"/> or 'using' statement.</para>
        /// </summary>
        /// <param name="array">The array to be passed to a job.</param>
        /// <returns>The pointer to the array to be passed to a job.</returns>
        public static JobArrayPointer Create(Array array)
        {
            // pin the memory so that the garbage collector does not move/free it.
            var gcHandle = GCHandle.Alloc(array, GCHandleType.Pinned);

            return new JobArrayPointer(gcHandle.AddrOfPinnedObject().ToPointer(), array.Length)
            {
                gcHandle = gcHandle,
                gcHandleUsed = true,
            };
        }

        /// <summary>Creates a new <see cref="JobArrayPointer"/> for the given native array.</summary>
        /// <param name="nativeArrayStream">The native array to be passed to a job.</param>
        /// <returns>The pointer to the array to be passed to a job.</returns>
        public static JobArrayPointer Create<T>(NativeArrayStream<T> nativeArrayStream) where T : struct
        {
            return new JobArrayPointer(nativeArrayStream.GetUnsafePtr(), nativeArrayStream.length);
        }

        /// <summary>Disposes of all pinned managed memory used by this instance.</summary>
        public void Dispose()
        {
            // free the managed memory so that the garbage collector can move/free it.
            if (gcHandleUsed)
                gcHandle.Free();

            // reset the fields on this struct even though copies may still have them set.
            pointer = default;
            length = default;
            gcHandleUsed = default;
            gcHandle = default;
        }
    }
}