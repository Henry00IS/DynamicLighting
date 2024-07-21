using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Jobs;

namespace AlpacaIT.DynamicLighting
{
    // implements accelerated transform position retrieval using the unity job system.

    public partial class DynamicLightManager
    {
        /// <summary>Contains an array of <see cref="Transform"/> used with <see cref="GetTransformPositionsJob"/>.</summary>
        [HideInInspector]
        internal TransformAccessArray lightPositionsRaycastedLightTransforms;

        /// <summary>Contains an array of <see cref="Vector3"/> positions after <see cref="GetTransformPositionsJob"/>.</summary>
        [HideInInspector]
        internal Vector3[] lightPositionsRaycastedLightPositions;

        /// <summary>Initialization of the DynamicLightManager.LightPositions partial class.</summary>
        private void LightPositionsInitialize()
        {
            var raycastedDynamicLightsCount = raycastedDynamicLights.Count;

            lightPositionsRaycastedLightTransforms = new TransformAccessArray(raycastedDynamicLightsCount);
            lightPositionsRaycastedLightPositions = new Vector3[raycastedDynamicLightsCount];

            // iterate over all raycasted dynamic light sources:
            for (int i = 0; i < raycastedDynamicLightsCount; i++)
            {
                var raycastedDynamicLight = raycastedDynamicLights[i];

                // destroyed raycasted lights in the scene, must still exist in the shader.
                if (!raycastedDynamicLight.lightAvailable)
                {
                    // add invalid transform to the native array to keep the indices correct:
                    lightPositionsRaycastedLightTransforms.Add(null);
                }
                else
                {
                    // add the valid transform to the native array (important to make reloads work
                    // as light sources will not register themselves again after a reload):
                    lightPositionsRaycastedLightTransforms.Add(raycastedDynamicLight.light.transform);
                }
            }
        }

        /// <summary>Cleanup of the DynamicLightManager.LightPositions partial class.</summary>
        private void LightPositionsCleanup()
        {
            if (lightPositionsRaycastedLightTransforms.isCreated)
                lightPositionsRaycastedLightTransforms.Dispose();

            lightPositionsRaycastedLightPositions = null;
        }

        /// <summary>Called before the lights are iterated for updates.</summary>
        private unsafe void LightPositionsUpdate()
        {
            // fetch all of the light positions using the unity job system:
            fixed (Vector3* lightPositionsRaycastedLightPositionsPtr = lightPositionsRaycastedLightPositions)
            {
                var job = new GetTransformPositionsJob(lightPositionsRaycastedLightPositionsPtr);
                job.ScheduleReadOnly(lightPositionsRaycastedLightTransforms, 32).Complete();
            }
        }

        /// <summary>
        /// Especially for the undo system in Unity Editor where upon deletion we may have lost the
        /// transform, we fetch the transform whenever a light source gets enabled. This can also
        /// happen when the light is disabled and not available at initialization. The transform
        /// property of the dynamic light source is cached so it should be fast.
        /// <para>Assumes that <see cref="RaycastedDynamicLight.lightAvailable"/> is true.</para>
        /// </summary>
        /// <param name="raycastedDynamicLightsIndex">
        /// The index into the <see cref="raycastedDynamicLights"/> array.
        /// </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LightPositionsOnRaycastedDynamicLightAvailable(int raycastedDynamicLightsIndex)
        {
            lightPositionsRaycastedLightTransforms[raycastedDynamicLightsIndex] = raycastedDynamicLights[raycastedDynamicLightsIndex].light.transform;
        }
    }
}