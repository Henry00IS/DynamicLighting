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
    }
}