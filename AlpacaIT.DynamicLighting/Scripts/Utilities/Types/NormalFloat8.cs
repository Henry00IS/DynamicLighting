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

namespace AlpacaIT.DynamicLighting
{
    /// <summary>
    /// Represents an 8-bit-precision floating-point number (-1.0f to 1.0f).
    /// <para><b>-1.0f, NaN, NegativeInfinity</b> = -1.0f.</para>
    /// <para><b>0.0f, -0.0f</b> = 0.0f.</para>
    /// <para><b>1.0f, PositiveInfinity</b> = 1.0f.</para>
    /// </summary>
    internal struct NormalFloat8
    {
        /// <summary>
        /// The internal 8-bit byte (0-254).
        /// <para>For precise rounding the value 255 is unused (could be used to implement NaN).</para>
        /// </summary>
        public readonly byte byteValue;

        /// <summary>Represents the smallest possible value of <see cref="NormalFloat8"/>.</summary>
        public const float MinValue = -1.0f;
        /// <summary>Represents the largest possible value of <see cref="NormalFloat8"/>.</summary>
        public const float MaxValue = 1.0f;

        /// <summary>
        /// Represents the smallest positive <see cref="NormalFloat8"/> value that is greater than zero.
        /// </summary>
        public const float Epsilon = 0.007874012f;

        /// <summary>Used internally to map the byte to a float.</summary>
        private const float Scale = 2.0f / 254.0f;
        /// <summary>Used internally to map the float to a byte.</summary>
        private const float InvScale = 254.0f / 2.0f;

        /// <summary>
        /// Creates a new <see cref="NormalFloat8"/> from the given <see cref="float"/> (an implicit
        /// conversion is available).
        /// </summary>
        /// <param name="f">The single-precision floating point value to be converted.</param>
        public NormalFloat8(float f)
        {
            if (f < -1f) f = -1f;
            if (f > 1f) f = 1f;
            byteValue = (byte)((f + 1.0f) * InvScale);
        }

        /// <summary>Implicit operator that converts a <see cref="NormalFloat8"/> to <see cref="float"/>.</summary>
        /// <param name="f8">The <see cref="NormalFloat8"/> to be converted.</param>
        public static implicit operator float(NormalFloat8 f8)
        {
            return f8.byteValue * Scale - 1.0f;
        }

        /// <summary>Implicit operator that converts a <see cref="float"/> to <see cref="NormalFloat8"/>.</summary>
        /// <param name="f8">The <see cref="float"/> to be converted.</param>
        public static implicit operator NormalFloat8(float f)
        {
            return new NormalFloat8(f);
        }

        /// <summary>
        /// Converts the numeric floating-point value of this instance to its equivalent string representation.
        /// </summary>
        /// <returns>The string representation of the value of this instance.</returns>
        public override string ToString()
        {
            return ((float)this).ToString();
        }
    }
}