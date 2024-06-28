using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>
    /// Represents a rectangle in pixel coordinates that encompasses a triangle. It helps convert
    /// coordinates and can add padding. It guarantees a 2x2 texture size where min and max are
    /// within array bounds (0-1 in this example) and always encompasses a pixel.
    /// </summary>
    internal struct PixelTriangleRect
    {
        /// <summary>The given size of the texture (guaranteed to be at least 2).</summary>
        public readonly int textureSize;
        /// <summary>The given size of the texture minus 1 (guaranteed to be at least 1).</summary>
        public readonly int textureSizeMin1;

        /// <summary>The minimum x-position (guaranteed to be within 0 - <see cref="textureSizeMin1"/> and smaller than <see cref="xMax"/>.</summary>
        public readonly int xMin;
        /// <summary>The minimum y-position (guaranteed to be within 0 - <see cref="textureSizeMin1"/> and smaller than <see cref="yMax"/>.</summary>
        public readonly int yMin;
        /// <summary>The maximum x-position (guaranteed to be within 0 - <see cref="textureSizeMin1"/> and larger than <see cref="xMin"/>.</summary>
        public readonly int xMax;
        /// <summary>The maximum y-position (guaranteed to be within 0 - <see cref="textureSizeMin1"/> and larger than <see cref="yMin"/>.</summary>
        public readonly int yMax;

        /// <summary>Creates a new <see cref="PixelTriangleRect"/>.</summary>
        /// <param name="textureSize">The total texture dimension (e.g. 256 for 256x256).</param>
        /// <param name="triangleRect">The triangle rectangle in UV coordinates.</param>
        public PixelTriangleRect(int textureSize, Rect triangleRect)
        {
            // ensure there is at least one pixel.
            if (textureSize <= 1)
                textureSize = 2;

            this.textureSize = textureSize;
            textureSizeMin1 = textureSize - 1;

            // clamp the UV coordinates to be within 0.0 - 1.0 range.
            var triangleRect_xMin = Mathf.Clamp01(triangleRect.xMin);
            var triangleRect_yMin = Mathf.Clamp01(triangleRect.yMin);
            var triangleRect_xMax = Mathf.Clamp01(triangleRect.xMax);
            var triangleRect_yMax = Mathf.Clamp01(triangleRect.yMax);

            // convert into texture coordinates that can be used with 2d array operations.
            xMin = Mathf.FloorToInt(triangleRect_xMin * textureSizeMin1);
            yMin = Mathf.FloorToInt(triangleRect_yMin * textureSizeMin1);
            xMax = Mathf.CeilToInt(triangleRect_xMax * textureSizeMin1);
            yMax = Mathf.CeilToInt(triangleRect_yMax * textureSizeMin1);

            // 1. check the uv 1.0 borders:

            // we know that max x will also be the texture size.
            if (xMin == textureSizeMin1)
                xMin--;

            // we know that max y will also be the texture size.
            if (yMin == textureSizeMin1)
                yMin--;

            // 2. check for min being the same as max.

            // check for min x being the same as max x.
            if (xMin == xMax)
            {
                // try moving min left.
                if (xMin > 0)
                {
                    xMin--;
                }
                // otherwise move max right.
                else
                {
                    xMax++;
                }
            }

            // check for min y being the same as max y.
            if (yMin == yMax)
            {
                // try moving min up.
                if (yMin > 0)
                {
                    yMin--;
                }
                // otherwise move max down.
                else
                {
                    yMax++;
                }
            }
        }

        // todo: fact check this comment.
        /// <summary>Gets the width of the rectangle (guaranteed to be at least 1).</summary>
        public readonly int width => xMax - xMin;

        // todo: fact check this comment.
        /// <summary>Gets the height of the rectangle (guaranteed to be at least 1).</summary>
        public readonly int height => yMax - yMin;
    }
}