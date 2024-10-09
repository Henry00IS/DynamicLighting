using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>
    /// Represents a 3-dimensional vector with 8-bit-precision floating-point numbers (-1.0f to 1.0f).
    /// <para><b>-1.0f, NaN, NegativeInfinity</b> = -1.0f.</para>
    /// <para><b>0.0f, -0.0f</b> = 0.0f.</para>
    /// <para><b>1.0f, PositiveInfinity</b> = 1.0f.</para>
    /// </summary>
    internal struct MiniVector3
    {
        /// <summary>The X component of the vector.</summary>
        public float8 x;

        /// <summary>The Y component of the vector.</summary>
        public float8 y;

        /// <summary>The Z component of the vector.</summary>
        public float8 z;

        /// <summary>Creates a new <see cref="MiniVector3"/>.</summary>
        /// <param name="x">The X component of the vector.</param>
        /// <param name="y">The Y component of the vector.</param>
        /// <param name="z">The Z component of the vector.</param>
        public MiniVector3(float8 x, float8 y, float8 z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        /// <summary>Implicit operator that converts a <see cref="MiniVector3"/> to <see cref="Vector3"/>.</summary>
        /// <param name="v">The <see cref="MiniVector3"/> to be converted.</param>
        public static implicit operator Vector3(MiniVector3 v)
        {
            return new Vector3(v.x, v.y, v.z);
        }

        /// <summary>Implicit operator that converts a <see cref="Vector3"/> to <see cref="MiniVector3"/>.</summary>
        /// <param name="f8">The <see cref="float"/> to be converted.</param>
        public static implicit operator MiniVector3(Vector3 v)
        {
            return new MiniVector3(v.x, v.y, v.z);
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