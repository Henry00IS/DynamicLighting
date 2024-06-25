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
using System.Collections;
using System.Collections.Generic;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>
    /// Manages a compact three-dimensional array of bit values (as seen looking down from above),
    /// which are represented as Booleans, where true indicates that the bit is on (1) and false
    /// indicates the bit is off (0).
    /// </summary>
    public class BitArray3 : IReadOnlyCollection<bool>, ICloneable
    {
        /// <summary>The internal one-dimensional array of bits.</summary>
        private readonly BitArray _Bits;

        /// <summary>The width of the array in bits.</summary>
        private readonly int _Width;

        /// <summary>The height of the array in bits.</summary>
        private readonly int _Height;

        /// <summary>The depth of the array in bits.</summary>
        private readonly int _Depth;

        /// <summary>The width of the array in bits (x-axis).</summary>
        public int Width => _Width;

        /// <summary>The height of the array in bits (y-axis).</summary>
        public int Height => _Height;

        /// <summary>The depth of the array in bits (z-axis).</summary>
        public int Depth => _Depth;

        /// <summary>Creates a new instance of <see cref="BitArray3"/> for cloning.</summary>
        private BitArray3()
        {
            // invalid until private properties are manually assigned.
        }

        /// <summary>
        /// Creates a new instance of <see cref="BitArray3"/> with the specified amount of bits.
        /// </summary>
        /// <param name="width">The amount of bits to be stored in the array horizontally.</param>
        /// <param name="height">The amount of bits to be stored in the array vertically.</param>
        /// <param name="depth">The amount of bits to be stored in the array along the depth.</param>
        public BitArray3(int width, int height, int depth)
        {
            if (width < 0) throw new ArgumentOutOfRangeException(nameof(width), "Non-negative number required.");
            if (height < 0) throw new ArgumentOutOfRangeException(nameof(height), "Non-negative number required.");
            if (depth < 0) throw new ArgumentOutOfRangeException(nameof(depth), "Non-negative number required.");
            _Width = width;
            _Height = height;
            _Depth = depth;

            // create the internal one-dimensional array of bits.
            _Bits = new BitArray(width * height * depth);
        }

        /// <summary>
        /// Creates a new instance of <see cref="BitArray2"/> from an existing <see cref="BitArray"/>.
        /// </summary>
        /// <param name="width">The amount of bits to be stored in the array horizontally.</param>
        /// <param name="height">The amount of bits to be stored in the array vertically.</param>
        /// <param name="depth">The amount of bits to be stored in the array along the depth.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The width, height and depth must match the amount of bits in the given bit array.
        /// </exception>
        public BitArray3(BitArray bits, int width, int height, int depth)
        {
            if (bits == null) throw new ArgumentNullException(nameof(bits));
            if (width < 0) throw new ArgumentOutOfRangeException(nameof(width), "Non-negative number required.");
            if (height < 0) throw new ArgumentOutOfRangeException(nameof(height), "Non-negative number required.");
            if (depth < 0) throw new ArgumentOutOfRangeException(nameof(depth), "Non-negative number required.");
            if (width * height * depth != bits.Length) throw new ArgumentOutOfRangeException("width,height", "The width, height and depth must match the amount of bits in the given bit array.");
            _Width = width;
            _Height = height;
            _Depth = depth;

            // copy the one-dimensional array of bits.
            _Bits = new BitArray(bits);
        }

        /// <summary>
        /// Creates a new instance of <see cref="BitArray3"/> and copies the bits from another <see cref="BitArray3"/>.
        /// </summary>
        /// <param name="original">The bit array to be copied into this new instance.</param>
        public BitArray3(BitArray3 original)
        {
            if (original == null) throw new ArgumentNullException(nameof(original));
            _Bits = original.ToBitArray();
            _Width = original._Width;
            _Height = original._Height;
            _Depth = original._Depth;
        }

        /// <summary>
        /// Creates a new instance of <see cref="BitArray3"/> and copies the bits from the given <see
        /// cref="uint[]"/> array.
        /// </summary>
        /// <param name="original">The array to be copied into this new instance.</param>
        /// <param name="width">The amount of bits to be stored in the array horizontally.</param>
        /// <param name="height">The amount of bits to be stored in the array vertically.</param>
        /// <param name="depth">The amount of bits to be stored in the array along the depth.</param>
        public BitArray3(uint[] original, int width, int height, int depth) : this(new BitArray(original, width * height * depth), width, height, depth)
        {
        }

        /// <summary>
        /// Converts this two-dimensional <see cref="BitArray3"/> into a one-dimensional <see cref="BitArray"/>.
        /// </summary>
        /// <returns>The one-dimensional <see cref="BitArray"/> with a copy of all of the bits.</returns>
        public BitArray ToBitArray() => new BitArray(_Bits);

        /// <summary>Gets or sets a bit at the specified coordinates.</summary>
        /// <param name="x">The X-Coordinate in the array (up to the width).</param>
        /// <param name="y">The Y-Coordinate in the array (up to the height).</param>
        /// <param name="z">The Z-Coordinate in the array (up to the depth).</param>
        /// <returns>True when the bit is on (1) and false when the bit is off (0).</returns>
        public bool this[int x, int y, int z]
        {
            get
            {
                if (x < 0 || x >= _Width || y < 0 || y >= _Height || z < 0 || z >= _Depth) throw new IndexOutOfRangeException("Index was outside the bounds of the array.");
                return _Bits[x + (y * _Width) + (z * _Width * _Height)];
            }
            set
            {
                if (x < 0 || x >= _Width || y < 0 || y >= _Height || z < 0 || z >= _Depth) throw new IndexOutOfRangeException("Index was outside the bounds of the array.");
                _Bits[x + (y * _Width) + (z * _Width * _Height)] = value;
            }
        }

        /// <summary>Retrieves a <see cref="uint[]"/> containing all of the bits in the array.</summary>
        /// <returns>The <see cref="uint[]"/> containing all of the bits.</returns>
        public uint[] ToUInt32Array() => _Bits.ToUInt32Array();

        /// <summary>Retrieves a <see cref="byte[]"/> containing all of the bits in the array.</summary>
        /// <returns>The <see cref="byte[]"/> containing all of the bits.</returns>
        public byte[] ToByteArray() => _Bits.ToByteArray();

        #region IReadOnlyCollection<bool> Implementation

        int IReadOnlyCollection<bool>.Count => _Bits.Length;

        public IEnumerator<bool> GetEnumerator()
        {
            for (int i = 0; i < _Bits.Length; i++)
                yield return _Bits[i];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion IReadOnlyCollection<bool> Implementation

        #region ICloneable Implementation

        public object Clone()
        {
            return new BitArray3(this);
        }

        #endregion ICloneable Implementation
    }
}