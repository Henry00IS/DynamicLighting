using System.Collections.Generic;

namespace AlpacaIT.DynamicLighting
{
    // implements distance cube generation.

    internal partial class DynamicLightingTracer
    {
        private const int distanceCubesResolution = 32;

        private unsafe ulong DistanceCubesGenerate()
        {
            List<uint> data = new List<uint>();

            const int headerSize = 4;

            // create a header for every raycasted light.
            for (int i = 0; i < pointLights.Length; i++)
            {
                var light = pointLights[i];
                float lightRadius = light.lightRadius;

                // +----------------+
                // |Cube Data Offset|
                // +----------------+
                // |Cube Dimensions |
                // +----------------+
                // |Cube Compression|
                // +----------------+
                // |Cube Distance   |
                // +----------------+

                // the data offset (will be filled out later).
                data.Add(0);
                data.Add(distanceCubesResolution);
                data.Add(0);
                data.Add(*(uint*)&lightRadius);
            }

            uint offset = (uint)data.Count;
            for (int i = 0; i < pointLights.Length; i++)
            {
                var light = pointLights[i];
                var lightCache = pointLightsCache[i];

                // render a small photon cube:
                var headerOffset = i * headerSize;
                var photonCube = PhotonCameraRender(lightCache.position, light.lightRadius, false, distanceCubesResolution);

                // write the distances:
                for (int j = 0; j < distanceCubesResolution * distanceCubesResolution; j++)
                    data.Add(photonCube.faces[1].distances[j].ToFloatAsUInt32(light.lightRadius)); // L OK
                for (int j = 0; j < distanceCubesResolution * distanceCubesResolution; j++)
                    data.Add(photonCube.faces[0].distances[j].ToFloatAsUInt32(light.lightRadius)); // R OK
                for (int j = 0; j < distanceCubesResolution * distanceCubesResolution; j++)
                    data.Add(photonCube.faces[3].distances[j].ToFloatAsUInt32(light.lightRadius)); // Floor
                for (int j = 0; j < distanceCubesResolution * distanceCubesResolution; j++)
                    data.Add(photonCube.faces[2].distances[j].ToFloatAsUInt32(light.lightRadius)); // Ceiling
                for (int j = 0; j < distanceCubesResolution * distanceCubesResolution; j++)
                    data.Add(photonCube.faces[5].distances[j].ToFloatAsUInt32(light.lightRadius)); // Back
                for (int j = 0; j < distanceCubesResolution * distanceCubesResolution; j++)
                    data.Add(photonCube.faces[4].distances[j].ToFloatAsUInt32(light.lightRadius)); // Works on my machine OK

                photonCube.Dispose();

                data[headerOffset] = offset;
                offset = (uint)data.Count;
            }
            dynamicLightManager.raycastedScene.dynamicLightsDistanceCubes.Write(data.ToArray());

            return (ulong)data.Count * 4;
        }
    }
}