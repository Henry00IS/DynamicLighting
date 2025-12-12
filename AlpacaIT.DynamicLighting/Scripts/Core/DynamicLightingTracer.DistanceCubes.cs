using System.Collections.Generic;
using Unity.Mathematics;

namespace AlpacaIT.DynamicLighting
{
    // implements distance cube generation.

    internal partial class DynamicLightingTracer
    {
        internal const int distanceCubesResolution = 64;

        private unsafe ulong DistanceCubesGenerate()
        {
            var data = new List<uint>(pointLights.Length * distanceCubesResolution * distanceCubesResolution * 3); // storing two distances per pixel.
            for (int i = 0; i < pointLights.Length; i++)
            {
                var light = pointLights[i];
                var lightCache = pointLightsCache[i];

                // render a small photon cube:
                var photonCube = PhotonCameraRender(lightCache.position, light.lightRadius, false, distanceCubesResolution);

                // write the distances.
                var distanceCubesResolutionSqr = distanceCubesResolution * distanceCubesResolution;
                data.Capacity += distanceCubesResolutionSqr * 3;

                // add 0.25 bias, this avoids a bias addition operation in the shader.
                if (dynamicGeometryLightingMode == DynamicGeometryLightingMode.DistanceCubes)
                {
                    for (int j = 0; j < distanceCubesResolutionSqr; j += 2)
                        data.Add(Pack_f32f32_to_f16f16(photonCube.faces[1].distancesPtr[j].ToFloat(light.lightRadius) + 0.25f, photonCube.faces[1].distancesPtr[j + 1].ToFloat(light.lightRadius) + 0.25f)); // Left
                    for (int j = 0; j < distanceCubesResolutionSqr; j += 2)
                        data.Add(Pack_f32f32_to_f16f16(photonCube.faces[0].distancesPtr[j].ToFloat(light.lightRadius) + 0.25f, photonCube.faces[0].distancesPtr[j + 1].ToFloat(light.lightRadius) + 0.25f)); // Right
                    for (int j = 0; j < distanceCubesResolutionSqr; j += 2)
                        data.Add(Pack_f32f32_to_f16f16(photonCube.faces[3].distancesPtr[j].ToFloat(light.lightRadius) + 0.25f, photonCube.faces[3].distancesPtr[j + 1].ToFloat(light.lightRadius) + 0.25f)); // Down
                    for (int j = 0; j < distanceCubesResolutionSqr; j += 2)
                        data.Add(Pack_f32f32_to_f16f16(photonCube.faces[2].distancesPtr[j].ToFloat(light.lightRadius) + 0.25f, photonCube.faces[2].distancesPtr[j + 1].ToFloat(light.lightRadius) + 0.25f)); // Up
                    for (int j = 0; j < distanceCubesResolutionSqr; j += 2)
                        data.Add(Pack_f32f32_to_f16f16(photonCube.faces[5].distancesPtr[j].ToFloat(light.lightRadius) + 0.25f, photonCube.faces[5].distancesPtr[j + 1].ToFloat(light.lightRadius) + 0.25f)); // Backward
                    for (int j = 0; j < distanceCubesResolutionSqr; j += 2)
                        data.Add(Pack_f32f32_to_f16f16(photonCube.faces[4].distancesPtr[j].ToFloat(light.lightRadius) + 0.25f, photonCube.faces[4].distancesPtr[j + 1].ToFloat(light.lightRadius) + 0.25f)); // Forward
                }
                // angular interpolation wants accuracy.
                else
                {
                    for (int j = 0; j < distanceCubesResolutionSqr; j += 2)
                        data.Add(Pack_f32f32_to_f16f16(photonCube.faces[1].distancesPtr[j].ToFloat(light.lightRadius), photonCube.faces[1].distancesPtr[j + 1].ToFloat(light.lightRadius))); // Left
                    for (int j = 0; j < distanceCubesResolutionSqr; j += 2)
                        data.Add(Pack_f32f32_to_f16f16(photonCube.faces[0].distancesPtr[j].ToFloat(light.lightRadius), photonCube.faces[0].distancesPtr[j + 1].ToFloat(light.lightRadius))); // Right
                    for (int j = 0; j < distanceCubesResolutionSqr; j += 2)
                        data.Add(Pack_f32f32_to_f16f16(photonCube.faces[3].distancesPtr[j].ToFloat(light.lightRadius), photonCube.faces[3].distancesPtr[j + 1].ToFloat(light.lightRadius))); // Down
                    for (int j = 0; j < distanceCubesResolutionSqr; j += 2)
                        data.Add(Pack_f32f32_to_f16f16(photonCube.faces[2].distancesPtr[j].ToFloat(light.lightRadius), photonCube.faces[2].distancesPtr[j + 1].ToFloat(light.lightRadius))); // Up
                    for (int j = 0; j < distanceCubesResolutionSqr; j += 2)
                        data.Add(Pack_f32f32_to_f16f16(photonCube.faces[5].distancesPtr[j].ToFloat(light.lightRadius), photonCube.faces[5].distancesPtr[j + 1].ToFloat(light.lightRadius))); // Backward
                    for (int j = 0; j < distanceCubesResolutionSqr; j += 2)
                        data.Add(Pack_f32f32_to_f16f16(photonCube.faces[4].distancesPtr[j].ToFloat(light.lightRadius), photonCube.faces[4].distancesPtr[j + 1].ToFloat(light.lightRadius))); // Forward
                }

                photonCube.Dispose();
            }
            dynamicLightManager.raycastedScene.dynamicLightsDistanceCubes.Write(data.ToArray());

            return (ulong)data.Count * 4;
        }

        private uint Pack_f32f32_to_f16f16(float lower, float upper)
        {
            return (math.f32tof16(upper) << 16) | math.f32tof16(lower);
        }
    }
}