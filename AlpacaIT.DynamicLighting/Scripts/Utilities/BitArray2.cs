///////////////////////////////////////////////////////////////////////////////////////////////////
// MIT License
//
// Copyright(c) 2023 Henry de Jongh
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
using System.Text;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>
    /// Manages a compact two-dimensional array of bit values, which are represented as Booleans,
    /// where true indicates that the bit is on (1) and false indicates the bit is off (0).
    /// </summary>
    internal class BitArray2 : IReadOnlyCollection<bool>, ICloneable
    {
        /// <summary>The internal one-dimensional array of bits.</summary>
        private readonly BitArray _Bits;

        /// <summary>The width of the array in bits.</summary>
        public readonly int Width;

        /// <summary>The height of the array in bits.</summary>
        public readonly int Height;

        /// <summary>Creates a new instance of <see cref="BitArray2"/> for cloning.</summary>
        private BitArray2()
        {
            // invalid until private properties are manually assigned.
        }

        /// <summary>
        /// Creates a new instance of <see cref="BitArray2"/> with the specified amount of bits.
        /// </summary>
        /// <param name="width">The amount of bits to be stored in the array horizontally.</param>
        /// <param name="height">The amount of bits to be stored in the array vertically.</param>
        public BitArray2(int width, int height)
        {
            if (width < 0) throw new ArgumentOutOfRangeException(nameof(width), "Non-negative number required.");
            if (height < 0) throw new ArgumentOutOfRangeException(nameof(height), "Non-negative number required.");
            Width = width;
            Height = height;

            // create the internal one-dimensional array of bits.
            _Bits = new BitArray(width * height);
        }

        /// <summary>
        /// Creates a new instance of <see cref="BitArray2"/> from an existing <see cref="BitArray"/>.
        /// </summary>
        /// <param name="width">The amount of bits to be stored in the array horizontally.</param>
        /// <param name="height">The amount of bits to be stored in the array vertically.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The width and height must match the amount of bits in the given bit array.
        /// </exception>
        public BitArray2(BitArray bits, int width, int height)
        {
            if (bits == null) throw new ArgumentNullException(nameof(bits));
            if (width < 0) throw new ArgumentOutOfRangeException(nameof(width), "Non-negative number required.");
            if (height < 0) throw new ArgumentOutOfRangeException(nameof(height), "Non-negative number required.");
            if (width * height != bits.Length) throw new ArgumentOutOfRangeException("width,height", "The width and height must match the amount of bits in the given bit array.");
            Width = width;
            Height = height;

            // copy the one-dimensional array of bits.
            _Bits = new BitArray(bits);
        }

        /// <summary>
        /// Creates a new instance of <see cref="BitArray2"/> and copies the bits from another <see cref="BitArray2"/>.
        /// </summary>
        /// <param name="original">The bit array to be copied into this new instance.</param>
        public BitArray2(BitArray2 original)
        {
            if (original == null) throw new ArgumentNullException(nameof(original));
            _Bits = original.ToBitArray();
            Width = original.Width;
            Height = original.Height;
        }

        /// <summary>
        /// Creates a new instance of <see cref="BitArray2"/> and copies the bits from the given <see
        /// cref="uint[]"/> array.
        /// </summary>
        /// <param name="original">The array to be copied into this new instance.</param>
        /// <param name="width">The amount of bits to be stored in the array horizontally.</param>
        /// <param name="height">The amount of bits to be stored in the array vertically.</param>
        public BitArray2(uint[] original, int width, int height) : this(new BitArray(original, width * height), width, height)
        {
        }

        /// <summary>
        /// Converts this two-dimensional <see cref="BitArray2"/> into a one-dimensional <see cref="BitArray"/>.
        /// </summary>
        /// <returns>The one-dimensional <see cref="BitArray"/> with a copy of all of the bits.</returns>
        public BitArray ToBitArray() => new BitArray(_Bits);

        /// <summary>Gets or sets a bit at the specified coordinates.</summary>
        /// <param name="x">The X-Coordinate in the array (up to the width).</param>
        /// <param name="y">The Y-Coordinate in the array (up to the height).</param>
        /// <returns>True when the bit is on (1) and false when the bit is off (0).</returns>
        public bool this[int x, int y]
        {
            get
            {
                if (x < 0 || x >= Width || y < 0 || y >= Height) throw new IndexOutOfRangeException("Index was outside the bounds of the array.");
                return _Bits[x + y * Width];
            }
            set
            {
                if (x < 0 || x >= Width || y < 0 || y >= Height) throw new IndexOutOfRangeException("Index was outside the bounds of the array.");
                _Bits[x + y * Width] = value;
            }
        }

        /// <summary>Retrieves a <see cref="uint[]"/> containing all of the bits in the array.</summary>
        /// <returns>The <see cref="uint[]"/> containing all of the bits.</returns>
        public uint[] ToUInt32Array() => _Bits.ToUInt32Array();

        /// <summary>Retrieves a <see cref="byte[]"/> containing all of the bits in the array.</summary>
        /// <returns>The <see cref="byte[]"/> containing all of the bits.</returns>
        public byte[] ToByteArray() => _Bits.ToByteArray();

        /// <summary>
        /// Returns a string that represents the current object. The two-dimensional grid of bits.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            var nl = Environment.NewLine;
            var sb = new StringBuilder(_Bits.Length + Height * nl.Length);

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                    sb.Append(this[x, y] ? "1" : "0");
                sb.Append(nl);
            }
            sb.Remove(sb.Length - nl.Length, nl.Length);

            return sb.ToString();
        }

        #region Bit Operators

        /// <summary>Sets all bits in the <see cref="BitArray2"/> to the specified value.</summary>
        /// <param name="value">The boolean value to assign to all bits.</param>
        public void SetAll(bool value) => _Bits.SetAll(value);

        /// <summary>
        /// Inverts all the bit values in the <see cref="BitArray2"/>, so that bits set to true 1 are
        /// changed to false 0, and bits set to false 0 are changed to true 1.
        /// </summary>
        public void Not() => _Bits.Not();

        /// <summary>
        /// Performs the bitwise AND operation between the bits of the current <see
        /// cref="BitArray2"/> object and the corresponding bits in the specified array. The current
        /// <see cref="BitArray2"/> object will be modified to store the result of the bitwise AND operation.
        /// </summary>
        /// <param name="value">The array with which to perform the bitwise AND operation.</param>
        /// <exception cref="ArgumentNullException">Value is null.</exception>
        /// <exception cref="ArgumentException">
        /// The value and the current <see cref="BitArray2"/> do not have the same number of bits.
        /// </exception>
        public void And(BitArray2 value) => _Bits.And(value._Bits);

        /// <summary>
        /// Performs the bitwise OR operation between the bits of the current <see
        /// cref="BitArray2"/> object and the corresponding bits in the specified array. The current
        /// <see cref="BitArray2"/> object will be modified to store the result of the bitwise OR operation.
        /// </summary>
        /// <param name="value">The array with which to perform the bitwise OR operation.</param>
        /// <exception cref="ArgumentNullException">Value is null.</exception>
        /// <exception cref="ArgumentException">
        /// The value and the current <see cref="BitArray2"/> do not have the same number of bits.
        /// </exception>
        public void Or(BitArray2 value) => _Bits.Or(value._Bits);

        /// <summary>
        /// Performs the bitwise XOR operation between the bits of the current <see
        /// cref="BitArray2"/> object and the corresponding bits in the specified array. The current
        /// <see cref="BitArray2"/> object will be modified to store the result of the bitwise XOR operation.
        /// </summary>
        /// <param name="value">The array with which to perform the bitwise XOR operation.</param>
        /// <exception cref="ArgumentNullException">Value is null.</exception>
        /// <exception cref="ArgumentException">
        /// The value and the current <see cref="BitArray2"/> do not have the same number of bits.
        /// </exception>
        public void Xor(BitArray2 value) => _Bits.Xor(value._Bits);

        /// <summary>
        /// Performs the bitwise left shift operation on the bits of the current <see
        /// cref="BitArray2"/> object. The bits that slide off the end disappear and the spaces are
        /// always filled with zeros.
        /// </summary>
        /// <param name="amount">How many positions to shift the bits by.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The <paramref name="amount"/> can not be a negative number.
        /// </exception>
        public void Shl(int amount) => _Bits.Shl(amount);

        /// <summary>
        /// Performs the bitwise right shift operation on the bits of the current <see
        /// cref="BitArray2"/> object. The bits that slide off the end disappear and the spaces are
        /// always filled with zeros.
        /// </summary>
        /// <param name="amount">How many positions to shift the bits by.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The <paramref name="amount"/> can not be a negative number.
        /// </exception>
        public void Shr(int amount) => _Bits.Shr(amount);

        /// <summary>
        /// Performs the bitwise left rotate operation on the bits of the current <see
        /// cref="BitArray2"/> object. The bits that slide off the end are fed back into spaces.
        /// </summary>
        /// <param name="amount">How many positions to rotate the bits by.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The <paramref name="amount"/> can not be a negative number.
        /// </exception>
        public void Rol(int amount) => _Bits.Rol(amount);

        /// <summary>
        /// Performs the bitwise right rotate operation on the bits of the current <see
        /// cref="BitArray2"/> object. The bits that slide off the end are fed back into spaces.
        /// </summary>
        /// <param name="amount">How many positions to rotate the bits by.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The <paramref name="amount"/> can not be a negative number.
        /// </exception>
        public void Ror(int amount) => _Bits.Ror(amount);

        /// <summary>Checks whether all bits in the bit array are zero.</summary>
        /// <returns>True when all bits are zero else false.</returns>
        public bool IsZero() => _Bits.IsZero();

        #endregion Bit Operators

        #region Drawing Plotters

        /// <summary>
        /// Plots a line using Bresenham's line algorithm setting the bits to <paramref name="value"/>.
        /// <para>The plotting coordinates can be positioned out of bounds.</para>
        /// </summary>
        /// <param name="x1">The start x-position in the two-dimensional array of bit values.</param>
        /// <param name="y1">The start y-position in the two-dimensional array of bit values.</param>
        /// <param name="x2">The end x-position in the two-dimensional array of bit values.</param>
        /// <param name="y2">The end y-position in the two-dimensional array of bit values.</param>
        /// <param name="value">The boolean value to assign to all bits on the line.</param>
        public void PlotLine(int x1, int y1, int x2, int y2, bool value = true)
        {
            var dx = Math.Abs(x2 - x1);
            var sx = x1 < x2 ? 1 : -1;
            var dy = -Math.Abs(y2 - y1);
            var sy = y1 < y2 ? 1 : -1;
            var error = dx + dy;

            while (true)
            {
                if (x1 >= 0 && x1 < Width && y1 >= 0 && y1 < Height)
                    this[x1, y1] = value;
                if (x1 == x2 && y1 == y2) return;
                var e2 = 2 * error;
                if (e2 >= dy)
                {
                    if (x1 == x2) return;
                    error += dy;
                    x1 += sx;
                }
                if (e2 <= dx)
                {
                    if (y1 == y2) return;
                    error += dx;
                    y1 += sy;
                }
            }
        }

        /// <summary>
        /// Plots a dotted line using Bresenham's line algorithm setting the bits to <paramref name="value"/>.
        /// <para>
        /// This method skips every other bit, according to a global X pattern with the top-left bit
        /// of the global <see cref="BitArray2"/> beginning with 1 to draw dotted.
        /// </para>
        /// <para>The plotting coordinates can be positioned out of bounds.</para>
        /// <code>
        ///1 0 1 0  ->  1 0 1 0    1 0 0 0    1 0 0 0
        ///0 1 0 1      0 0 0 0    0 0 0 0    0 1 0 0
        ///1 0 1 0      0 0 0 0    1 0 0 0    0 0 1 0
        ///0 1 0 1      0 0 0 0    0 0 0 0    0 0 0 1
        /// </code>
        /// </summary>
        /// <param name="x1">The start x-position in the two-dimensional array of bit values.</param>
        /// <param name="y1">The start y-position in the two-dimensional array of bit values.</param>
        /// <param name="x2">The end x-position in the two-dimensional array of bit values.</param>
        /// <param name="y2">The end y-position in the two-dimensional array of bit values.</param>
        /// <param name="value">The boolean value to assign to all bits on the line.</param>
        public void PlotDottedLine(int x1, int y1, int x2, int y2, bool value = true)
        {
            var dx = Math.Abs(x2 - x1);
            var sx = x1 < x2 ? 1 : -1;
            var dy = -Math.Abs(y2 - y1);
            var sy = y1 < y2 ? 1 : -1;
            var error = dx + dy;

            while (true)
            {
                var yoff = y1 % 2;
                var xoff = (x1 + 1) % 2;
                if (xoff + yoff == 1 && x1 >= 0 && x1 < Width && y1 >= 0 && y1 < Height)
                    this[x1, y1] = value;
                if (x1 == x2 && y1 == y2) return;
                var e2 = 2 * error;
                if (e2 >= dy)
                {
                    if (x1 == x2) return;
                    error += dy;
                    x1 += sx;
                }
                if (e2 <= dx)
                {
                    if (y1 == y2) return;
                    error += dx;
                    y1 += sy;
                }
            }
        }

        /// <summary>
        /// Plots a rectangle (outline) setting the bits to <paramref name="value"/>.
        /// <para>The plotting coordinates can be positioned out of bounds.</para>
        /// </summary>
        /// <param name="x1">The start x-position in the two-dimensional array of bit values.</param>
        /// <param name="y1">The start y-position in the two-dimensional array of bit values.</param>
        /// <param name="x2">The end x-position in the two-dimensional array of bit values.</param>
        /// <param name="y2">The end y-position in the two-dimensional array of bit values.</param>
        /// <param name="value">The boolean value to assign to all bits on the rectangle.</param>
        public void PlotRectangle(int x1, int y1, int x2, int y2, bool value = true)
        {
            PlotLine(x1, y1, x2, y1, value);
            PlotLine(x1, y2, x2, y2, value);
            PlotLine(x1, y1, x1, y2, value);
            PlotLine(x2, y1, x2, y2, value);
        }

        /// <summary>
        /// Plots a dotted rectangle (outline) setting the bits to <paramref name="value"/>.
        /// <para>
        /// This method skips every other bit, according to a global X pattern with the top-left bit
        /// of the global <see cref="BitArray2"/> beginning with 1 to draw dotted.
        /// </para>
        /// <para>The plotting coordinates can be positioned out of bounds.</para>
        /// <code>
        ///1 0 1 0  ->  1 0 1 0
        ///0 1 0 1      0 0 0 1
        ///1 0 1 0      1 0 0 0
        ///0 1 0 1      0 1 0 1
        /// </code>
        /// </summary>
        /// <param name="x1">The start x-position in the two-dimensional array of bit values.</param>
        /// <param name="y1">The start y-position in the two-dimensional array of bit values.</param>
        /// <param name="x2">The end x-position in the two-dimensional array of bit values.</param>
        /// <param name="y2">The end y-position in the two-dimensional array of bit values.</param>
        /// <param name="value">The boolean value to assign to all bits on the rectangle.</param>
        public void PlotDottedRectangle(int x1, int y1, int x2, int y2, bool value = true)
        {
            PlotDottedLine(x1, y1, x2, y1, value);
            PlotDottedLine(x1, y2, x2, y2, value);
            PlotDottedLine(x1, y1, x1, y2, value);
            PlotDottedLine(x2, y1, x2, y2, value);
        }

        /// <summary>
        /// Plots a triangle (outline) setting the bits to <paramref name="value"/>.
        /// <para>The plotting coordinates can be positioned out of bounds.</para>
        /// </summary>
        /// <param name="x1">The first x-position in the two-dimensional array of bit values.</param>
        /// <param name="y1">The first y-position in the two-dimensional array of bit values.</param>
        /// <param name="x2">The second x-position in the two-dimensional array of bit values.</param>
        /// <param name="y2">The second y-position in the two-dimensional array of bit values.</param>
        /// <param name="x3">The third x-position in the two-dimensional array of bit values.</param>
        /// <param name="y3">The third y-position in the two-dimensional array of bit values.</param>
        /// <param name="value">The boolean value to assign to all bits on the triangle.</param>
        public void PlotTriangle(int x1, int y1, int x2, int y2, int x3, int y3, bool value = true)
        {
            PlotLine(x1, y1, x2, y2, value);
            PlotLine(x2, y2, x3, y3, value);
            PlotLine(x3, y3, x1, y1, value);
        }

        /// <summary>
        /// Plots a dotted triangle (outline) setting the bits to <paramref name="value"/>.
        /// <para>
        /// This method skips every other bit, according to a global X pattern with the top-left bit
        /// of the global <see cref="BitArray2"/> beginning with 1 to draw dotted.
        /// </para>
        /// <para>The plotting coordinates can be positioned out of bounds.</para>
        /// <code>
        ///1 0 1 0  ->  1 0 0 0
        ///0 1 0 1      0 1 0 0
        ///1 0 1 0      1 0 1 0
        ///0 1 0 1      0 1 0 1
        /// </code>
        /// </summary>
        /// <param name="x1">The first x-position in the two-dimensional array of bit values.</param>
        /// <param name="y1">The first y-position in the two-dimensional array of bit values.</param>
        /// <param name="x2">The second x-position in the two-dimensional array of bit values.</param>
        /// <param name="y2">The second y-position in the two-dimensional array of bit values.</param>
        /// <param name="x3">The third x-position in the two-dimensional array of bit values.</param>
        /// <param name="y3">The third y-position in the two-dimensional array of bit values.</param>
        /// <param name="value">The boolean value to assign to all bits on the triangle.</param>
        public void PlotDottedTriangle(int x1, int y1, int x2, int y2, int x3, int y3, bool value = true)
        {
            PlotDottedLine(x1, y1, x2, y2, value);
            PlotDottedLine(x2, y2, x3, y3, value);
            PlotDottedLine(x3, y3, x1, y1, value);
        }

        #endregion Drawing Plotters

        #region Setting and Getting Bytes, Integers and Floats

        /// <summary>Reads 8 bits starting at the specified bit array index as an unsigned byte.</summary>
        /// <param name="x">The X-Coordinate in the bit array to start reading at.</param>
        /// <param name="y">The Y-Coordinate in the bit array to start reading at.</param>
        /// <returns>The 8-bit unsigned integer.</returns>
        public byte GetByte(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) throw new IndexOutOfRangeException("Index was outside the bounds of the array.");
            return _Bits.GetByte(x + y * Width);
        }

        /// <summary>Writes 8 bits starting at the specified bit array index as an unsigned byte.</summary>
        /// <param name="x">The X-Coordinate in the bit array to start writing at.</param>
		/// <param name="y">The Y-Coordinate in the bit array to start writing at.</param>
        /// <param name="value">The 8-bit unsigned integer.</param>
        public void SetByte(int x, int y, byte value)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) throw new IndexOutOfRangeException("Index was outside the bounds of the array.");
            _Bits.SetByte(x + y * Width, value);
        }

        /// <summary>Reads 16 bits starting at the specified bit array index as an unsigned integer.</summary>
        /// <param name="x">The X-Coordinate in the bit array to start reading at.</param>
		/// <param name="y">The Y-Coordinate in the bit array to start reading at.</param>
        /// <returns>The 16-bit unsigned integer.</returns>
        public ushort GetUInt16(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) throw new IndexOutOfRangeException("Index was outside the bounds of the array.");
            return _Bits.GetUInt16(x + y * Width);
        }

        /// <summary>Writes 16 bits starting at the specified bit array index as an unsigned integer.</summary>
        /// <param name="x">The X-Coordinate in the bit array to start writing at.</param>
		/// <param name="y">The Y-Coordinate in the bit array to start writing at.</param>
        /// <param name="value">The 16-bit unsigned integer.</param>
        public void SetUInt16(int x, int y, ushort value)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) throw new IndexOutOfRangeException("Index was outside the bounds of the array.");
            _Bits.SetUInt16(x + y * Width, value);
        }

        /// <summary>Reads 16 bits starting at the specified bit array index as an unsigned integer in big-endian order.</summary>
        /// <param name="x">The X-Coordinate in the bit array to start reading at.</param>
		/// <param name="y">The Y-Coordinate in the bit array to start reading at.</param>
        /// <returns>The 16-bit unsigned integer.</returns>
        public ushort GetUInt16BigEndian(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) throw new IndexOutOfRangeException("Index was outside the bounds of the array.");
            return _Bits.GetUInt16BigEndian(x + y * Width);
        }

        /// <summary>Writes 16 bits starting at the specified bit array index as an unsigned integer in big-endian order.</summary>
        /// <param name="x">The X-Coordinate in the bit array to start writing at.</param>
		/// <param name="y">The Y-Coordinate in the bit array to start writing at.</param>
        /// <param name="value">The 16-bit unsigned integer.</param>
        public void SetUInt16BigEndian(int x, int y, ushort value)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) throw new IndexOutOfRangeException("Index was outside the bounds of the array.");
            _Bits.SetUInt16BigEndian(x + y * Width, value);
        }

        /// <summary>Reads 16 bits starting at the specified bit array index as a signed integer.</summary>
        /// <param name="x">The X-Coordinate in the bit array to start reading at.</param>
		/// <param name="y">The Y-Coordinate in the bit array to start reading at.</param>
        /// <returns>The 16-bit signed integer.</returns>
        public short GetInt16(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) throw new IndexOutOfRangeException("Index was outside the bounds of the array.");
            return _Bits.GetInt16(x + y * Width);
        }

        /// <summary>Writes 16 bits starting at the specified bit array index as a signed integer.</summary>
        /// <param name="x">The X-Coordinate in the bit array to start writing at.</param>
		/// <param name="y">The Y-Coordinate in the bit array to start writing at.</param>
        /// <param name="value">The 16-bit signed integer.</param>
        public void SetInt16(int x, int y, short value)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) throw new IndexOutOfRangeException("Index was outside the bounds of the array.");
            _Bits.SetInt16(x + y * Width, value);
        }

        /// <summary>Reads 16 bits starting at the specified bit array index as a signed integer in big-endian order.</summary>
        /// <param name="x">The X-Coordinate in the bit array to start reading at.</param>
        /// <param name="y">The Y-Coordinate in the bit array to start reading at.</param>
        /// <returns>The 16-bit signed integer.</returns>
        public short GetInt16BigEndian(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) throw new IndexOutOfRangeException("Index was outside the bounds of the array.");
            return _Bits.GetInt16BigEndian(x + y * Width);
        }

        /// <summary>Writes 16 bits starting at the specified bit array index as a signed integer in big-endian order.</summary>
        /// <param name="x">The X-Coordinate in the bit array to start writing at.</param>
		/// <param name="y">The Y-Coordinate in the bit array to start writing at.</param>
        /// <param name="value">The 16-bit signed integer.</param>
        public void SetInt16BigEndian(int x, int y, short value)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) throw new IndexOutOfRangeException("Index was outside the bounds of the array.");
            _Bits.SetInt16BigEndian(x + y * Width, value);
        }

        /// <summary>Reads 32 bits starting at the specified bit array index as an unsigned integer.</summary>
        /// <param name="x">The X-Coordinate in the bit array to start reading at.</param>
		/// <param name="y">The Y-Coordinate in the bit array to start reading at.</param>
        /// <returns>The 32-bit unsigned integer.</returns>
        public uint GetUInt32(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) throw new IndexOutOfRangeException("Index was outside the bounds of the array.");
            return _Bits.GetUInt32(x + y * Width);
        }

        /// <summary>Writes 32 bits starting at the specified bit array index as an unsigned integer.</summary>
        /// <param name="x">The X-Coordinate in the bit array to start writing at.</param>
		/// <param name="y">The Y-Coordinate in the bit array to start writing at.</param>
        /// <param name="value">The 32-bit unsigned integer.</param>
        public void SetUInt32(int x, int y, uint value)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) throw new IndexOutOfRangeException("Index was outside the bounds of the array.");
            _Bits.SetUInt32(x + y * Width, value);
        }

        /// <summary>Reads 32 bits starting at the specified bit array index as an unsigned integer in big-endian order.</summary>
        /// <param name="x">The X-Coordinate in the bit array to start reading at.</param>
		/// <param name="y">The Y-Coordinate in the bit array to start reading at.</param>
        /// <returns>The 32-bit unsigned integer.</returns>
        public uint GetUInt32BigEndian(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) throw new IndexOutOfRangeException("Index was outside the bounds of the array.");
            return _Bits.GetUInt32BigEndian(x + y * Width);
        }

        /// <summary>Writes 32 bits starting at the specified bit array index as an unsigned integer in big-endian order.</summary>
        /// <param name="x">The X-Coordinate in the bit array to start writing at.</param>
		/// <param name="y">The Y-Coordinate in the bit array to start writing at.</param>
        /// <param name="value">The 32-bit unsigned integer.</param>
        public void SetUInt32BigEndian(int x, int y, uint value)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) throw new IndexOutOfRangeException("Index was outside the bounds of the array.");
            _Bits.SetUInt32BigEndian(x + y * Width, value);
        }

        /// <summary>Reads 32 bits starting at the specified bit array index as a signed integer.</summary>
        /// <param name="x">The X-Coordinate in the bit array to start reading at.</param>
		/// <param name="y">The Y-Coordinate in the bit array to start reading at.</param>
        /// <returns>The 32-bit signed integer.</returns>
        public int GetInt32(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) throw new IndexOutOfRangeException("Index was outside the bounds of the array.");
            return _Bits.GetInt32(x + y * Width);
        }

        /// <summary>Writes 32 bits starting at the specified bit array index as a signed integer.</summary>
        /// <param name="x">The X-Coordinate in the bit array to start writing at.</param>
		/// <param name="y">The Y-Coordinate in the bit array to start writing at.</param>
        /// <param name="value">The 32-bit signed integer.</param>
        public void SetInt32(int x, int y, int value)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) throw new IndexOutOfRangeException("Index was outside the bounds of the array.");
            _Bits.SetInt32(x + y * Width, value);
        }

        /// <summary>Reads 32 bits starting at the specified bit array index as a signed integer in big-endian order.</summary>
        /// <param name="x">The X-Coordinate in the bit array to start reading at.</param>
		/// <param name="y">The Y-Coordinate in the bit array to start reading at.</param>
        /// <returns>The 32-bit signed integer.</returns>
        public int GetInt32BigEndian(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) throw new IndexOutOfRangeException("Index was outside the bounds of the array.");
            return _Bits.GetInt32BigEndian(x + y * Width);
        }

        /// <summary>Writes 32 bits starting at the specified bit array index as a signed integer in big-endian order.</summary>
        /// <param name="x">The X-Coordinate in the bit array to start writing at.</param>
		/// <param name="y">The Y-Coordinate in the bit array to start writing at.</param>
        /// <param name="value">The 32-bit signed integer.</param>
        public void SetInt32BigEndian(int x, int y, int value)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) throw new IndexOutOfRangeException("Index was outside the bounds of the array.");
            _Bits.SetInt32BigEndian(x + y * Width, value);
        }

        /// <summary>Reads 32 bits starting at the specified bit array index as a single-precision floating-point value.</summary>
        /// <param name="x">The X-Coordinate in the bit array to start reading at.</param>
		/// <param name="y">The Y-Coordinate in the bit array to start reading at.</param>
        /// <returns>The 32-bit single-precision floating-point number.</returns>
        public float GetSingle(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) throw new IndexOutOfRangeException("Index was outside the bounds of the array.");
            return _Bits.GetSingle(x + y * Width);
        }

        /// <summary>Writes 32 bits starting at the specified bit array index as a single-precision floating-point value.</summary>
        /// <param name="x">The X-Coordinate in the bit array to start writing at.</param>
		/// <param name="y">The Y-Coordinate in the bit array to start writing at.</param>
        /// <param name="value">The 32-bit single-precision floating-point number.</param>
        public void SetSingle(int x, int y, float value)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) throw new IndexOutOfRangeException("Index was outside the bounds of the array.");
            _Bits.SetSingle(x + y * Width, value);
        }

        /// <summary>Reads 32 bits starting at the specified bit array index as a single-precision floating-point value in big-endian order.</summary>
        /// <param name="x">The X-Coordinate in the bit array to start writing at.</param>
		/// <param name="y">The Y-Coordinate in the bit array to start writing at.</param>
        /// <returns>The 32-bit single-precision floating-point number.</returns>
        public float GetSingleBigEndian(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) throw new IndexOutOfRangeException("Index was outside the bounds of the array.");
            return _Bits.GetSingleBigEndian(x + y * Width);
        }

        /// <summary>Reads 32 bits starting at the specified bit array index as a single-precision floating-point value in big-endian order.</summary>
        /// <param name="x">The X-Coordinate in the bit array to start writing at.</param>
		/// <param name="y">The Y-Coordinate in the bit array to start writing at.</param>
        /// <returns>The 32-bit single-precision floating-point number.</returns>
        public void SetSingleBigEndian(int x, int y, float value)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) throw new IndexOutOfRangeException("Index was outside the bounds of the array.");
            _Bits.SetSingleBigEndian(x + y * Width, value);
        }

        /// <summary>Reads 64 bits starting at the specified bit array index as an unsigned integer.</summary>
        /// <param name="x">The X-Coordinate in the bit array to start reading at.</param>
		/// <param name="y">The Y-Coordinate in the bit array to start reading at.</param>
        /// <returns>The 64-bit unsigned integer.</returns>
        public ulong GetUInt64(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) throw new IndexOutOfRangeException("Index was outside the bounds of the array.");
            return _Bits.GetUInt64(x + y * Width);
        }

        /// <summary>Writes 64 bits starting at the specified bit array index as an unsigned integer.</summary>
        /// <param name="x">The X-Coordinate in the bit array to start writing at.</param>
		/// <param name="y">The Y-Coordinate in the bit array to start writing at.</param>
        /// <param name="value">The 64-bit unsigned integer.</param>
        public void SetUInt64(int x, int y, ulong value)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) throw new IndexOutOfRangeException("Index was outside the bounds of the array.");
            _Bits.SetUInt64(x + y * Width, value);
        }

        /// <summary>Reads 64 bits starting at the specified bit array index as an unsigned integer in big-endian order.</summary>
        /// <param name="x">The X-Coordinate in the bit array to start reading at.</param>
		/// <param name="y">The Y-Coordinate in the bit array to start reading at.</param>
        /// <returns>The 64-bit unsigned integer.</returns>
        public ulong GetUInt64BigEndian(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) throw new IndexOutOfRangeException("Index was outside the bounds of the array.");
            return _Bits.GetUInt64BigEndian(x + y * Width);
        }

        /// <summary>Writes 64 bits starting at the specified bit array index as an unsigned integer in big-endian order.</summary>
        /// <param name="x">The X-Coordinate in the bit array to start writing at.</param>
		/// <param name="y">The Y-Coordinate in the bit array to start writing at.</param>
        /// <param name="value">The 64-bit unsigned integer.</param>
        public void SetUInt64BigEndian(int x, int y, ulong value)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) throw new IndexOutOfRangeException("Index was outside the bounds of the array.");
            _Bits.SetUInt64BigEndian(x + y * Width, value);
        }

        /// <summary>Reads 64 bits starting at the specified bit array index as a signed integer.</summary>
        /// <param name="x">The X-Coordinate in the bit array to start reading at.</param>
		/// <param name="y">The Y-Coordinate in the bit array to start reading at.</param>
        /// <returns>The 64-bit signed integer.</returns>
        public long GetInt64(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) throw new IndexOutOfRangeException("Index was outside the bounds of the array.");
            return _Bits.GetInt64(x + y * Width);
        }

        /// <summary>Writes 64 bits starting at the specified bit array index as a signed integer.</summary>
        /// <param name="x">The X-Coordinate in the bit array to start writing at.</param>
		/// <param name="y">The Y-Coordinate in the bit array to start writing at.</param>
        /// <param name="value">The 64-bit signed integer.</param>
        public void SetInt64(int x, int y, long value)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) throw new IndexOutOfRangeException("Index was outside the bounds of the array.");
            _Bits.SetInt64(x + y * Width, value);
        }

        /// <summary>Reads 64 bits starting at the specified bit array index as a signed integer in big-endian order.</summary>
        /// <param name="x">The X-Coordinate in the bit array to start reading at.</param>
		/// <param name="y">The Y-Coordinate in the bit array to start reading at.</param>
        /// <returns>The 64-bit signed integer.</returns>
        public long GetInt64BigEndian(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) throw new IndexOutOfRangeException("Index was outside the bounds of the array.");
            return _Bits.GetInt64BigEndian(x + y * Width);
        }

        /// <summary>Writes 64 bits starting at the specified bit array index as a signed integer in big-endian order.</summary>
        /// <param name="x">The X-Coordinate in the bit array to start writing at.</param>
		/// <param name="y">The Y-Coordinate in the bit array to start writing at.</param>
        /// <param name="value">The 64-bit signed integer.</param>
        public void SetInt64BigEndian(int x, int y, long value)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) throw new IndexOutOfRangeException("Index was outside the bounds of the array.");
            _Bits.SetInt64BigEndian(x + y * Width, value);
        }

        /// <summary>Reads 64 bits starting at the specified bit array index as a double-precision floating-point value.</summary>
        /// <param name="x">The X-Coordinate in the bit array to start reading at.</param>
		/// <param name="y">The Y-Coordinate in the bit array to start reading at.</param>
        /// <returns>The 64-bit double-precision floating-point number.</returns>
        public double GetDouble(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) throw new IndexOutOfRangeException("Index was outside the bounds of the array.");
            return _Bits.GetDouble(x + y * Width);
        }

        /// <summary>Writes 64 bits starting at the specified bit array index as a double-precision floating-point value.</summary>
        /// <param name="x">The X-Coordinate in the bit array to start writing at.</param>
		/// <param name="y">The Y-Coordinate in the bit array to start writing at.</param>
        /// <param name="value">The 64-bit double-precision floating-point number.</param>
        public void SetDouble(int x, int y, double value)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) throw new IndexOutOfRangeException("Index was outside the bounds of the array.");
            _Bits.SetDouble(x + y * Width, value);
        }

        /// <summary>Reads 64 bits starting at the specified bit array index as a double-precision floating-point value in big-endian order.</summary>
        /// <param name="x">The X-Coordinate in the bit array to start writing at.</param>
		/// <param name="y">The Y-Coordinate in the bit array to start writing at.</param>
        /// <returns>The 64-bit double-precision floating-point number.</returns>
        public double GetDoubleBigEndian(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) throw new IndexOutOfRangeException("Index was outside the bounds of the array.");
            return _Bits.GetDoubleBigEndian(x + y * Width);
        }

        /// <summary>Reads 64 bits starting at the specified bit array index as a double-precision floating-point value in big-endian order.</summary>
        /// <param name="x">The X-Coordinate in the bit array to start writing at.</param>
		/// <param name="y">The Y-Coordinate in the bit array to start writing at.</param>
        /// <returns>The 64-bit double-precision floating-point number.</returns>
        public void SetDoubleBigEndian(int x, int y, double value)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) throw new IndexOutOfRangeException("Index was outside the bounds of the array.");
            _Bits.SetDoubleBigEndian(x + y * Width, value);
        }

        #endregion Setting and Getting Bytes, Integers and Floats

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
            return new BitArray2(this);
        }

        #endregion ICloneable Implementation

        /// <summary>
        /// Copies all the bits of the current <see cref="BitArray2"/> to the specified <see
        /// cref="BitArray2"/>. The <paramref name="destination"/> must have the same amount of bits.
        /// </summary>
        /// <param name="destination">The destination <see cref="BitArray2"/> to write to.</param>
        /// <exception cref="ArgumentNullException">Value is null.</exception>
        /// <exception cref="ArgumentException">
        /// The value and the current <see cref="BitArray2"/> do not have the same number of bits.
        /// </exception>
        public void CopyTo(BitArray2 destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            _Bits.CopyTo(destination._Bits);
        }
    }
}