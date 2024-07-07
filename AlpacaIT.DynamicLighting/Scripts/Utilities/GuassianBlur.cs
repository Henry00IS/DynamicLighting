using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    // contains source code from https://github.com/daniel-ilett/smo-shaders (see Licenses/SmoShaders.txt).
    // shoutouts to Daniel Ilett!

    internal static class GaussianBlur
    {
        public static unsafe void ApplyGaussianBlur(Color* destination, Color[] source, int size, int kernelSize = 21, float spread = 5.0f)
        {
            float[] gaussianWeights = GenerateGaussianWeights(kernelSize, spread);

            // Apply the horizontal blur pass
            HorizontalBlur(source, destination, gaussianWeights, size, size);

            var source_gc = GCHandle.Alloc(source, GCHandleType.Pinned);
            var source_ptr = source_gc.AddrOfPinnedObject();
            Buffer.MemoryCopy(destination, (void*)source_ptr, size * size * sizeof(Color), size * size * sizeof(Color));
            source_gc.Free();

            // Apply the vertical blur pass
            VerticalBlur(source, destination, gaussianWeights, size, size);
        }

        private static float[] GenerateGaussianWeights(int kernelSize, float sigma)
        {
            float[] weights = new float[kernelSize];
            float sum = 0.0f;
            int halfKernel = kernelSize / 2;
            float sigma2 = sigma * sigma;

            for (int i = -halfKernel; i <= halfKernel; i++)
            {
                float weight = (1.0f / Mathf.Sqrt(2.0f * Mathf.PI * sigma2)) * Mathf.Exp(-(i * i) / (2.0f * sigma2));
                weights[i + halfKernel] = weight;
                sum += weight;
            }

            // Normalize the weights
            for (int i = 0; i < kernelSize; i++)
            {
                weights[i] /= sum;
            }

            return weights;
        }

        private static unsafe void HorizontalBlur(Color[] source, Color* destination, float[] weights, int width, int height)
        {
            int halfKernel = weights.Length / 2;
            Color[] sourcePixels = source;
            Color* destinationPixels = destination;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color colorSum = Color.black;
                    float weightSum = 0.0f;

                    for (int k = -halfKernel; k <= halfKernel; k++)
                    {
                        int sampleX = Mathf.Clamp(x + k, 0, width - 1);
                        Color sample = sourcePixels[y * width + sampleX];
                        float weight = weights[k + halfKernel];
                        colorSum += sample * weight;
                        weightSum += weight;
                    }

                    destinationPixels[y * width + x] = colorSum / weightSum;
                }
            }
        }

        private static unsafe void VerticalBlur(Color[] source, Color* destination, float[] weights, int width, int height)
        {
            int halfKernel = weights.Length / 2;
            Color[] sourcePixels = source;
            Color* destinationPixels = destination;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Color colorSum = Color.black;
                    float weightSum = 0.0f;

                    for (int k = -halfKernel; k <= halfKernel; k++)
                    {
                        int sampleY = Mathf.Clamp(y + k, 0, height - 1);
                        Color sample = sourcePixels[sampleY * width + x];
                        float weight = weights[k + halfKernel];
                        colorSum += sample * weight;
                        weightSum += weight;
                    }

                    destinationPixels[y * width + x] = colorSum / weightSum;
                }
            }
        }
    }
}