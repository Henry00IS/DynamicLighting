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

using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>
    /// Represents a 3-dimensional vector with 8-bit-precision floating-point numbers (-1.0f to 1.0f).
    /// <para><b>-1.0f, NaN, NegativeInfinity</b> = -1.0f.</para>
    /// <para><b>0.0f, -0.0f</b> = 0.0f.</para>
    /// <para><b>1.0f, PositiveInfinity</b> = 1.0f.</para>
    /// </summary>
    internal struct NormalFloat8Vector3
    {
        /// <summary>The X component of the vector.</summary>
        public NormalFloat8 x;

        /// <summary>The Y component of the vector.</summary>
        public NormalFloat8 y;

        /// <summary>The Z component of the vector.</summary>
        public NormalFloat8 z;

        /// <summary>Creates a new <see cref="NormalFloat8Vector3"/>.</summary>
        /// <param name="x">The X component of the vector.</param>
        /// <param name="y">The Y component of the vector.</param>
        /// <param name="z">The Z component of the vector.</param>
        public NormalFloat8Vector3(NormalFloat8 x, NormalFloat8 y, NormalFloat8 z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        /// <summary>Implicit operator that converts a <see cref="NormalFloat8Vector3"/> to <see cref="Vector3"/>.</summary>
        /// <param name="v">The <see cref="NormalFloat8Vector3"/> to be converted.</param>
        public static implicit operator Vector3(NormalFloat8Vector3 v)
        {
            return new Vector3(v.x, v.y, v.z);
        }

        /// <summary>Implicit operator that converts a <see cref="Vector3"/> to <see cref="NormalFloat8Vector3"/>.</summary>
        /// <param name="f8">The <see cref="float"/> to be converted.</param>
        public static implicit operator NormalFloat8Vector3(Vector3 v)
        {
            return new NormalFloat8Vector3(v.x, v.y, v.z);
        }

        /// <summary>
        /// Converts the numeric vector value of this instance to its equivalent string representation.
        /// </summary>
        /// <returns>The string representation of the value of this instance.</returns>
        public override string ToString()
        {
            return string.Format("{0}, {1}, {2}", x, y, z);
        }
    }
}