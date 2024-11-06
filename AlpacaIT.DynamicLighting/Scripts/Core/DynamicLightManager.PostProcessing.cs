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

        /// <summary>Stores the original value of the shader property "dynamic_lights_count".</summary>
        private int postProcessingShaderDynamicLightsCount;

        /// <summary>Stores the original value of the shader property "realtime_lights_count".</summary>
        private int postProcessingShaderRealtimeLightsCount;

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
            var lightCurrentVolumetricRadius = light.currentVolumetricRadius;
            if (lightCurrentVolumetricRadius == 0.0f) return;

            // if the volumetric light can not be seen by the camera we can skip it.
            if (!MathEx.CheckSphereIntersectsFrustum(cameraFrustumPlanes, shaderLight->position, lightCurrentVolumetricRadius))
                return;

            // copy volumetric light sources into the post processing system.
            fixed (ShaderDynamicLight* volumetricLight = &postProcessingShaderVolumetricLights[postProcessingVolumetricLightsCount++])
            {
                // copy the shader dynamic light struct.
                *volumetricLight = *shaderLight;

                // recycle the light radius to store the volumetric radius.
                volumetricLight->radiusSqr = light.lightVolumetricRadius;

                // recycle the general purpose floats to store other volumetric data.
                volumetricLight->gpFloat1 = light.lightVolumetricThickness;

                // recycle the channel to store the volumetric type.
                volumetricLight->channel = (uint)light.lightVolumetricType;

                switch (light.lightVolumetricType)
                {
                    // recycle general purpose floats and shimmer scale for the box scale.
                    case DynamicLightVolumetricType.Box:
                        var lightScale = light.cache.transformScale;
                        volumetricLight->gpFloat2 = lightScale.x;
                        volumetricLight->gpFloat3 = lightScale.y;
                        volumetricLight->shimmerScale = lightScale.z;
                        break;

                    // recycle general purpose floats for the cone angle.
                    case DynamicLightVolumetricType.ConeZ:
                        // fixme: this is silly guesswork:
                        var mul1 = Mathf.Lerp(1.0f, 1.3f, light.lightOuterCutoff / 90f);
                        volumetricLight->gpFloat2 = Mathf.Cos((Mathf.PI * 0.5f) + light.lightOuterCutoff * Mathf.Deg2Rad) * mul1;
                        break;

                    case DynamicLightVolumetricType.ConeY:
                        // fixme: this is silly guesswork:
                        var mul2 = Mathf.Lerp(1.0f, 1.3f, light.lightOuterCutoff / 90f);
                        volumetricLight->gpFloat2 = Mathf.Cos((Mathf.PI * 0.5f) + light.lightOuterCutoff * Mathf.Deg2Rad) * mul2;

                        // when using cone_y the up vector will be calculated.
                        volumetricLight->forward = volumetricLight->up;
                        break;
                }
            }
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
            postProcessingShaderDynamicLightsCount = shadersLastDynamicLightsCount;
            postProcessingShaderRealtimeLightsCount = shadersLastRealtimeLightsCount;
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
            ShadersSetGlobalDynamicLightsCount(postProcessingShaderDynamicLightsCount);
            ShadersSetGlobalRealtimeLightsCount(postProcessingShaderRealtimeLightsCount);
        }
    }
}