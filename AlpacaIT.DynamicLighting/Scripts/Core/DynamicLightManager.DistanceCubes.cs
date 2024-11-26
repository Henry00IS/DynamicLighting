using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    // implements clever lookup table textures for cubemap sampling in compute buffers.

    public partial class DynamicLightManager
    {
        /// <summary>The cubemap texture for a 32x32x6 cubemap lookup table.</summary>
        private Cubemap distanceCubesLookupTexture32;

        /// <summary>Initialization of the DynamicLightManager.DistanceCubes partial class.</summary>
        private void DistanceCubesInitialize()
        {
            int size = 32;

            // create a cubemap with a single floating-point channel.
            distanceCubesLookupTexture32 = new Cubemap(size, TextureFormat.RFloat, false);
            distanceCubesLookupTexture32.filterMode = FilterMode.Point;

            // build the lookup texture.
            DistanceCubesBuild(size);

            // set the global texture for use in shaders.
            ShadersSetGlobalDynamicLightsDistanceCubesLookup32(distanceCubesLookupTexture32);
        }

        /// <summary>Cleanup of the DynamicLightManager.DistanceCubes partial class.</summary>
        private void DistanceCubesCleanup()
        {
            DestroyImmediate(distanceCubesLookupTexture32);
            distanceCubesLookupTexture32 = null;
        }

        private void DistanceCubesBuild(int size)
        {
            for (int face = 0; face < 6; face++)
            {
                DistanceCubesBuildFace(face, size);
            }

            // apply the changes to the cubemap texture.
            distanceCubesLookupTexture32.Apply();
        }

        /// <summary>Builds a single face of the cubemap lookup texture.</summary>
        private void DistanceCubesBuildFace(int faceIndex, int size)
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

            distanceCubesLookupTexture32.SetPixels(colors, (CubemapFace)faceIndex, 0);
        }
    }
}