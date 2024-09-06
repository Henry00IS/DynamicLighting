using System;
using System.Collections.Generic;
using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>
    /// Dynamic list based on the fixed-length <see cref="ComputeBuffer"/> that automatically grows
    /// and recreates the internal compute buffer.
    /// </summary>
    internal class ComputeBufferList<T> : List<T> where T : struct
    {
        /// <summary>Gets the internal <see cref="ComputeBuffer"/> of this list (read only).</summary>
        public ComputeBuffer buffer;

        /// <summary>The size of one element in the buffer (read only).</summary>
        public readonly int stride;

        /// <summary>The length of the <see cref="buffer"/> (read only).</summary>
        public int length;

        /// <summary>
        /// Creates a new instance of <see cref="ComputeBufferList{T}"/> with an initial capacity.
        /// </summary>
        /// <param name="capacity">
        /// The capacity to reserve on the graphics card. When exceeded, the <see cref="buffer"/>
        /// will be recreated to accomodate the additional items on <see cref="Upload"/>.
        /// </param>
        public ComputeBufferList(int capacity)
        {
            stride = System.Runtime.InteropServices.Marshal.SizeOf(typeof(T));
            length = capacity;
            CreateBuffer();
        }

        /// <summary>Releases the <see cref="buffer"/> on the graphics card.</summary>
        private void ReleaseBuffer()
        {
            if (buffer != null && buffer.IsValid())
                buffer.Release();
            buffer = null;
        }

        /// <summary>Releases the <see cref="buffer"/> and creates a new one of size <see cref="length"/>.</summary>
        private void CreateBuffer()
        {
            ReleaseBuffer();
            buffer = new ComputeBuffer(length, stride, ComputeBufferType.Default);
        }

        /// <summary>Releases the <see cref="ComputeBuffer"/> to prevent a memory leak.</summary>
        public void Release()
        {
            ReleaseBuffer();
        }

        /// <summary>Uploads the current <see cref="ComputeBufferList{T}"/> to the graphics card.</summary>
        /// <returns>True when the <see cref="buffer"/> changed else false.</returns>
        public bool Upload()
        {
            // grow the compute buffer if necessary.
            var count = Count;
            if (count > length)
            {
                length = count;
                CreateBuffer();
                return true;
            }

            // upload the list to the graphics card (data beyond count is stale).
            buffer.SetData(this, 0, 0, count);
            return false;
        }
    }
}