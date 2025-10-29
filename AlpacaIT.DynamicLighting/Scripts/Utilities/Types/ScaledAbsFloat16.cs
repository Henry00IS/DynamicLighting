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
    /// Represents a 16-bit-precision floating-point number (0.0f to 'scale'). The 'scale' affects
    /// the precision of the floating-point number, smaller scales have more precision.
    /// <para><b>-1.0f, 0.0f, -0.0f, NaN, NegativeInfinity</b> = 0.0f.</para>
    /// <para><b>'scale', PositiveInfinity</b> = 'scale'.</para>
    /// </summary>
    internal struct ScaledAbsFloat16
    {
        /// <summary>The internal 16-bit unsigned integer (0-65535).</summary>
        public readonly ushort ushortValue;

        /// <summary>Creates a new <see cref="ScaledAbsFloat16"/> from the given <see cref="float"/>.</summary>
        /// <param name="f">The single-precision floating point value to be converted.</param>
        /// <param name="scale">The scale (maximum number) representable by this instance.</param>
        public ScaledAbsFloat16(float f, float scale)
        {
            if (scale == 0f) { ushortValue = 0; return; }
            if (f > scale) f = scale;
            if (f < 0f) f = 0f;
            ushortValue = (ushort)(f / scale * ushort.MaxValue);
        }

        /// <summary>Converts the <see cref="ScaledAbsFloat16"/> to <see cref="float"/>.</summary>
        /// <param name="scale">The scale (maximum number) representable by this instance.</param>
        /// <returns>The floating-point representation of this instance.</returns>
        public float ToFloat(float scale)
        {
            return ushortValue / (float)ushort.MaxValue * scale;
        }

        #region Packing

        /// <summary>Creates a new <see cref="ScaledAbsFloat16"/> from the given <see cref="ushort"/>.</summary>
        /// <param name="ushortValue">The internal number representation.</param>
        private ScaledAbsFloat16(ushort ushortValue)
        {
            this.ushortValue = ushortValue;
        }

        /// <summary>
        /// Combines two <see cref="ScaledAbsFloat16"/> instances into a single 32-bit unsigned
        /// integer. The <paramref name="left"/> instance's <see cref="ushortValue"/> is shifted
        /// into the most significant 16 bits (leftmost), and the <paramref name="right"/>
        /// instance's <see cref="ushortValue"/> is placed in the least significant 16 bits (rightmost).
        /// </summary>
        /// <param name="left">The <see cref="ScaledAbsFloat16"/> to place in the high 16 bits.</param>
        /// <param name="right">The <see cref="ScaledAbsFloat16"/> to place in the low 16 bits.</param>
        /// <returns>A <see cref="uint"/> with the combined 16-bit values.</returns>
        public static uint Pack(ScaledAbsFloat16 left, ScaledAbsFloat16 right)
        {
            return ((uint)left.ushortValue << 16) | right.ushortValue;
        }

        /// <summary>
        /// Splits a 32-bit unsigned integer into two <see cref="ScaledAbsFloat16"/> instances. The
        /// most significant 16 bits (leftmost) are used to create the 'left' instance, and the
        /// least significant 16 bits (rightmost) are used to create the 'right' instance. This is
        /// the inverse operation of <see cref="Pack"/>.
        /// </summary>
        /// <param name="value">The <see cref="uint"/> containing the combined 16-bit values.</param>
        /// <returns>
        /// A tuple containing the left (leftmost bits) and right (rightmost bits) <see
        /// cref="ScaledAbsFloat16"/> instances.
        /// </returns>
        public static (ScaledAbsFloat16 left, ScaledAbsFloat16 right) Unpack(uint value)
        {
            ushort leftUshort = (ushort)(value >> 16);
            ushort rightUshort = (ushort)value;
            return (new ScaledAbsFloat16(leftUshort), new ScaledAbsFloat16(rightUshort));
        }

        #endregion Packing
    }
}