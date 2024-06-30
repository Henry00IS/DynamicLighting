using System;
using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>
    /// Special texture format that uses high color (5 bits for red, 6 bits for green, 5 bits for
    /// blue). This is used in the shader for compressed bounce lighting data.
    /// </summary>
    public class HighColorTexture : ICloneable
    {
        /// <summary>The internal one-dimensional array of 16-bit elements that store the colors.</summary>
        private readonly HighColor[] _Data;

        /// <summary>The width of the texture in pixels.</summary>
        private readonly int _Width;

        /// <summary>The height of the texture in pixels.</summary>
        private readonly int _Height;

        /// <summary>The width of the texture in pixels.</summary>
        public int Width => _Width;

        /// <summary>The height of the texture in pixels.</summary>
        public int Height => _Height;

        /// <summary>Creates a new instance of <see cref="HighColorTexture"/> for cloning.</summary>
        private HighColorTexture()
        {
            // invalid until private properties are manually assigned.
        }

        /// <summary>
        /// Creates a new instance of <see cref="HighColorTexture"/> and copies the colors from
        /// another <see cref="HighColorTexture"/>.
        /// </summary>
        /// <param name="original">The texture to be copied into this new instance.</param>
        public HighColorTexture(HighColorTexture original)
        {
            if (original == null) throw new ArgumentNullException(nameof(original));
            _Data = (HighColor[])original._Data.Clone();
            _Width = original._Width;
            _Height = original._Height;
        }

        /// <summary>
        /// Creates a new instance of <see cref="HighColorTexture"/> with the specified amount of pixels.
        /// </summary>
        /// <param name="width">The amount of pixels to be stored in the texture horizontally.</param>
        /// <param name="height">The amount of pixels to be stored in the texture vertically.</param>
        public HighColorTexture(int width, int height)
        {
            if (width < 0) throw new ArgumentOutOfRangeException(nameof(width), "Non-negative number required.");
            if (height < 0) throw new ArgumentOutOfRangeException(nameof(height), "Non-negative number required.");
            _Width = width;
            _Height = height;

            // create the internal one-dimensional array of colors.
            _Data = new HighColor[width * height];
        }

        /// <summary>Gets or sets a color at the specified pixel coordinates.</summary>
        /// <param name="x">The X-Coordinate in the texture (up to the width).</param>
        /// <param name="y">The Y-Coordinate in the texture (up to the height).</param>
        /// <returns>The color at the specified pixel coordinates.</returns>
        public HighColor this[int x, int y]
        {
            get
            {
                if (x < 0 || x >= _Width || y < 0 || y >= _Height) throw new IndexOutOfRangeException("Index was outside the bounds of the array.");
                return _Data[x + y * _Width];
            }
            set
            {
                if (x < 0 || x >= _Width || y < 0 || y >= _Height) throw new IndexOutOfRangeException("Index was outside the bounds of the array.");
                _Data[x + y * _Width] = value;
            }
        }

        /// <summary>Retrieves a <see cref="uint[]"/> containing all of the colors in the texture.</summary>
        /// <returns>The <see cref="uint[]"/> containing all of the colors.</returns>
        public uint[] ToUInt32Array()
        {
            var result = new uint[_Width * _Height];
            var index = 0;
            for (int y = 0; y < _Height; y++)
            {
                for (int x = 0; x < _Width; x++)
                {
                    result[index++] = _Data[y * _Width + x].color;
                }
            }
            return result;
        }

        /// <summary>
        /// This is no good. Fixme. Replaceme.
        /// </summary>
        public void Dilate()
        {
            var copy = new HighColorTexture(this);

            for (int y = 0; y < _Height; y++)
            {
                for (int x = 0; x < _Width; x++)
                {
                    // todo: optimize this (very wasteful iterations).
                    //if (x >= 2 && y >= 2 && x < _Width - 2 && y < _Height - 2)
                    //continue;

                    var c = copy[x, y];
                    c = !c.isBlack ? c : TryGetShadowBit(copy, x - 1, y - 1);
                    c = !c.isBlack ? c : TryGetShadowBit(copy, x, y - 1);
                    c = !c.isBlack ? c : TryGetShadowBit(copy, x + 1, y - 1);
                    c = !c.isBlack ? c : TryGetShadowBit(copy, x - 1, y);
                    c = !c.isBlack ? c : TryGetShadowBit(copy, x + 1, y);
                    c = !c.isBlack ? c : TryGetShadowBit(copy, x - 1, y + 1);
                    c = !c.isBlack ? c : TryGetShadowBit(copy, x, y + 1);
                    c = !c.isBlack ? c : TryGetShadowBit(copy, x + 1, y + 1);
                    this[x, y] = c;
                }
            }
        }

        private HighColor TryGetShadowBit(HighColorTexture texture, int x, int y)
        {
            if (x >= 0 && y >= 0 && x < texture.Width && y < texture.Height)
                return texture[x, y];
            return new HighColor();
        }

        #region ICloneable Implementation

        public object Clone()
        {
            return new HighColorTexture(this);
        }

        #endregion ICloneable Implementation

        ///// <summary>The internal array of 16-bit elements that store the colors.</summary>
        //private readonly HighColor[,] _Data;
        //
        ///// <summary>
        ///// Creates a new instance of <see cref="HighColorTexture"/> with the specified amount of pixels.
        ///// </summary>
        ///// <param name="width">The amount of pixels to be stored in the texture horizontally.</param>
        ///// <param name="height">The amount of pixels to be stored in the texture vertically.</param>
        //public HighColorTexture(int width, int height)
        //{
        //    if (width < 0) throw new ArgumentOutOfRangeException(nameof(width), "Non-negative number required.");
        //    if (height < 0) throw new ArgumentOutOfRangeException(nameof(height), "Non-negative number required.");
        //
        //    // create the internal two-dimensional array of pixels.
        //    _Data = new HighColor[width, height];
        //}
        //
        ///// <summary>
        ///// Creates a new instance of <see cref="HighColorTexture"/> from an existing <see cref="Color[]"/>.
        ///// </summary>
        ///// <param name="width">The amount of colors to be stored in the array horizontally.</param>
        ///// <param name="height">The amount of colors to be stored in the array vertically.</param>
        ///// <exception cref="ArgumentOutOfRangeException">
        ///// The width and height must match the amount of colors in the given color array.
        ///// </exception>
        //public HighColorTexture(Color[] colors, int width, int height)
        //{
        //    if (colors == null) throw new ArgumentNullException(nameof(colors));
        //    if (width < 0) throw new ArgumentOutOfRangeException(nameof(width), "Non-negative number required.");
        //    if (height < 0) throw new ArgumentOutOfRangeException(nameof(height), "Non-negative number required.");
        //    if (width * height != colors.Length) throw new ArgumentOutOfRangeException("width,height", "The width and height must match the amount of colors in the given color array.");
        //
        //    _Data = new HighColor[width, height];
        //
        //    // copy the one-dimensional array of colors.
        //    for (int y = 0; y < height; y++)
        //    {
        //        int yPtr = y * width;
        //
        //        for (int x = 0; x <= width; x++)
        //        {
        //            int xyPtr = yPtr + x;
        //
        //            _Data[x, y] = new HighColor(colors[xyPtr]);
        //        }
        //    }
        //}
        //
        ///// <summary>Retrieves a <see cref="uint[]"/> containing all of the colors in the texture.</summary>
        ///// <returns>The <see cref="uint[]"/> containing all of the colors.</returns>
        //public uint[] ToUInt32Array()
        //{
        //    int width = _Data.GetLength(0);
        //    int height = _Data.GetLength(1);
        //    var result = new uint[width * height / 2];
        //
        //    int index = 0;
        //    for (int y = 0; y < height; y++)
        //    {
        //        for (int x = 0; x < width; x += 2)
        //        {
        //            ushort high = _Data[x, y].color;
        //            ushort low = _Data[x + 1, y].color;
        //            result[index++] = (uint)((high << 16) | low);
        //        }
        //    }
        //
        //    return result;
        //}
    }
}