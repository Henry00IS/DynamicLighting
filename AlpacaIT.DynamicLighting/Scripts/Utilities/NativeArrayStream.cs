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
using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>Specialized <see cref="Stream"/> that uses native memory <see cref="NativeArray{T}"/>.</summary>
    /// <typeparam name="T">The type stored in the <see cref="NativeArray{T}"/>.</typeparam>
    internal unsafe class NativeArrayStream<T> : Stream where T : struct
    {
        /// <summary>The native memory storing the stream data.</summary>
        private readonly NativeArray<T> buffer;
        /// <summary>The native memory pointer into <see cref="buffer"/>.</summary>
        private readonly byte* bufferPtr;
        /// <summary>The length of <see cref="buffer"/> in bytes.</summary>
        public readonly int length;
        /// <summary>The current position pointer in the stream.</summary>
        private int position;

        /// <summary>Creates a new instance of <see cref="NativeArrayStream{T}"/>.</summary>
        /// <param name="length">The length of the stream in bytes(!).</param>
        /// <param name="allocator">The native allocator type.</param>
        public NativeArrayStream(int length, Allocator allocator = Allocator.Temp)
        {
            var sizeOfT = System.Runtime.InteropServices.Marshal.SizeOf(typeof(T));
            this.length = length;
            if ((length % sizeOfT) != 0) throw new ArgumentOutOfRangeException(nameof(length), "The length must be a multiple of <T> (" + sizeOfT + ")");
            buffer = new NativeArray<T>(length / sizeOfT, allocator);
            bufferPtr = (byte*)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(buffer);
            position = 0;
        }

        public NativeArrayStream(T[] array, Allocator allocator = Allocator.Temp)
        {
            var sizeOfT = System.Runtime.InteropServices.Marshal.SizeOf(typeof(T));
            this.length = array.Length * sizeOfT;
            if ((length % sizeOfT) != 0) throw new ArgumentOutOfRangeException(nameof(length), "The length must be a multiple of <T> (" + sizeOfT + ")");
            buffer = new NativeArray<T>(array, allocator);
            bufferPtr = (byte*)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(buffer);
            position = 0;
        }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => true;

        public override long Length => length;

        public override long Position
        {
            get => position;
            set
            {
                int location = (int)value;
                if (location < 0)
                    throw new ArgumentOutOfRangeException("The position is outside of the stream.");
                if (location >= length)
                    throw new ArgumentOutOfRangeException("The position is outside of the stream.");
                position = location;
            }
        }

        public override void Flush()
        {
            // there is nothing to be flushed.
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (!this.buffer.IsCreated) throw new ObjectDisposedException(nameof(NativeArrayStream<T>), "The stream has already been disposed.");

            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset), "Non-negative number required.");
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "Non-negative number required.");
            if (buffer.Length - offset < count) throw new ArgumentException("Invalid offset and count relative to buffer length.");
            if (count == 0) return 0; // caller did not wish to read anything.

            // the amount of bytes that can still be read from this stream.
            int bytesRemainingInStream = length - position;
            if (bytesRemainingInStream <= 0) return 0; // reached the end of the stream.

            // either the amount the caller requested or the bytes remaining.
            int bytesRead = Mathf.Min(bytesRemainingInStream, count);

            // source: native memory plus the current stream position.
            // destination: target buffer plus their desired offset.
            byte* srcPtr = bufferPtr + position;
            fixed (byte* destPtr = &buffer[offset])
            {
                UnsafeUtility.MemCpy(destPtr, srcPtr, bytesRead);
            }

            // forward the stream position by the bytes read.
            position += bytesRead;
            return bytesRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            int location;

            switch (origin)
            {
                case SeekOrigin.Begin:
                    location = (int)offset;
                    break;

                case SeekOrigin.Current:
                    location = position + (int)offset;
                    break;

                case SeekOrigin.End:
                    location = length + (int)offset;
                    break;

                default: throw new ArgumentException("Invalid '" + nameof(SeekOrigin) + "' value.", nameof(origin));
            }

            if (location < 0 || location >= length)
                throw new ArgumentOutOfRangeException(nameof(offset), "Attempted to seek outside the stream bounds.");

            position = location;
            return position;
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (!this.buffer.IsCreated) throw new ObjectDisposedException(nameof(NativeArrayStream<T>), "The stream has already been disposed.");

            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset), "Non-negative number required.");
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "Non-negative number required.");
            if (buffer.Length - offset < count) throw new ArgumentException("Invalid offset and count relative to buffer length.");
            if (count == 0) return; // caller did not wish to write anything.

            // the amount of bytes that can still be written to this stream.
            int bytesRemainingInStream = length - position;
            if (bytesRemainingInStream <= 0 || count > bytesRemainingInStream) throw new EndOfStreamException("No space left in the stream.");

            // source: target buffer plus their desired offset.
            // destination: native memory plus the current stream position.
            byte* destPtr = bufferPtr + position;
            fixed (byte* srcPtr = &buffer[offset])
            {
                UnsafeUtility.MemCpy(destPtr, srcPtr, count);
            }

            // forward the stream position by the bytes written.
            position += count;
        }

        /// <summary>Gets the underlying <see cref="NativeArray{T}"/>.</summary>
        /// <returns>The underlying <see cref="NativeArray{T}"/>.</returns>
        public NativeArray<T> GetNativeArray() => buffer;

        protected override void Dispose(bool disposing)
        {
            if (disposing && buffer.IsCreated)
                buffer.Dispose();
            base.Dispose(disposing);
        }
    }
}