using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;

namespace AlpacaIT.DynamicLighting
{
    internal partial class DynamicLightingTracer
    {
        /// <summary>
        /// Uses the Unity job system to to calculate the world position for every UV position on a triangle.
        /// </summary>
        public class TriangleUvTo3dStep
        {
            /// <summary>The first world-space vertex of the triangle.</summary>
            private Vector3 vertex1;

            /// <summary>The second world-space vertex of the triangle.</summary>
            private Vector3 vertex2;

            /// <summary>The third world-space vertex of the triangle.</summary>
            private Vector3 vertex3;

            /// <summary>The uv1 coordinate of the first vertex of the triangle.</summary>
            private Vector2 uv1;

            /// <summary>The uv1 coordinate of the second vertex of the triangle.</summary>
            private Vector2 uv2;

            /// <summary>The uv1 coordinate of the third vertex of the triangle.</summary>
            private Vector2 uv3;

            /// <summary>The triangle surface area result of <see cref="MathEx.UvTo3dFastPrerequisite"/>.</summary>
            private float triangleSurfaceArea;

            /// <summary>The bounding box encompassing the triangle in UV1-pixel coordinates.</summary>
            private PixelTriangleRect pixelTriangleRect;

            /// <summary>The texture size required to evenly distribute texels over the mesh.</summary>
            private float meshTextureSize;

            /// <summary>The world-space coordinates for all pixels in <see cref="pixelTriangleRect"/>.</summary>
            public NativeArray<Vector3> worldPositions;

            /// <summary>Creates a new instance of <see cref="TriangleUvTo3dStep"/>.</summary>
            /// <param name="vertex1">The first world-space vertex of the triangle.</param>
            /// <param name="vertex2">The second world-space vertex of the triangle.</param>
            /// <param name="vertex3">The third world-space vertex of the triangle.</param>
            /// <param name="uv1">The uv1 coordinate of the first vertex of the triangle.</param>
            /// <param name="uv2">The uv1 coordinate of the second vertex of the triangle.</param>
            /// <param name="uv3">The uv1 coordinate of the third vertex of the triangle.</param>
            /// <param name="triangleSurfaceArea">The triangle surface area result of <see cref="MathEx.UvTo3dFastPrerequisite"/>.</param>
            /// <param name="pixelTriangleRect">The bounding box encompassing the triangle in UV1-pixel coordinates.</param>
            /// <param name="meshTextureSize">The texture size required to evenly distribute texels over the mesh.</param>
            public TriangleUvTo3dStep(Vector3 vertex1, Vector3 vertex2, Vector3 vertex3, Vector2 uv1, Vector2 uv2, Vector2 uv3, float triangleSurfaceArea, PixelTriangleRect pixelTriangleRect, float meshTextureSize)
            {
                this.vertex1 = vertex1;
                this.vertex2 = vertex2;
                this.vertex3 = vertex3;
                this.uv1 = uv1;
                this.uv2 = uv2;
                this.uv3 = uv3;
                this.triangleSurfaceArea = triangleSurfaceArea;
                this.pixelTriangleRect = pixelTriangleRect;
                this.meshTextureSize = meshTextureSize;
            }

            private struct ProcessUvTo3dJob : IJobParallelFor
            {
                /// <summary>The first world-space vertex of the triangle (read only).</summary>
                [ReadOnly]
                public Vector3 inVertex1;

                /// <summary>The second world-space vertex of the triangle (read only).</summary>
                [ReadOnly]
                public Vector3 inVertex2;

                /// <summary>The third world-space vertex of the triangle (read only).</summary>
                [ReadOnly]
                public Vector3 inVertex3;

                /// <summary>The uv1 coordinate of the first vertex of the triangle (read only).</summary>
                [ReadOnly]
                public Vector2 inUv1;

                /// <summary>The uv1 coordinate of the second vertex of the triangle (read only).</summary>
                [ReadOnly]
                public Vector2 inUv2;

                /// <summary>The uv1 coordinate of the third vertex of the triangle (read only).</summary>
                [ReadOnly]
                public Vector2 inUv3;

                /// <summary>The triangle surface area result of <see cref="MathEx.UvTo3dFastPrerequisite"/> (read only).</summary>
                [ReadOnly]
                public float inTriangleSurfaceArea;

                /// <summary>The bounding box encompassing the triangle in UV1-pixel coordinates (read only).</summary>
                [ReadOnly]
                public PixelTriangleRect inPixelTriangleRect;

                /// <summary>The texture size required to evenly distribute texels over the mesh (read only).</summary>
                [ReadOnly]
                public float inMeshTextureSize;

                /// <summary>The size of half a pixel on the mesh texture to center the UV coordinates (read only).</summary>
                [ReadOnly]
                public float inHalf;

                /// <summary>The world-space coordinates for all pixels in <see cref="inPixelTriangleRect"/>.</summary>
                public NativeArray<Vector3> outWorldPositions;

                public void Execute(int i)
                {
                    // we must turn the one-dimensional index into two-dimensional coordinates:
                    var width = inPixelTriangleRect.width + 1;
                    int x = i % width;
                    int y = i / width;

                    // these coordinates must be offset by the rectangle min positions.
                    x += inPixelTriangleRect.xMin;
                    y += inPixelTriangleRect.yMin;

                    // convert the pixel coordinates into uv coordinates.
                    float xx = x / inMeshTextureSize;
                    float yy = y / inMeshTextureSize;

                    // converts the uv-space coordinate to world space.
                    outWorldPositions[i] = MathEx.UvTo3dFast(inTriangleSurfaceArea, new Vector2(xx + inHalf, yy + inHalf), inVertex1, inVertex2, inVertex3, inUv1, inUv2, inUv3);
                }
            }

            public void Execute()
            {
                var texelCount = (pixelTriangleRect.width + 1) * (pixelTriangleRect.height + 1);

                // prepare native memory to store the results.
                worldPositions = new NativeArray<Vector3>(texelCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

                // prepare a job that will process all uv coordinates in parallel.
                var processUvTo3dJob = new ProcessUvTo3dJob()
                {
                    inVertex1 = vertex1,
                    inVertex2 = vertex2,
                    inVertex3 = vertex3,
                    inUv1 = uv1,
                    inUv2 = uv2,
                    inUv3 = uv3,
                    inTriangleSurfaceArea = triangleSurfaceArea,
                    inPixelTriangleRect = pixelTriangleRect,
                    inMeshTextureSize = meshTextureSize,
                    inHalf = 1.0f / (meshTextureSize * 2f),
                    outWorldPositions = worldPositions,
                };

                // wait here while processing the job on multiple threads (including the main thread).
                processUvTo3dJob.Schedule(texelCount, 64).Complete();
            }

            public void Dispose()
            {
                worldPositions.Dispose();
            }
        }
    }
}