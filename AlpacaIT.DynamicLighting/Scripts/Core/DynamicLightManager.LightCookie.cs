using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace AlpacaIT.DynamicLighting
{
    // implements spot light cookie textures.

    public partial class DynamicLightManager
    {
        /// <summary>The array of light cookie textures.</summary>
        private RenderTexture lightCookieTextures;

        private int lightCookieTextureIndex;
        private const int lightCookieTextureBudget = 64; // 64 MiB of 1024x1024 R8.

        /// <summary>Lookup table to give the same texture the same light cookie index.</summary>
        private Dictionary<Texture, uint> lightCookieTextureIndices;

        /// <summary>Initialization of the DynamicLightManager.LightCookie partial class.</summary>
        private void LightCookieInitialize()
        {
            lightCookieTextureIndices = new Dictionary<Texture, uint>();

            lightCookieTextures = new RenderTexture(1024, 1024, 0, RenderTextureFormat.RHalf, 0);
            lightCookieTextures.dimension = TextureDimension.Tex2DArray;
            lightCookieTextures.volumeDepth = lightCookieTextureBudget;
            lightCookieTextures.Create();

            Shader.SetGlobalTexture("light_cookies", lightCookieTextures);
        }

        /// <summary>Cleanup of the DynamicLightManager.LightCookie partial class.</summary>
        private void LightCookieCleanup()
        {
            lightCookieTextureIndices.Clear();
            lightCookieTextureIndices = null;

            lightCookieTextures.Release();
            lightCookieTextures = null;
        }

        /// <summary>Called before the lights are processed for rendering.</summary>
        private void LightCookieUpdate()
        {
            lightCookieTextureIndices.Clear();
            lightCookieTextureIndex = 0;
        }

        private unsafe void LightCookieProcessLight(ShaderDynamicLight* shaderLight, DynamicLight light)
        {
            // the shader light must still be active.
            if (shaderLight->radiusSqr == -1.0f) return;

            // the light must be a spot light.
            if (light.lightType != DynamicLightType.Spot) return;

            // the light cookie texture must be set.
            if (shaderLight->cookieIndex != uint.MaxValue) return;

            // the cookie can not be rendered above 90° or at 0°.
            if (shaderLight->gpFloat2 < 0.0f || shaderLight->gpFloat2 == 1.0f) return;

            // assign the same index to the same cookie texture this frame.
            var texture = light.lightCookieTexture;
            if (lightCookieTextureIndices.TryGetValue(texture, out uint index))
            {
                shaderLight->channel |= (uint)1 << 16; // cookie available bit
                shaderLight->cookieIndex = index;
            }
            else
            {
                // we ran out of light cookies.
                if (lightCookieTextureIndex >= lightCookieTextureBudget)
                    return;

                // if the light can not be seen by the camera we do not reserve a slot for this cookie.
                if (!MathEx.CheckSphereIntersectsFrustum(cameraFrustumPlanes, shaderLight->position, light.lightRadius))
                    return;

                // blit the cookie texture every frame so that we can support render textures.
                Graphics.Blit(texture, lightCookieTextures, 0, lightCookieTextureIndex);

                shaderLight->channel |= (uint)1 << 16; // cookie available bit
                shaderLight->cookieIndex = (uint)lightCookieTextureIndex++;

                // store the texture handle and cookie index.
                lightCookieTextureIndices.Add(texture, shaderLight->cookieIndex);
            }
        }
    }
}