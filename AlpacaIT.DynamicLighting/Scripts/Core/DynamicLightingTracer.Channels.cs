using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    // implements the channel assignment logic for raycastable dynamic light sources. Every light
    // must have a unique channel (indicating bit 0 to 31) to store their shadows on the graphics
    // card. The algorithms ensures that overlapping lights do not get assigned the same channel,
    // but the channel can be re-used when outside of the light's radius.

    internal partial class DynamicLightingTracer
    {
        /// <summary>
        /// Keeps track of the next light channel to be assigned when searching for available channels.
        /// <para>
        /// This variable helps to ensure that half of all available channels (0 to 15) are used
        /// sequentially before any channel is reused. The original issue is that the system would
        /// eagerly recycle the same channel (starting from 0) as soon as it became available, even
        /// if other channels were still unused. This could lead to nearby lights on the same mesh
        /// being assigned the same channel, increasing the risk of light leaking on the lightmap texture.
        /// </para>
        /// <para>
        /// By keeping track of the last assigned channel, <see cref="channelsNextChannel"/> ensures
        /// that the system works through most channels in a cyclic manner (0 to 15) before wrapping
        /// around to 0. This reduces the chances of lights that are spatially close to one another
        /// being assigned the same channel, which helps mitigate potential light leakage issues.
        /// </para>
        /// </summary>
        private uint channelsNextChannel = 0;

        /// <summary>
        /// Assigns light channels to all of the <see cref="DynamicLight"/> instances in the scene.
        /// <para>Requires the <see cref="pointLights"/> array to be initialized.</para>
        /// </summary>
        private void ChannelsUpdatePointLightsInScene()
        {
            // first reset all the channels to an invalid value.
            for (int i = 0; i < pointLights.Length; i++)
            {
                var light = pointLights[i];
                light.lightChannel = 255;
            }

            for (int i = 0; i < pointLights.Length; i++)
            {
                var light = pointLights[i];

                if (ChannelsTryFindUnusedChannelAtWorldPosition(light.transform.position, light.lightRadius, out var channel))
                {
#if UNITY_EDITOR
                    // required to use serialized object to override fields in prefabs:
                    // this does: light.lightChannel = channel;
                    var serializedObject = new UnityEditor.SerializedObject(light);
                    var lightChannelProperty = serializedObject.FindProperty(nameof(DynamicLight.lightChannel));
                    lightChannelProperty.intValue = (int)channel;
                    serializedObject.ApplyModifiedProperties();
#else
                    light.lightChannel = channel;
#endif
                }
                else
                {
                    Debug.LogError("More than 32 lights intersect at the same position! This is not supported! Please spread your light sources further apart or reduce their radius.", light);
                }
            }
        }

        /// <summary>Attempts to find an unused light channel at the specified world position.</summary>
        /// <param name="position">The world position to find an unused light channel at.</param>
        /// <param name="radius">The radius that the light requires.</param>
        /// <param name="channel">The unused light channel number if found.</param>
        /// <returns>True when an unused channel was found else false.</returns>
        private bool ChannelsTryFindUnusedChannelAtWorldPosition(Vector3 position, float radius, out uint channel)
        {
            // find all used channels that intersect our radius.
            var channels = new bool[32];
            for (int i = 0; i < pointLights.Length; i++)
            {
                var light = pointLights[i];
                if (light.lightChannel == 255) continue;

                if (MathEx.SpheresIntersect(light.transform.position, light.lightRadius, position, radius))
                    channels[light.lightChannel] = true;
            }

            // sequentially find a free channel starting from the next channel.
            for (int i = 0; i < channels.Length; i++)
            {
                channel = (channelsNextChannel + (uint)i) % 16;
                if (!channels[channel])
                {
                    // update the next channel variable for the next assignment.
                    channelsNextChannel = (channel + 1) % 16;
                    return true;
                }
            }

            // we ran out of sequential channels so recycle a free channel.
            for (channel = 16; channel < channels.Length; channel++)
                if (!channels[channel])
                    return true;

            channel = 255;
            return false;
        }
    }
}