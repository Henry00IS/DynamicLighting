using System.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>
    /// Represents a high color value (5 bits for red, 6 bits for green, 5 bits for blue) with a
    /// bias that allows for more shades of gray in the lower end as this class is intended for
    /// indirect bounce lighting that is typically very dark.
    /// </summary>
    public struct HighColor
    {
        /// <summary>Gets or sets the 16-bit color.</summary>
        public ushort color;

        /// <summary>Gets or sets the red component (0-31).</summary>
        public byte r
        {
            get => (byte)((color >> 11) & 0x1F);
            set => color = (ushort)((color & 0x07FF) | ((value & 0x1F) << 11));
        }

        /// <summary>Gets or sets the green component (0-63).</summary>
        public byte g
        {
            get => (byte)((color >> 5) & 0x3F);
            set => color = (ushort)((color & 0xF81F) | ((value & 0x3F) << 5));
        }

        /// <summary>Gets or sets the blue component (0-31).</summary>
        public byte b
        {
            get => (byte)(color & 0x1F);
            set => color = (ushort)((color & 0xFFE0) | (value & 0x1F));
        }

        /// <summary>
        /// Creates a new <see cref="HighColor"/> from three floating point values.
        /// </summary>
        /// <param name="r">The red channel (0.0 - 1.0).</param>
        /// <param name="g">The green channel (0.0 - 1.0).</param>
        /// <param name="b">The blue channel (0.0 - 1.0).</param>
        public HighColor(float r, float g, float b)
        {
            color = 0; // required by the c# compiler.
            this.r = (byte)(math.pow(math.saturate(r), 1.0f / 2.0f) * 31f);
            this.g = (byte)(math.pow(math.saturate(g), 1.0f / 2.0f) * 63f);
            this.b = (byte)(math.pow(math.saturate(b), 1.0f / 2.0f) * 31f);
        }

        public HighColor(Color color) : this(color.r, color.g, color.b)
        {
        }
    }
}