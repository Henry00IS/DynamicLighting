using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine.Jobs;

namespace AlpacaIT.DynamicLighting
{
    internal partial class DynamicLightingTracer
    {
        /// <summary>Uses the Unity job system to fix seams caused by unvisited pixels.</summary>
        public unsafe class SeamsPaddingStep
        {
            /// <summary>The 1bpp shadow occlusion bitmask for every light source.</summary>
            private uint* pixelsOcclusionPtr;

            /// <summary>The visited flag bitmask for every light source.</summary>
            private bool* pixelsVisitedPtr;

            /// <summary>The texture size required to evenly distribute texels over the mesh.</summary>
            private int meshTextureSize;

            /// <summary>The texture size squared required to evenly distribute texels over the mesh.</summary>
            private uint meshTextureSizeSqr;

            /// <summary>Creates a new instance of <see cref="TriangleUvTo3dStep"/>.</summary>
            /// <param name="pixelsOcclusion">The 1bpp shadow occlusion bitmask for every light source.</param>
            /// <param name="pixelsVisited">The visited flag bitmask for every light source.</param>
            /// <param name="meshTextureSize">The texture size required to evenly distribute texels over the mesh.</param>
            public SeamsPaddingStep(uint* pixelsOcclusion, bool* pixelsVisited, int meshTextureSize)
            {
                this.pixelsOcclusionPtr = pixelsOcclusion;
                this.pixelsVisitedPtr = pixelsVisited;
                this.meshTextureSize = meshTextureSize;
                meshTextureSizeSqr = (uint)(meshTextureSize * meshTextureSize);
            }

            private struct ProcessSeamsPaddingJob : IJobParallelFor
            {
                /// <summary>The 1bpp shadow occlusion bitmask for every light source.</summary>
                [ReadOnly]
                [NativeDisableUnsafePtrRestriction]
                public uint* inPixelsOcclusionPtr;

                /// <summary>The visited flag bitmask for every light source (read only).</summary>
                [ReadOnly]
                [NativeDisableUnsafePtrRestriction]
                public bool* inPixelsVisitedPtr;

                /// <summary>The texture size required to evenly distribute texels over the mesh (read only).</summary>
                [ReadOnly]
                public int inMeshTextureSize;

                /// <summary>The texture size squared required to evenly distribute texels over the mesh (read only).</summary>
                [ReadOnly]
                public uint inMeshTextureSizeSqr;

                public void Execute(int y)
                {
                    int yPtr = y * inMeshTextureSize;

                    for (int x = 0; x < inMeshTextureSize; x++)
                    {
                        int xyPtr = yPtr + x;

                        // if we find an unvisited pixel it will appear as a black seam in the scene.
                        if (inPixelsVisitedPtr[xyPtr]) continue;
                        uint res = 0;

                        // x x x x x
                        // x x x x x
                        // x x C x x
                        // x x x x x
                        // x x x x x

                        // p00 p10 p20 p30 p40
                        // p01 p11 p21 p31 p41
                        // p02 p12 p22 p32 p42
                        // p03 p13 p23 p33 p43
                        // p04 p14 p24 p34 p44

                        // fetch "occlusion" pixels (where l22 is the center).
                        // fetch "visited" pixels (where p22 is the center).

                        GetPixels(x, y - 2, out var l20, out var p20);
                        GetPixels(x, y - 1, out var l21, out var p21);
                        GetPixels(x - 2, y, out var l02, out var p02);
                        GetPixels(x - 1, y, out var l12, out var p12);
                        GetPixels(x + 1, y, out var l32, out var p32);
                        GetPixels(x + 2, y, out var l42, out var p42);
                        GetPixels(x, y + 1, out var l23, out var p23);
                        GetPixels(x, y + 2, out var l24, out var p24);

                        //
                        //     x
                        //   x C x
                        //     x
                        //

                        // left 1x
                        if (p12)
                            res |= l12;
                        // right 1x
                        if (p32)
                            res |= l32;
                        // up 1x
                        if (p21)
                            res |= l21;
                        // down 1x
                        if (p23)
                            res |= l23;

                        //     x
                        //
                        // x   C   x
                        //
                        //     x

                        // left 2x
                        if (!p12 && p02)
                            res |= l02;
                        // right 2x
                        if (!p32 && p42)
                            res |= l42;
                        // up 2x
                        if (!p21 && p20)
                            res |= l20;
                        // down 2x
                        if (!p23 && p24)
                            res |= l24;

                        inPixelsOcclusionPtr[xyPtr] = res;
                    }
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                private void GetPixels(int x, int y, out uint occlusion, out bool visited)
                {
                    int offset = y * inMeshTextureSize + x;
                    if ((uint)offset >= inMeshTextureSizeSqr)
                    {
                        occlusion = 0;
                        visited = false;
                        return;
                    }
                    occlusion = inPixelsOcclusionPtr[offset];
                    visited = inPixelsVisitedPtr[offset];
                }
            }

            public void Execute()
            {
                // prepare a job that will process one bitmask.
                var processSeamsPaddingJob = new ProcessSeamsPaddingJob()
                {
                    inPixelsOcclusionPtr = pixelsOcclusionPtr,
                    inPixelsVisitedPtr = pixelsVisitedPtr,
                    inMeshTextureSize = meshTextureSize,
                    inMeshTextureSizeSqr = meshTextureSizeSqr,
                };

                // schedule it on the unity job system.

                // wait here while processing the job on multiple threads (including the main thread).
                processSeamsPaddingJob.Schedule(meshTextureSize, 32).Complete();
            }
        }
    }
}