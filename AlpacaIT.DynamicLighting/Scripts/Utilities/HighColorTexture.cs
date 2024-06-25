using System;
using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>
    /// Special texture format that uses high color (5 bits for red, 6 bits for green, 5 bits for
    /// blue). This is used in the shader for compressed bounce lighting data.
    /// </summary>
    public class HighColorTexture
    {
        /// <summary>The internal array of 16-bit elements that store the colors.</summary>
        private readonly HighColor[,] _Data;

        /// <summary>
        /// Creates a new instance of <see cref="HighColorTexture"/> with the specified amount of pixels.
        /// </summary>
        /// <param name="width">The amount of pixels to be stored in the texture horizontally.</param>
        /// <param name="height">The amount of pixels to be stored in the texture vertically.</param>
        public HighColorTexture(int width, int height)
        {
            if (width < 0) throw new ArgumentOutOfRangeException(nameof(width), "Non-negative number required.");
            if (height < 0) throw new ArgumentOutOfRangeException(nameof(height), "Non-negative number required.");

            // create the internal two-dimensional array of pixels.
            _Data = new HighColor[width, height];
        }

        /// <summary>
        /// Creates a new instance of <see cref="HighColorTexture"/> from an existing <see cref="Color[]"/>.
        /// </summary>
        /// <param name="width">The amount of colors to be stored in the array horizontally.</param>
        /// <param name="height">The amount of colors to be stored in the array vertically.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The width and height must match the amount of colors in the given color array.
        /// </exception>
        public HighColorTexture(Color[] colors, int width, int height)
        {
            if (colors == null) throw new ArgumentNullException(nameof(colors));
            if (width < 0) throw new ArgumentOutOfRangeException(nameof(width), "Non-negative number required.");
            if (height < 0) throw new ArgumentOutOfRangeException(nameof(height), "Non-negative number required.");
            if (width * height != colors.Length) throw new ArgumentOutOfRangeException("width,height", "The width and height must match the amount of colors in the given color array.");

            _Data = new HighColor[width, height];

            // copy the one-dimensional array of colors.
            for (int y = 0; y < height; y++)
            {
                int yPtr = y * width;

                for (int x = 0; x <= width; x++)
                {
                    int xyPtr = yPtr + x;

                    _Data[x, y] = new HighColor(colors[xyPtr]);
                }
            }
        }

        /// <summary>Retrieves a <see cref="uint[]"/> containing all of the colors in the texture.</summary>
        /// <returns>The <see cref="uint[]"/> containing all of the colors.</returns>
        public uint[] ToUInt32Array()
        {
            int width = _Data.GetLength(0);
            int height = _Data.GetLength(1);
            var result = new uint[width * height / 2];

            int index = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x += 2)
                {
                    ushort high = _Data[x, y].color;
                    ushort low = _Data[x + 1, y].color;
                    result[index++] = (uint)((high << 16) | low);
                }
            }

            return result;
        }
    }
}