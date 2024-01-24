using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    // implements post-processing shader acceleration code that ensures that only volumetric light
    // sources are present on the graphics card. the alternative has been an iteration over all
    // light sources in the scene to filter through them per fragment, which was very slow.

    public partial class DynamicLightManager
    {
        /// <summary>
        /// The <see cref="ComputeBuffer"/> used to replace <see cref="dynamicLightsBuffer"/> during rendering.
        /// </summary>
        private ComputeBuffer postProcessingDynamicLightsBuffer;

        /// <summary>
        /// Contains all of the volumetric lights ready to be passed to the graphics card for post processing.
        /// </summary>
        private ShaderDynamicLight[] postProcessingShaderVolumetricLights;

        /// <summary>
        /// The amount of volumetric light sources in the <see
        /// cref="postProcessingShaderVolumetricLights"/> array.
        /// </summary>
        internal int postProcessingVolumetricLightsCount;

        /// <summary>
        /// Initialization of the DynamicLightManager.PostProcessing partial class.
        /// </summary>
        private void PostProcessingInitialize()
        {
            // allocate the required arrays according to our budget.
            postProcessingShaderVolumetricLights = new ShaderDynamicLight[totalLightBudget];
        }

        /// <summary>
        /// Whenever the dynamic or realtime light budgets change the shader buffers are updated in
        /// the DynamicLightManager.PostProcessing partial class.
        /// </summary>
        private void PostProcessingReallocateShaderLightBuffer()
        {
            // re-allocate the required arrays according to our new budget.
            postProcessingShaderVolumetricLights = new ShaderDynamicLight[totalLightBudget];
        }

        private unsafe void PostProcessingProcessLight(ShaderDynamicLight* shaderLight, DynamicLight light)
        {
            // the shader light must still be active.
            if (shaderLight->radiusSqr == -1.0f) return;

            // the light must be volumetric.
            if (light.lightVolumetricType == DynamicLightVolumetricType.None) return;

            // nothing to do when the volumetric intensity is zero.
            if (shaderLight->volumetricIntensity == 0.0f) return;

            // nothing to do when the volumetric radius is zero.
            if (shaderLight->volumetricRadius == 0.0f) return;

            // copy volumetric light sources into the post processing system.
            postProcessingShaderVolumetricLights[postProcessingVolumetricLightsCount++] = *shaderLight;
        }

        /// <summary>Executes before a camera starts rendering the post processing shader.</summary>
        internal void PostProcessingOnPreRenderCallback()
        {
            // we require that we are fully initialized.
            if (!isInitialized) return;

            // upload the volumetric light sources to the graphics card.
            if (postProcessingVolumetricLightsCount > 0)
            {
                postProcessingDynamicLightsBuffer = new ComputeBuffer(postProcessingVolumetricLightsCount, dynamicLightStride, ComputeBufferType.Default);
                postProcessingDynamicLightsBuffer.SetData(postProcessingShaderVolumetricLights, 0, 0, postProcessingVolumetricLightsCount);
                ShadersSetGlobalDynamicLights(postProcessingDynamicLightsBuffer);
            }

            // upload shader values to the graphics card.
            ShadersSetGlobalDynamicLightsCount(postProcessingVolumetricLightsCount);
            ShadersSetGlobalRealtimeLightsCount(0);
        }

        /// <summary>Executes after a camera stops rendering the post processing shader.</summary>
        internal void PostProcessingOnPostRenderCallback()
        {
            // we require that we are fully initialized.
            if (!isInitialized) return;

            // delete the volumetric light sources from the graphics card.
            if (postProcessingDynamicLightsBuffer != null)
            {
                ShadersSetGlobalDynamicLights(dynamicLightsBuffer);
                if (postProcessingDynamicLightsBuffer.IsValid())
                {
                    postProcessingDynamicLightsBuffer.Dispose();
                    postProcessingDynamicLightsBuffer = null;
                }
            }

            // restore shader values on the graphics card.
            ShadersSetGlobalDynamicLightsCount(shadersLastDynamicLightsCount);
            ShadersSetGlobalRealtimeLightsCount(shadersLastRealtimeLightsCount);
        }
    }
}