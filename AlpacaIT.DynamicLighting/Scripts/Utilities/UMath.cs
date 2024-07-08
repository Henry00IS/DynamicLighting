using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>
    /// Internal math library with advanced general-purpose computations that use unsafe operations
    /// to speed up equations significantly and provides in-place modification of the original structs.
    /// <para>These functions have all been benchmarked in the Unity Editor environment.</para>
    /// <para>There are no copy&amp;paste variations for all possible types, only proven fast functions.</para>
    /// </summary>
    internal static unsafe class UMath
    {
        /// <summary>Applies the componentwise absolute value of a float3 vector.</summary>
        /// <param name="value">Input value.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Abs(float3* value)
        {
            *(uint*)&value->x &= 0x7FFFFFFFu;
            *(uint*)&value->y &= 0x7FFFFFFFu;
            *(uint*)&value->z &= 0x7FFFFFFFu;
        }

        /// <summary>Applies the result of a componentwise multiplication operation on a float2 vector and a float value.</summary>
        /// <param name="lhs">Left hand side float2 to use to compute componentwise multiplication.</param>
        /// <param name="rhs">Right hand side float to use to compute componentwise multiplication.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Scale(float2* lhs, float rhs)
        {
            lhs->x *= rhs;
            lhs->y *= rhs;
        }

        /// <summary>Applies the result of a componentwise multiplication operation on a float3 vector and a float value.</summary>
        /// <param name="lhs">Left hand side float3 to use to compute componentwise multiplication.</param>
        /// <param name="rhs">Right hand side float to use to compute componentwise multiplication.</param>
        /// <returns>float3 result of the componentwise multiplication.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Scale(float3* lhs, float rhs)
        {
            lhs->x *= rhs;
            lhs->y *= rhs;
            lhs->z *= rhs;
        }

        /// <summary>Applies the result of a componentwise addition operation on a float2 vector and a float value.</summary>
        /// <param name="lhs">Left hand side float2 to use to compute componentwise addition.</param>
        /// <param name="rhs">Right hand side float to use to compute componentwise addition.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Add(float2* lhs, float rhs)
        {
            lhs->x += rhs;
            lhs->y += rhs;
        }

        /// <summary>Returns the result of a componentwise addition operation on two float3 vectors.</summary>
        /// <param name="lhs">Left hand side float3 to use to compute componentwise addition.</param>
        /// <param name="rhs">Right hand side float3 to use to compute componentwise addition.</param>
        /// <returns>float3 result of the componentwise addition.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Add(float3* lhs, float3* rhs)
        {
            lhs->x += rhs->x;
            lhs->y += rhs->y;
            lhs->z += rhs->z;
        }

        /// <summary>Applies the result of a componentwise unary minus operation on a float3 vector.</summary>
        /// <param name="val">Value to use when computing the componentwise unary minus.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Negate(float3* val)
        {
            val->x = -val->x;
            val->y = -val->y;
            val->z = -val->z;
        }

        /// <summary>Applies the result of a componentwise subtraction operation on two float3 vectors.</summary>
        /// <param name="lhs">Left hand side float3 to use to compute componentwise subtraction.</param>
        /// <param name="rhs">Right hand side float3 to use to compute componentwise subtraction.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Subtract(float3* lhs, float3* rhs)
        {
            lhs->x -= rhs->x;
            lhs->y -= rhs->y;
            lhs->z -= rhs->z;
        }

        /// <summary>Returns the dot product of two float3 vectors.</summary>
        /// <param name="x">The first vector.</param>
        /// <param name="y">The second vector.</param>
        /// <returns>The dot product of two vectors.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Dot(float3* x, float3* y)
        {
            return x->x * y->x + x->y * y->y + x->z * y->z;
        }

        /// <summary>Applies a normalized version of the float3 vector x by scaling it by 1 / length(x).</summary>
        /// <param name="x">Vector to normalize.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Normalize(float3* x)
        {
            Scale(x, math.rsqrt(Dot(x, x)));
        }
    }
}