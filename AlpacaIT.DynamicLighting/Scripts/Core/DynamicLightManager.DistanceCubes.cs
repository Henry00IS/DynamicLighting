using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    // implements clever lookup table textures for cubemap sampling in compute buffers.

    public partial class DynamicLightManager
    {
        /// <summary>The cubemap texture for a 64x64x6 cubemap index lookup table.</summary>
        private Cubemap distanceCubesIndexLookupTexture;

        /// <summary>Initialization of the DynamicLightManager.DistanceCubes partial class.</summary>
        private void DistanceCubesInitialize()
        {
            if (dynamicGeometryLightingModeInCurrentScene != DynamicGeometryLightingMode.LightingOnly)
            {
                // create a cubemap with a single floating-point channel.
                distanceCubesIndexLookupTexture = new Cubemap(DynamicLightingTracer.distanceCubesResolution, TextureFormat.RFloat, false);
                distanceCubesIndexLookupTexture.filterMode = FilterMode.Point;

                // build the lookup textures.
                DistanceCubesBuildIndex(DynamicLightingTracer.distanceCubesResolution);

                // set the global texture for use in shaders.
                ShadersSetGlobalDynamicLightsDistanceCubesIndexLookup(distanceCubesIndexLookupTexture);
            }
        }

        /// <summary>Cleanup of the DynamicLightManager.DistanceCubes partial class.</summary>
        private void DistanceCubesCleanup()
        {
            if (distanceCubesIndexLookupTexture)
                DestroyImmediate(distanceCubesIndexLookupTexture);
            distanceCubesIndexLookupTexture = null;
        }

        private void DistanceCubesBuildIndex(int size)
        {
            for (int face = 0; face < 6; face++)
            {
                DistanceCubesBuildIndexFace(face, size);
            }

            // apply the changes to the cubemap texture.
            distanceCubesIndexLookupTexture.Apply();
        }

        /// <summary>Builds a single face of the cubemap lookup texture.</summary>
        private void DistanceCubesBuildIndexFace(int faceIndex, int size)
        {
            var sizeSqr = size * size;
            var colors = new Color[sizeSqr];
            int faceOffset = faceIndex * sizeSqr;
            int index = faceOffset;

            // flipped faces with inverted logic:
            if (faceIndex == 0 || faceIndex == 1 || faceIndex == 4 || faceIndex == 5)
            {
                for (int y = 0; y < size; y++)
                {
                    int invertedY = size - y - 1;
                    int invertedYOffset = invertedY * size;
                    for (int x = 0; x < size; x++)
                    {
                        int invertedX = size - x - 1;
                        int xyPtr = invertedYOffset + invertedX;
                        colors[xyPtr].r = index++;
                    }
                }
            }

            // regular faces:
            else
            {
                for (int i = 0; i < sizeSqr; i++)
                    colors[i].r = index++;
            }

            distanceCubesIndexLookupTexture.SetPixels(colors, (CubemapFace)faceIndex, 0);
        }
    }
}