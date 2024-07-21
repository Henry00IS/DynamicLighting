using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Jobs;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>
    /// Unity job that fetches <see cref="Transform.position"/> vectors from an array of transforms.
    /// </summary>
    internal unsafe struct GetTransformPositionsJob : IJobParallelForTransform
    {
        /// <summary>
        /// Pointer pointing towards a <see cref="Vector3[]"/> to be filled with positions (must
        /// have at least the size of the input <see cref="TransformAccessArray"/>.
        /// </summary>
        [NativeDisableUnsafePtrRestriction]
        [ReadOnly]
        public Vector3* outPositions;

        /// <summary>Creates a new instance of <see cref="GetTransformPositionsJob"/>.</summary>
        /// <param name="outPositions">
        /// Pointer pointing towards a <see cref="Vector3[]"/> to be filled with positions (must
        /// have at least the size of the input <see cref="TransformAccessArray"/>.
        /// </param>
        public GetTransformPositionsJob(Vector3* outPositions)
        {
            this.outPositions = outPositions;
        }

        public void Execute(int index, TransformAccess transform)
        {
            if (transform.isValid)
                outPositions[index] = transform.position;
        }
    }
}