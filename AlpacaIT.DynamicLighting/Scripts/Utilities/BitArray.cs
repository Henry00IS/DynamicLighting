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
using System.Runtime.InteropServices;
using System.Text;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>
    /// Manages a compact array of bit values, which are represented as Booleans, where true
    /// indicates that the bit is on (1) and false indicates the bit is off (0).
    /// </summary>
    public class BitArray : IReadOnlyCollection<bool>, ICloneable
    {
        /// <summary>The internal array of 32-bit elements that store the bits.</summary>
        private readonly uint[] _Data;

        /// <summary>The size of the array in bits.</summary>
        private readonly int _Size;

        /// <summary>Creates a new instance of <see cref="BitArray"/> for cloning.</summary>
        private BitArray()
        {
            // invalid until private properties are manually assigned.
        }

        /// <summary>
        /// Creates a new instance of <see cref="BitArray"/> with the specified amount of bits.
        /// </summary>
        /// <param name="size">The amount of bits to be stored in the array.</param>
        public BitArray(int size)
        {
            if (size < 0) throw new ArgumentOutOfRangeException(nameof(size), "Non-negative number required.");
            _Size = size;

            // create the internal array of 32-bit elements.
            _Data = new uint[CalculateDataLength(size)];
        }

        /// <summary>
        /// Creates a new instance of <see cref="BitArray"/> and copies the bits from another <see cref="BitArray"/>.
        /// </summary>
        /// <param name="original">The bit array to be copied into this new instance.</param>
        public BitArray(BitArray original)
        {
            if (original == null) throw new ArgumentNullException(nameof(original));

            _Data = (uint[])original._Data.Clone();
            _Size = original._Size;
        }

        /// <summary>
        /// Creates a new instance of <see cref="BitArray"/> and copies the bits from the given <see
        /// cref="uint[]"/> array.
        /// </summary>
        /// <param name="original">The array to be copied into this new instance.</param>
        public BitArray(uint[] original)
        {
            if (original == null) throw new ArgumentNullException(nameof(original));

            _Data = (uint[])original.Clone();
            _Size = _Data.Length * 32;
        }

        /// <summary>
        /// Creates a new instance of <see cref="BitArray"/> and copies the bits from the given <see
        /// cref="uint[]"/> array.
        /// </summary>
        /// <param name="original">The array to be copied into this new instance.</param>
        /// <param name="size">The size of the array in bits to be copied.</param>
        public BitArray(uint[] original, int size)
        {
            if (original == null) throw new ArgumentNullException(nameof(original));
            if (size < 0) throw new ArgumentOutOfRangeException(nameof(size), "Non-negative number required.");
            if (size == 0) { _Data = new uint[0]; _Size = 0; return; }

            // only copy the 32-bit unsigned integers that we need.
            var length = CalculateDataLength(size);
            _Data = new uint[length];
            _Size = size;
            Array.Copy(original, _Data, length);

            // figure out how many bits are unused at the end and set them to 0.
            TrimUnusedBits();
        }

        /// <summary>
        /// Creates a new instance of <see cref="BitArray"/> and copies the bits from the given <see
        /// cref="byte[]"/> array.
        /// </summary>
        /// <param name="original">The array to be copied into this new instance.</param>
        public BitArray(byte[] original)
        {
            if (original == null) throw new ArgumentNullException(nameof(original));
            _Size = original.Length * 8;
            _Data = new uint[CalculateDataLength(_Size)];

            int i = 0;
            int j = 0;
            while (original.Length - j >= 4)
            {
                _Data[i++] = (uint)((original[j] & 0xff) |
                              ((original[j + 1] & 0xff) << 8) |
                              ((original[j + 2] & 0xff) << 16) |
                              ((original[j + 3] & 0xff) << 24));
                j += 4;
            }

            switch (original.Length - j)
            {
                case 3:
                    _Data[i] = (uint)((original[j + 2] & 0xff) << 16);
                    goto case 2;
                case 2:
                    _Data[i] |= (uint)((original[j + 1] & 0xff) << 8);
                    goto case 1;
                case 1:
                    _Data[i] |= (uint)(original[j] & 0xff);
                    break;
            }
        }

        /// <summary>Calculates the <paramref name="size"/>-bit array length required to fit the amount of bits inside.</summary>
        /// <param name="bits">The amount of bits to fit into the array.</param>
        /// <param name="size">The size of an element (by default 32 bits)</param>
        /// <returns>The number of <paramref name="size"/>-bit elements required.</returns>
        private int CalculateDataLength(int bits, int size = 32)
        {
            return bits > 0 ? (((bits - 1) / size) + 1) : 0;
        }

        /// <summary>Sets the right-most bits in memory that are unused to zero.</summary>
        private void TrimUnusedBits()
        {
            if (_Size == 0) return;

            // figure out how many bits are unused at the end and set them to 0.
            var unusedBitsCount = 31 - ((_Size - 1) % 32);
            _Data[_Data.Length - 1] &= uint.MaxValue >> unusedBitsCount;
        }

        /// <summary>Gets the total number of bits in the array.</summary>
        public int Length => _Size;

        /// <summary>Gets or sets the bit at the specified array index.</summary>
        /// <param name="index">The index of the bit in the array.</param>
        /// <returns>True when the bit is on (1) and false when the bit is off (0).</returns>
        /// <exception cref="IndexOutOfRangeException">
        /// Index was outside the bounds of the array.
        /// </exception>
        public bool this[int index]
        {
            get
            {
                if (index >= _Size) throw new IndexOutOfRangeException("Index was outside the bounds of the array.");
                return (_Data[index / 32] & (1 << (index % 32))) != 0;
            }
            set
            {
                if (index < 0 || index >= _Size) throw new IndexOutOfRangeException("Index was outside the bounds of the array.");
                if (value)
                    _Data[index / 32] |= (uint)1 << (index % 32);
                else
                    _Data[index / 32] &= ~((uint)1 << (index % 32));
            }
        }

        /// <summary>Retrieves a <see cref="uint[]"/> containing all of the bits in the array.</summary>
        /// <returns>The <see cref="uint[]"/> containing all of the bits.</returns>
        public uint[] ToUInt32Array() => (uint[])_Data.Clone();

        /// <summary>Retrieves a <see cref="byte[]"/> containing all of the bits in the array.</summary>
        /// <returns>The <see cref="byte[]"/> containing all of the bits.</returns>
        public byte[] ToByteArray()
        {
            var result = new byte[CalculateDataLength(_Size, 8)];
            var j = 0;
            for (int i = 0; i < _Data.Length; i++)
            {
                var bytes = new Bytes64(_Data[i]);
                if (j >= result.Length) break;
                result[j++] = bytes.b0;
                if (j >= result.Length) break;
                result[j++] = bytes.b1;
                if (j >= result.Length) break;
                result[j++] = bytes.b2;
                if (j >= result.Length) break;
                result[j++] = bytes.b3;
            }
            return result;
        }

        /// <summary>
        /// Returns a string that represents the current object. The one-dimensional string of bits.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            var sb = new StringBuilder(_Size);

            for (int i = 0; i < _Size; i++)
                sb.Append(this[i] ? "1" : "0");

            return sb.ToString();
        }

        #region Bit Operators

        /// <summary>Sets all bits in the <see cref="BitArray"/> to the specified value.</summary>
        /// <param name="value">The boolean value to assign to all bits.</param>
        public void SetAll(bool value)
        {
            uint fill = value ? uint.MaxValue : 0;

            // iterate over the full 32-bit elements to make this process faster.
            for (int i = 0; i < _Data.Length; i++)
                _Data[i] = fill;

            // figure out how many bits are unused at the end and keep them set to 0.
            TrimUnusedBits();
        }

        /// <summary>
        /// Inverts all the bit values in the <see cref="BitArray"/>, so that bits set to true 1 are
        /// changed to false 0, and bits set to false 0 are changed to true 1.
        /// </summary>
        public void Not()
        {
            // iterate over the full 32-bit elements to make this process faster.
            for (int i = 0; i < _Data.Length; i++)
                _Data[i] = ~_Data[i];

            // figure out how many bits are unused at the end and keep them set to 0.
            TrimUnusedBits();
        }

        /// <summary>
        /// Performs the bitwise AND operation between the bits of the current <see
        /// cref="BitArray"/> object and the corresponding bits in the specified array. The current
        /// <see cref="BitArray"/> object will be modified to store the result of the bitwise AND operation.
        /// </summary>
        /// <param name="value">The array with which to perform the bitwise AND operation.</param>
        /// <exception cref="ArgumentNullException">Value is null.</exception>
        /// <exception cref="ArgumentException">
        /// The value and the current <see cref="BitArray"/> do not have the same number of bits.
        /// </exception>
        public void And(BitArray value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (_Size != value._Size) throw new ArgumentException("The value and the current " + nameof(BitArray) + " do not have the same number of bits.");

            // iterate over the full 32-bit elements to make this process faster.
            for (int i = 0; i < _Data.Length; i++)
                _Data[i] &= value._Data[i];

            // figure out how many bits are unused at the end and keep them set to 0.
            TrimUnusedBits();
        }

        /// <summary>
        /// Performs the bitwise OR operation between the bits of the current <see
        /// cref="BitArray"/> object and the corresponding bits in the specified array. The current
        /// <see cref="BitArray"/> object will be modified to store the result of the bitwise OR operation.
        /// </summary>
        /// <param name="value">The array with which to perform the bitwise OR operation.</param>
        /// <exception cref="ArgumentNullException">Value is null.</exception>
        /// <exception cref="ArgumentException">
        /// The value and the current <see cref="BitArray"/> do not have the same number of bits.
        /// </exception>
        public void Or(BitArray value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (_Size != value._Size) throw new ArgumentException("The value and the current " + nameof(BitArray) + " do not have the same number of bits.");

            // iterate over the full 32-bit elements to make this process faster.
            for (int i = 0; i < _Data.Length; i++)
                _Data[i] |= value._Data[i];

            // figure out how many bits are unused at the end and keep them set to 0.
            TrimUnusedBits();
        }

        /// <summary>
        /// Performs the bitwise XOR operation between the bits of the current <see
        /// cref="BitArray"/> object and the corresponding bits in the specified array. The current
        /// <see cref="BitArray"/> object will be modified to store the result of the bitwise XOR operation.
        /// </summary>
        /// <param name="value">The array with which to perform the bitwise XOR operation.</param>
        /// <exception cref="ArgumentNullException">Value is null.</exception>
        /// <exception cref="ArgumentException">
        /// The value and the current <see cref="BitArray"/> do not have the same number of bits.
        /// </exception>
        public void Xor(BitArray value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (_Size != value._Size) throw new ArgumentException("The value and the current " + nameof(BitArray) + " do not have the same number of bits.");

            // iterate over the full 32-bit elements to make this process faster.
            for (int i = 0; i < _Data.Length; i++)
                _Data[i] ^= value._Data[i];

            // figure out how many bits are unused at the end and keep them set to 0.
            TrimUnusedBits();
        }

        /// <summary>
        /// Performs the bitwise left shift operation on the bits of the current <see
        /// cref="BitArray"/> object. The bits that slide off the end disappear and the spaces are
        /// always filled with zeros.
        /// </summary>
        /// <param name="amount">How many positions to shift the bits by.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The <paramref name="amount"/> can not be a negative number.
        /// </exception>
        public void Shl(int amount)
        {
            if (_Size == 0) return;
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "Non-negative number required.");

            // this is slow and could be made faster with full 32-bit element shifts.
            var bits = _Size - 1;
            for (int i = 0; i < amount; i++)
            {
                for (int index = 0; index < bits; index++)
                    this[index] = this[index + 1];
                this[bits] = false;
            }
        }

        /// <summary>
        /// Performs the bitwise right shift operation on the bits of the current <see
        /// cref="BitArray"/> object. The bits that slide off the end disappear and the spaces are
        /// always filled with zeros.
        /// </summary>
        /// <param name="amount">How many positions to shift the bits by.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The <paramref name="amount"/> can not be a negative number.
        /// </exception>
        public void Shr(int amount)
        {
            if (_Size == 0) return;
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "Non-negative number required.");

            // this is slow and could be made faster with full 32-bit element shifts.
            var bits = _Size - 1;
            for (int i = 0; i < amount; i++)
            {
                for (int index = bits; index-- > 0;)
                    this[index + 1] = this[index];
                this[0] = false;
            }
        }

        /// <summary>
        /// Performs the bitwise left rotate operation on the bits of the current <see
        /// cref="BitArray"/> object. The bits that slide off the end are fed back into spaces.
        /// </summary>
        /// <param name="amount">How many positions to rotate the bits by.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The <paramref name="amount"/> can not be a negative number.
        /// </exception>
        public void Rol(int amount)
        {
            if (_Size == 0) return;
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "Non-negative number required.");

            // this is slow and could be made faster with full 32-bit element shifts.
            var bits = _Size - 1;
            for (int i = 0; i < amount; i++)
            {
                var first = this[0];
                for (int index = 0; index < bits; index++)
                    this[index] = this[index + 1];
                this[bits] = first;
            }
        }

        /// <summary>
        /// Performs the bitwise right rotate operation on the bits of the current <see
        /// cref="BitArray"/> object. The bits that slide off the end are fed back into spaces.
        /// </summary>
        /// <param name="amount">How many positions to rotate the bits by.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The <paramref name="amount"/> can not be a negative number.
        /// </exception>
        public void Ror(int amount)
        {
            if (_Size == 0) return;
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount), "Non-negative number required.");

            // this is slow and could be made faster with full 32-bit element shifts.
            var bits = _Size - 1;
            for (int i = 0; i < amount; i++)
            {
                var last = this[bits];
                for (int index = bits; index-- > 0;)
                    this[index + 1] = this[index];
                this[0] = last;
            }
        }

        /// <summary>Checks whether all bits in the bit array are zero.</summary>
        /// <returns>True when all bits are zero else false.</returns>
        public bool IsZero()
        {
            // iterate over the full 32-bit elements to make this process faster.
            for (int i = 0; i < _Data.Length; i++)
                if (_Data[i] != 0)
                    return false;
            return true;
        }

        #endregion Bit Operators

        #region Setting and Getting Bytes, Integers and Floats

        /// <summary>Reads 8 bits starting at the specified bit array index as an unsigned byte.</summary>
        /// <param name="index">The bit array index to start reading at.</param>
        /// <returns>The 8-bit unsigned integer.</returns>
        public byte GetByte(int index)
        {
            if (index < 0 || index > _Size - 8) throw new IndexOutOfRangeException("Index was outside; or reading beyond the bounds of the array.");
            byte result = 0;
            for (int i = 0; i < 8; i++)
                result |= (byte)((this[index + i] ? 1 : 0) << i);
            return result;
        }

        /// <summary>Writes 8 bits starting at the specified bit array index as an unsigned byte.</summary>
        /// <param name="index">The bit array index to start writing at.</param>
        /// <param name="value">The 8-bit unsigned integer.</param>
        public void SetByte(int index, byte value)
        {
            if (index < 0 || index > _Size - 8) throw new IndexOutOfRangeException("Index was outside; or writing beyond the bounds of the array.");
            for (int i = 0; i < 8; i++)
                this[index + i] = (value & (1 << i)) > 0;
        }

        /// <summary>Reads 16 bits starting at the specified bit array index as an unsigned integer.</summary>
        /// <param name="index">The bit array index to start reading at.</param>
        /// <returns>The 16-bit unsigned integer.</returns>
        public ushort GetUInt16(int index)
        {
            if (index < 0 || index > _Size - 16) throw new IndexOutOfRangeException("Index was outside; or reading beyond the bounds of the array.");
            return new Bytes64(GetByte(index), GetByte(index + 8)).vUInt16;
        }

        /// <summary>Writes 16 bits starting at the specified bit array index as an unsigned integer.</summary>
        /// <param name="index">The bit array index to start writing at.</param>
        /// <param name="value">The 16-bit unsigned integer.</param>
        public void SetUInt16(int index, ushort value)
        {
            var bytes = new Bytes64(value);
            SetByte(index, bytes.b0);
            SetByte(index + 8, bytes.b1);
        }

        /// <summary>Reads 16 bits starting at the specified bit array index as an unsigned integer in big-endian order.</summary>
        /// <param name="index">The bit array index to start reading at.</param>
        /// <returns>The 16-bit unsigned integer.</returns>
        public ushort GetUInt16BigEndian(int index)
        {
            if (index < 0 || index > _Size - 16) throw new IndexOutOfRangeException("Index was outside; or reading beyond the bounds of the array.");
            return new Bytes64(GetByte(index + 8), GetByte(index)).vUInt16;
        }

        /// <summary>Writes 16 bits starting at the specified bit array index as an unsigned integer in big-endian order.</summary>
        /// <param name="index">The bit array index to start writing at.</param>
        /// <param name="value">The 16-bit unsigned integer.</param>
        public void SetUInt16BigEndian(int index, ushort value)
        {
            var bytes = new Bytes64(value);
            SetByte(index, bytes.b1);
            SetByte(index + 8, bytes.b0);
        }

        /// <summary>Reads 16 bits starting at the specified bit array index as a signed integer.</summary>
        /// <param name="index">The bit array index to start reading at.</param>
        /// <returns>The 16-bit signed integer.</returns>
        public short GetInt16(int index) => unchecked((short)GetUInt16(index));

        /// <summary>Writes 16 bits starting at the specified bit array index as a signed integer.</summary>
        /// <param name="index">The bit array index to start writing at.</param>
        /// <param name="value">The 16-bit signed integer.</param>
        public void SetInt16(int index, short value) => SetUInt16(index, unchecked((ushort)value));

        /// <summary>Reads 16 bits starting at the specified bit array index as a signed integer in big-endian order.</summary>
        /// <param name="index">The bit array index to start reading at.</param>
        /// <returns>The 16-bit signed integer.</returns>
        public short GetInt16BigEndian(int index) => unchecked((short)GetUInt16BigEndian(index));

        /// <summary>Writes 16 bits starting at the specified bit array index as a signed integer in big-endian order.</summary>
        /// <param name="index">The bit array index to start writing at.</param>
        /// <param name="value">The 16-bit signed integer.</param>
        public void SetInt16BigEndian(int index, short value) => SetUInt16BigEndian(index, unchecked((ushort)value));

        /// <summary>Reads 32 bits starting at the specified bit array index as an unsigned integer.</summary>
        /// <param name="index">The bit array index to start reading at.</param>
        /// <returns>The 32-bit unsigned integer.</returns>
        public uint GetUInt32(int index)
        {
            if (index < 0 || index > _Size - 32) throw new IndexOutOfRangeException("Index was outside; or reading beyond the bounds of the array.");
            return new Bytes64(GetByte(index), GetByte(index + 8), GetByte(index + 16), GetByte(index + 24)).vUInt32;
        }

        /// <summary>Writes 32 bits starting at the specified bit array index as an unsigned integer.</summary>
        /// <param name="index">The bit array index to start writing at.</param>
        /// <param name="value">The 32-bit unsigned integer.</param>
        public void SetUInt32(int index, uint value)
        {
            var bytes = new Bytes64(value);
            SetByte(index, bytes.b0);
            SetByte(index + 8, bytes.b1);
            SetByte(index + 16, bytes.b2);
            SetByte(index + 24, bytes.b3);
        }

        /// <summary>Reads 32 bits starting at the specified bit array index as an unsigned integer in big-endian order.</summary>
        /// <param name="index">The bit array index to start reading at.</param>
        /// <returns>The 32-bit unsigned integer.</returns>
        public uint GetUInt32BigEndian(int index)
        {
            if (index < 0 || index > _Size - 32) throw new IndexOutOfRangeException("Index was outside; or reading beyond the bounds of the array.");
            return new Bytes64(GetByte(index + 24), GetByte(index + 16), GetByte(index + 8), GetByte(index)).vUInt32;
        }

        /// <summary>Writes 32 bits starting at the specified bit array index as an unsigned integer in big-endian order.</summary>
        /// <param name="index">The bit array index to start writing at.</param>
        /// <param name="value">The 32-bit unsigned integer.</param>
        public void SetUInt32BigEndian(int index, uint value)
        {
            var bytes = new Bytes64(value);
            SetByte(index, bytes.b3);
            SetByte(index + 8, bytes.b2);
            SetByte(index + 16, bytes.b1);
            SetByte(index + 24, bytes.b0);
        }

        /// <summary>Reads 32 bits starting at the specified bit array index as a signed integer.</summary>
        /// <param name="index">The bit array index to start reading at.</param>
        /// <returns>The 32-bit signed integer.</returns>
        public int GetInt32(int index) => unchecked((int)GetUInt32(index));

        /// <summary>Writes 32 bits starting at the specified bit array index as a signed integer.</summary>
        /// <param name="index">The bit array index to start writing at.</param>
        /// <param name="value">The 32-bit signed integer.</param>
        public void SetInt32(int index, int value) => SetUInt32(index, unchecked((uint)value));

        /// <summary>Reads 32 bits starting at the specified bit array index as a signed integer in big-endian order.</summary>
        /// <param name="index">The bit array index to start reading at.</param>
        /// <returns>The 32-bit signed integer.</returns>
        public int GetInt32BigEndian(int index) => unchecked((int)GetUInt32BigEndian(index));

        /// <summary>Writes 32 bits starting at the specified bit array index as a signed integer in big-endian order.</summary>
        /// <param name="index">The bit array index to start writing at.</param>
        /// <param name="value">The 32-bit signed integer.</param>
        public void SetInt32BigEndian(int index, int value) => SetUInt32BigEndian(index, unchecked((uint)value));

        /// <summary>Reads 32 bits starting at the specified bit array index as a single-precision floating-point value.</summary>
        /// <param name="index">The bit array index to start reading at.</param>
        /// <returns>The 32-bit single-precision floating-point number.</returns>
        public float GetSingle(int index) => new Bytes64(GetUInt32(index)).vSingle;

        /// <summary>Writes 32 bits starting at the specified bit array index as a single-precision floating-point value.</summary>
        /// <param name="index">The bit array index to start writing at.</param>
        /// <param name="value">The 32-bit single-precision floating-point number.</param>
        public void SetSingle(int index, float value) => SetUInt32(index, new Bytes64(value).vUInt32);

        /// <summary>Reads 32 bits starting at the specified bit array index as a single-precision floating-point value in big-endian order.</summary>
        /// <param name="index">The bit array index to start reading at.</param>
        /// <returns>The 32-bit single-precision floating-point number.</returns>
        public float GetSingleBigEndian(int index)
        {
            if (index < 0 || index > _Size - 32) throw new IndexOutOfRangeException("Index was outside; or reading beyond the bounds of the array.");
            return new Bytes64(GetByte(index + 24), GetByte(index + 16), GetByte(index + 8), GetByte(index)).vSingle;
        }

        /// <summary>Reads 32 bits starting at the specified bit array index as a single-precision floating-point value in big-endian order.</summary>
        /// <param name="index">The bit array index to start reading at.</param>
        /// <returns>The 32-bit single-precision floating-point number.</returns>
        public void SetSingleBigEndian(int index, float value)
        {
            var bytes = new Bytes64(value);
            SetByte(index, bytes.b3);
            SetByte(index + 8, bytes.b2);
            SetByte(index + 16, bytes.b1);
            SetByte(index + 24, bytes.b0);
        }

        /// <summary>Reads 64 bits starting at the specified bit array index as an unsigned integer.</summary>
        /// <param name="index">The bit array index to start reading at.</param>
        /// <returns>The 64-bit unsigned integer.</returns>
        public ulong GetUInt64(int index)
        {
            if (index < 0 || index > _Size - 64) throw new IndexOutOfRangeException("Index was outside; or reading beyond the bounds of the array.");
            return new Bytes64(GetByte(index), GetByte(index + 8), GetByte(index + 16), GetByte(index + 24), GetByte(index + 32), GetByte(index + 40), GetByte(index + 48), GetByte(index + 56)).vUInt64;
        }

        /// <summary>Writes 64 bits starting at the specified bit array index as an unsigned integer.</summary>
        /// <param name="index">The bit array index to start writing at.</param>
        /// <param name="value">The 64-bit unsigned integer.</param>
        public void SetUInt64(int index, ulong value)
        {
            var bytes = new Bytes64(value);
            SetByte(index, bytes.b0);
            SetByte(index + 8, bytes.b1);
            SetByte(index + 16, bytes.b2);
            SetByte(index + 24, bytes.b3);
            SetByte(index + 32, bytes.b4);
            SetByte(index + 40, bytes.b5);
            SetByte(index + 48, bytes.b6);
            SetByte(index + 56, bytes.b7);
        }

        /// <summary>Reads 64 bits starting at the specified bit array index as an unsigned integer in big-endian order.</summary>
        /// <param name="index">The bit array index to start reading at.</param>
        /// <returns>The 64-bit unsigned integer.</returns>
        public ulong GetUInt64BigEndian(int index)
        {
            if (index < 0 || index > _Size - 64) throw new IndexOutOfRangeException("Index was outside; or reading beyond the bounds of the array.");
            return new Bytes64(GetByte(index + 56), GetByte(index + 48), GetByte(index + 40), GetByte(index + 32), GetByte(index + 24), GetByte(index + 16), GetByte(index + 8), GetByte(index)).vUInt64;
        }

        /// <summary>Writes 64 bits starting at the specified bit array index as an unsigned integer in big-endian order.</summary>
        /// <param name="index">The bit array index to start writing at.</param>
        /// <param name="value">The 64-bit unsigned integer.</param>
        public void SetUInt64BigEndian(int index, ulong value)
        {
            var bytes = new Bytes64(value);
            SetByte(index, bytes.b7);
            SetByte(index + 8, bytes.b6);
            SetByte(index + 16, bytes.b5);
            SetByte(index + 24, bytes.b4);
            SetByte(index + 32, bytes.b3);
            SetByte(index + 40, bytes.b2);
            SetByte(index + 48, bytes.b1);
            SetByte(index + 56, bytes.b0);
        }

        /// <summary>Reads 64 bits starting at the specified bit array index as a signed integer.</summary>
        /// <param name="index">The bit array index to start reading at.</param>
        /// <returns>The 64-bit signed integer.</returns>
        public long GetInt64(int index) => unchecked((long)GetUInt64(index));

        /// <summary>Writes 64 bits starting at the specified bit array index as a signed integer.</summary>
        /// <param name="index">The bit array index to start writing at.</param>
        /// <param name="value">The 64-bit signed integer.</param>
        public void SetInt64(int index, long value) => SetUInt64(index, unchecked((ulong)value));

        /// <summary>Reads 64 bits starting at the specified bit array index as a signed integer in big-endian order.</summary>
        /// <param name="index">The bit array index to start reading at.</param>
        /// <returns>The 64-bit signed integer.</returns>
        public long GetInt64BigEndian(int index) => unchecked((long)GetUInt64BigEndian(index));

        /// <summary>Writes 64 bits starting at the specified bit array index as a signed integer in big-endian order.</summary>
        /// <param name="index">The bit array index to start writing at.</param>
        /// <param name="value">The 64-bit signed integer.</param>
        public void SetInt64BigEndian(int index, long value) => SetUInt64BigEndian(index, unchecked((ulong)value));

        /// <summary>Reads 64 bits starting at the specified bit array index as a double-precision floating-point value.</summary>
        /// <param name="index">The bit array index to start reading at.</param>
        /// <returns>The 64-bit double-precision floating-point number.</returns>
        public double GetDouble(int index) => new Bytes64(GetUInt64(index)).vDouble;

        /// <summary>Writes 64 bits starting at the specified bit array index as a double-precision floating-point value.</summary>
        /// <param name="index">The bit array index to start writing at.</param>
        /// <param name="value">The 64-bit double-precision floating-point number.</param>
        public void SetDouble(int index, double value) => SetUInt64(index, new Bytes64(value).vUInt64);

        /// <summary>Reads 64 bits starting at the specified bit array index as a double-precision floating-point value in big-endian order.</summary>
        /// <param name="index">The bit array index to start reading at.</param>
        /// <returns>The 64-bit double-precision floating-point number.</returns>
        public double GetDoubleBigEndian(int index)
        {
            if (index < 0 || index > _Size - 64) throw new IndexOutOfRangeException("Index was outside; or reading beyond the bounds of the array.");
            return new Bytes64(GetByte(index + 56), GetByte(index + 48), GetByte(index + 40), GetByte(index + 32), GetByte(index + 24), GetByte(index + 16), GetByte(index + 8), GetByte(index)).vDouble;
        }

        /// <summary>Reads 64 bits starting at the specified bit array index as a double-precision floating-point value in big-endian order.</summary>
        /// <param name="index">The bit array index to start reading at.</param>
        /// <returns>The 64-bit double-precision floating-point number.</returns>
        public void SetDoubleBigEndian(int index, double value)
        {
            var bytes = new Bytes64(value);
            SetByte(index, bytes.b7);
            SetByte(index + 8, bytes.b6);
            SetByte(index + 16, bytes.b5);
            SetByte(index + 24, bytes.b4);
            SetByte(index + 32, bytes.b3);
            SetByte(index + 40, bytes.b2);
            SetByte(index + 48, bytes.b1);
            SetByte(index + 56, bytes.b0);
        }

        /// <summary>
        /// Exposes 64 bits of memory as bytes that can be read as several different data types.
        /// <para>Inspired by: https://stackoverflow.com/a/59273138</para>
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        private struct Bytes64
        {
            [FieldOffset(0)] public byte b0;
            [FieldOffset(1)] public byte b1;
            [FieldOffset(2)] public byte b2;
            [FieldOffset(3)] public byte b3;
            [FieldOffset(4)] public byte b4;
            [FieldOffset(5)] public byte b5;
            [FieldOffset(6)] public byte b6;
            [FieldOffset(7)] public byte b7;

            [FieldOffset(0)] public ushort vUInt16;
            [FieldOffset(0)] public uint vUInt32;
            [FieldOffset(0)] public ulong vUInt64;
            [FieldOffset(0)] public float vSingle;
            [FieldOffset(0)] public double vDouble;

            public Bytes64(byte b0, byte b1, byte b2, byte b3, byte b4, byte b5, byte b6, byte b7)
            {
                vUInt16 = 0; // required to be initialized by the C# compiler.
                vUInt32 = 0; // required to be initialized by the C# compiler.
                vUInt64 = 0; // required to be initialized by the C# compiler.
                vSingle = 0f; // required to be initialized by the C# compiler.
                vDouble = 0d; // required to be initialized by the C# compiler.

                this.b0 = b0;
                this.b1 = b1;
                this.b2 = b2;
                this.b3 = b3;
                this.b4 = b4;
                this.b5 = b5;
                this.b6 = b6;
                this.b7 = b7;
            }

            public Bytes64(byte b0, byte b1)
            {
                vUInt16 = 0; // required to be initialized by the C# compiler.
                vUInt32 = 0; // required to be initialized by the C# compiler.
                vUInt64 = 0; // required to be initialized by the C# compiler.
                vSingle = 0f; // required to be initialized by the C# compiler.
                vDouble = 0d; // required to be initialized by the C# compiler.
                b2 = 0; // required to be initialized by the C# compiler.
                b3 = 0; // required to be initialized by the C# compiler.
                b4 = 0; // required to be initialized by the C# compiler.
                b5 = 0; // required to be initialized by the C# compiler.
                b6 = 0; // required to be initialized by the C# compiler.
                b7 = 0; // required to be initialized by the C# compiler.

                this.b0 = b0;
                this.b1 = b1;
            }

            public Bytes64(byte b0, byte b1, byte b2, byte b3)
            {
                vUInt16 = 0; // required to be initialized by the C# compiler.
                vUInt32 = 0; // required to be initialized by the C# compiler.
                vUInt64 = 0; // required to be initialized by the C# compiler.
                vSingle = 0f; // required to be initialized by the C# compiler.
                vDouble = 0d; // required to be initialized by the C# compiler.
                b4 = 0; // required to be initialized by the C# compiler.
                b5 = 0; // required to be initialized by the C# compiler.
                b6 = 0; // required to be initialized by the C# compiler.
                b7 = 0; // required to be initialized by the C# compiler.

                this.b0 = b0;
                this.b1 = b1;
                this.b2 = b2;
                this.b3 = b3;
            }

            public Bytes64(ushort value)
            {
                vUInt32 = 0; // required to be initialized by the C# compiler.
                vUInt64 = 0; // required to be initialized by the C# compiler.
                vSingle = 0f; // required to be initialized by the C# compiler.
                vDouble = 0d; // required to be initialized by the C# compiler.
                b0 = 0; // required to be initialized by the C# compiler.
                b1 = 0; // required to be initialized by the C# compiler.
                b2 = 0; // required to be initialized by the C# compiler.
                b3 = 0; // required to be initialized by the C# compiler.
                b4 = 0; // required to be initialized by the C# compiler.
                b5 = 0; // required to be initialized by the C# compiler.
                b6 = 0; // required to be initialized by the C# compiler.
                b7 = 0; // required to be initialized by the C# compiler.

                vUInt16 = value;
            }

            public Bytes64(uint value)
            {
                vUInt16 = 0; // required to be initialized by the C# compiler.
                vUInt64 = 0; // required to be initialized by the C# compiler.
                vSingle = 0f; // required to be initialized by the C# compiler.
                vDouble = 0d; // required to be initialized by the C# compiler.
                b0 = 0; // required to be initialized by the C# compiler.
                b1 = 0; // required to be initialized by the C# compiler.
                b2 = 0; // required to be initialized by the C# compiler.
                b3 = 0; // required to be initialized by the C# compiler.
                b4 = 0; // required to be initialized by the C# compiler.
                b5 = 0; // required to be initialized by the C# compiler.
                b6 = 0; // required to be initialized by the C# compiler.
                b7 = 0; // required to be initialized by the C# compiler.

                vUInt32 = value;
            }

            public Bytes64(ulong value)
            {
                vUInt16 = 0; // required to be initialized by the C# compiler.
                vUInt32 = 0; // required to be initialized by the C# compiler.
                vSingle = 0f; // required to be initialized by the C# compiler.
                vDouble = 0d; // required to be initialized by the C# compiler.
                b0 = 0; // required to be initialized by the C# compiler.
                b1 = 0; // required to be initialized by the C# compiler.
                b2 = 0; // required to be initialized by the C# compiler.
                b3 = 0; // required to be initialized by the C# compiler.
                b4 = 0; // required to be initialized by the C# compiler.
                b5 = 0; // required to be initialized by the C# compiler.
                b6 = 0; // required to be initialized by the C# compiler.
                b7 = 0; // required to be initialized by the C# compiler.

                vUInt64 = value;
            }

            public Bytes64(float value)
            {
                vUInt16 = 0; // required to be initialized by the C# compiler.
                vUInt32 = 0; // required to be initialized by the C# compiler.
                vUInt64 = 0; // required to be initialized by the C# compiler.
                vDouble = 0d; // required to be initialized by the C# compiler.
                b0 = 0; // required to be initialized by the C# compiler.
                b1 = 0; // required to be initialized by the C# compiler.
                b2 = 0; // required to be initialized by the C# compiler.
                b3 = 0; // required to be initialized by the C# compiler.
                b4 = 0; // required to be initialized by the C# compiler.
                b5 = 0; // required to be initialized by the C# compiler.
                b6 = 0; // required to be initialized by the C# compiler.
                b7 = 0; // required to be initialized by the C# compiler.

                vSingle = value;
            }

            public Bytes64(double value)
            {
                vUInt16 = 0; // required to be initialized by the C# compiler.
                vUInt32 = 0; // required to be initialized by the C# compiler.
                vUInt64 = 0; // required to be initialized by the C# compiler.
                vSingle = 0f; // required to be initialized by the C# compiler.
                b0 = 0; // required to be initialized by the C# compiler.
                b1 = 0; // required to be initialized by the C# compiler.
                b2 = 0; // required to be initialized by the C# compiler.
                b3 = 0; // required to be initialized by the C# compiler.
                b4 = 0; // required to be initialized by the C# compiler.
                b5 = 0; // required to be initialized by the C# compiler.
                b6 = 0; // required to be initialized by the C# compiler.
                b7 = 0; // required to be initialized by the C# compiler.

                vDouble = value;
            }
        }

        #endregion Setting and Getting Bytes, Integers and Floats

        #region IReadOnlyCollection<bool> Implementation

        int IReadOnlyCollection<bool>.Count => _Size;

        public IEnumerator<bool> GetEnumerator()
        {
            for (int i = 0; i < _Size; i++)
                yield return this[i];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion IReadOnlyCollection<bool> Implementation

        #region ICloneable Implementation

        public object Clone()
        {
            return new BitArray(this);
        }

        #endregion ICloneable Implementation

        /// <summary>
        /// Copies all the bits of the current <see cref="BitArray"/> to the specified <see
        /// cref="BitArray"/>. The <paramref name="destination"/> must have the same amount of bits.
        /// </summary>
        /// <param name="destination">The destination <see cref="BitArray"/> to write to.</param>
        /// <exception cref="ArgumentNullException">Value is null.</exception>
        /// <exception cref="ArgumentException">
        /// The value and the current <see cref="BitArray"/> do not have the same number of bits.
        /// </exception>
        public void CopyTo(BitArray destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            if (_Size != destination._Size) throw new ArgumentException("The destination and the current " + nameof(BitArray) + " do not have the same number of bits.");

            _Data.CopyTo(destination._Data, 0);
        }
    }
}