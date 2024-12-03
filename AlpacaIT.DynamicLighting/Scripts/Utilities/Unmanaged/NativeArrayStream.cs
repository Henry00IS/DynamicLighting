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
using System.IO;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>Specialized <see cref="UnmanagedMemoryStream"/> that uses <see cref="NativeArray{T}"/>.</summary>
    /// <typeparam name="T">The type stored in the <see cref="NativeArray{T}"/>.</typeparam>
    internal unsafe class NativeArrayStream<T> : UnmanagedMemoryStream where T : struct
    {
        /// <summary>The native memory storing the stream data.</summary>
        private readonly NativeArray<T> buffer;

        /// <summary>The native memory pointer into <see cref="buffer"/>.</summary>
        private byte* bufferPtr;

        /// <summary>Creates a new instance of <see cref="NativeArrayStream{T}"/>.</summary>
        /// <param name="length">The length of the stream in bytes(!).</param>
        /// <param name="allocator">The native allocator type.</param>
        /// <param name="options">Whether native memory should be cleared or left uninitialized.</param>
        public NativeArrayStream(int length, Allocator allocator = Allocator.Temp, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
        {
            var sizeOfT = UnsafeUtility.SizeOf(typeof(T));
            if ((length % sizeOfT) != 0) throw new ArgumentOutOfRangeException(nameof(length), "The length must be a multiple of <T> (" + sizeOfT + ")");
            buffer = new NativeArray<T>(length / sizeOfT, allocator, options);
            bufferPtr = (byte*)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(buffer);
            Initialize(bufferPtr, length, length, FileAccess.ReadWrite);
        }

        /// <summary>Creates a new instance of <see cref="NativeArrayStream{T}"/>.</summary>
        /// <param name="array">The array of existing data to be copied.</param>
        /// <param name="allocator">The native allocator type.</param>
        public NativeArrayStream(T[] array, Allocator allocator = Allocator.Temp)
        {
            var sizeOfT = UnsafeUtility.SizeOf(typeof(T));
            var length = array.Length * sizeOfT;
            if ((length % sizeOfT) != 0) throw new ArgumentOutOfRangeException(nameof(length), "The length must be a multiple of <T> (" + sizeOfT + ")");
            buffer = new NativeArray<T>(array, allocator);
            bufferPtr = (byte*)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(buffer);
            Initialize(bufferPtr, length, length, FileAccess.ReadWrite);
        }

        /// <summary>
        /// Creates a new instance of <see cref="NativeArrayStream{T}"/>. This constructor uses the
        /// given pointer and length as the stream data.
        /// </summary>
        /// <param name="arrayPtr">The pointer to an array to be wrapped.</param>
        /// <param name="length">The length of the array in bytes(!).</param>
        public NativeArrayStream(void* arrayPtr, int length)
        {
            var sizeOfT = UnsafeUtility.SizeOf(typeof(T));
            if ((length % sizeOfT) != 0) throw new ArgumentOutOfRangeException(nameof(length), "The length must be a multiple of <T> (" + sizeOfT + ")");
            buffer = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(arrayPtr, length, Allocator.None);
            bufferPtr = (byte*)arrayPtr;
            Initialize(bufferPtr, length, length, FileAccess.ReadWrite);
        }

        /// <summary>Gets the underlying <see cref="NativeArray{T}"/>.</summary>
        /// <returns>The underlying <see cref="NativeArray{T}"/>.</returns>
        public NativeArray<T> GetNativeArray() => buffer;

        /// <summary>Gets the pointer to the underlying <see cref="NativeArray{T}"/> data.</summary>
        /// <returns>The pointer to the underlying <see cref="NativeArray{T}"/> data.</returns>
        public byte* GetUnsafePtr() => bufferPtr;

        /// <summary>
        /// The length of the stream in bytes (same as <see cref="UnmanagedMemoryStream.Length"/>
        /// but as an <see cref="int"/> for convenience).
        /// </summary>
        public int length => (int)Length;

        /// <summary>Disposes of the internal native memory if owned by this <see cref="NativeArrayStream{T}"/>.</summary>
        /// <param name="disposing">True when called by user and false when called by finalizer.</param>
        protected override void Dispose(bool disposing)
        {
            if (buffer.IsCreated)
                buffer.Dispose();
            bufferPtr = default;
            base.Dispose(disposing);
        }
    }
}