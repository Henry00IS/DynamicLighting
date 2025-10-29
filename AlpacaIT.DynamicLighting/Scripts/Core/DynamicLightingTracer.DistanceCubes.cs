using System.Collections.Generic;

namespace AlpacaIT.DynamicLighting
{
    // implements distance cube generation.

    internal partial class DynamicLightingTracer
    {
        internal const int distanceCubesResolution = 64;

        private unsafe ulong DistanceCubesGenerate()
        {
            var data = new List<uint>(pointLights.Length * distanceCubesResolution * distanceCubesResolution * 3);
            for (int i = 0; i < pointLights.Length; i++)
            {
                var light = pointLights[i];
                var lightCache = pointLightsCache[i];

                // render a small photon cube:
                var photonCube = PhotonCameraRender(lightCache.position, light.lightRadius, false, distanceCubesResolution);

                // write the distances:
                var distanceCubesResolutionSqr = distanceCubesResolution * distanceCubesResolution;
                data.Capacity += distanceCubesResolutionSqr * 3;
                for (int j = 0; j < distanceCubesResolutionSqr; j += 2)
                    data.Add(ScaledAbsFloat16.Pack(photonCube.faces[1].distancesPtr[j + 1], photonCube.faces[1].distancesPtr[j])); // Left
                for (int j = 0; j < distanceCubesResolutionSqr; j += 2)
                    data.Add(ScaledAbsFloat16.Pack(photonCube.faces[0].distancesPtr[j + 1], photonCube.faces[0].distancesPtr[j])); // Right
                for (int j = 0; j < distanceCubesResolutionSqr; j += 2)
                    data.Add(ScaledAbsFloat16.Pack(photonCube.faces[3].distancesPtr[j + 1], photonCube.faces[3].distancesPtr[j])); // Down
                for (int j = 0; j < distanceCubesResolutionSqr; j += 2)
                    data.Add(ScaledAbsFloat16.Pack(photonCube.faces[2].distancesPtr[j + 1], photonCube.faces[2].distancesPtr[j])); // Up
                for (int j = 0; j < distanceCubesResolutionSqr; j += 2)
                    data.Add(ScaledAbsFloat16.Pack(photonCube.faces[5].distancesPtr[j + 1], photonCube.faces[5].distancesPtr[j])); // Backward
                for (int j = 0; j < distanceCubesResolutionSqr; j += 2)
                    data.Add(ScaledAbsFloat16.Pack(photonCube.faces[4].distancesPtr[j + 1], photonCube.faces[4].distancesPtr[j])); // Forward

                photonCube.Dispose();
            }
            dynamicLightManager.raycastedScene.dynamicLightsDistanceCubes.Write(data.ToArray());

            return (ulong)data.Count * 4;
        }
    }
}