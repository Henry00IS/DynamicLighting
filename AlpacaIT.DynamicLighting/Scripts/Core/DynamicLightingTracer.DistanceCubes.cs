using System.Collections.Generic;

namespace AlpacaIT.DynamicLighting
{
    // implements distance cube generation.

    internal partial class DynamicLightingTracer
    {
        private const int distanceCubesResolution = 32;

        private unsafe ulong DistanceCubesGenerate()
        {
            var data = new List<uint>(pointLights.Length * distanceCubesResolution * distanceCubesResolution * 6);
            for (int i = 0; i < pointLights.Length; i++)
            {
                var light = pointLights[i];
                var lightCache = pointLightsCache[i];

                // render a small photon cube:
                var photonCube = PhotonCameraRender(lightCache.position, light.lightRadius, false, distanceCubesResolution);

                // write the distances:
                var distanceCubesResolutionSqr = distanceCubesResolution * distanceCubesResolution;
                data.Capacity += distanceCubesResolutionSqr * 6;
                for (int j = 0; j < distanceCubesResolutionSqr; j++)
                    data.Add(photonCube.faces[1].distancesPtr[j].ToFloat(light.lightRadius).AsUInt32()); // Left
                for (int j = 0; j < distanceCubesResolutionSqr; j++)
                    data.Add(photonCube.faces[0].distancesPtr[j].ToFloat(light.lightRadius).AsUInt32()); // Right
                for (int j = 0; j < distanceCubesResolutionSqr; j++)
                    data.Add(photonCube.faces[3].distancesPtr[j].ToFloat(light.lightRadius).AsUInt32()); // Down
                for (int j = 0; j < distanceCubesResolutionSqr; j++)
                    data.Add(photonCube.faces[2].distancesPtr[j].ToFloat(light.lightRadius).AsUInt32()); // Up
                for (int j = 0; j < distanceCubesResolutionSqr; j++)
                    data.Add(photonCube.faces[5].distancesPtr[j].ToFloat(light.lightRadius).AsUInt32()); // Backward
                for (int j = 0; j < distanceCubesResolutionSqr; j++)
                    data.Add(photonCube.faces[4].distancesPtr[j].ToFloat(light.lightRadius).AsUInt32()); // Forward

                photonCube.Dispose();
            }
            dynamicLightManager.raycastedScene.dynamicLightsDistanceCubes.Write(data.ToArray());

            return (ulong)data.Count * 4;
        }
    }
}