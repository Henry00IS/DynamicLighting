using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;

namespace AlpacaIT.DynamicLighting
{
    internal partial class DynamicLightingTracer
    {
        /// <summary>
        /// Uses the Unity job system to calculate the world position for every UV position on a triangle.
        /// </summary>
        public unsafe class TriangleUvToFull3dStep
        {
            /// <summary>The first world-space vertex of the triangle.</summary>
            private Vector3 vertex1;

            /// <summary>The second world-space vertex of the triangle.</summary>
            private Vector3 vertex2;

            /// <summary>The third world-space vertex of the triangle.</summary>
            private Vector3 vertex3;

            /// <summary>The first world-space normal of the triangle.</summary>
            private Vector3 normal1;

            /// <summary>The second world-space normal of the triangle.</summary>
            private Vector3 normal2;

            /// <summary>The third world-space normal of the triangle.</summary>
            private Vector3 normal3;

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

            /// <summary>The world-space positions for all pixels in <see cref="pixelTriangleRect"/>.</summary>
            private NativeArray<Vector3> worldPositions;

            /// <summary>The world-space normals for all pixels in <see cref="pixelTriangleRect"/>.</summary>
            private NativeArray<Vector3> worldNormals;

            /// <summary>The world-space positions for all pixels in <see cref="pixelTriangleRect"/>.</summary>
            public Vector3* worldPositionsPtr; // native pointer to disable bounds checking.

            /// <summary>The world-space normals for all pixels in <see cref="pixelTriangleRect"/>.</summary>
            public Vector3* worldNormalsPtr; // native pointer to disable bounds checking.

            /// <summary>Creates a new instance of <see cref="TriangleUvToFull3dStep"/>.</summary>
            /// <param name="vertex1">The first world-space vertex of the triangle.</param>
            /// <param name="vertex2">The second world-space vertex of the triangle.</param>
            /// <param name="vertex3">The third world-space vertex of the triangle.</param>
            /// <param name="normal1">The first world-space normal of the triangle.</param>
            /// <param name="normal2">The second world-space normal of the triangle.</param>
            /// <param name="normal3">The third world-space normal of the triangle.</param>
            /// <param name="uv1">The uv1 coordinate of the first vertex of the triangle.</param>
            /// <param name="uv2">The uv1 coordinate of the second vertex of the triangle.</param>
            /// <param name="uv3">The uv1 coordinate of the third vertex of the triangle.</param>
            /// <param name="triangleSurfaceArea">The triangle surface area result of <see cref="MathEx.UvTo3dFastPrerequisite"/>.</param>
            /// <param name="pixelTriangleRect">The bounding box encompassing the triangle in UV1-pixel coordinates.</param>
            /// <param name="meshTextureSize">The texture size required to evenly distribute texels over the mesh.</param>
            public TriangleUvToFull3dStep(Vector3 vertex1, Vector3 vertex2, Vector3 vertex3, Vector3 normal1, Vector3 normal2, Vector3 normal3, Vector2 uv1, Vector2 uv2, Vector2 uv3, float triangleSurfaceArea, PixelTriangleRect pixelTriangleRect, float meshTextureSize)
            {
                this.vertex1 = vertex1;
                this.vertex2 = vertex2;
                this.vertex3 = vertex3;
                this.normal1 = normal1;
                this.normal2 = normal2;
                this.normal3 = normal3;
                this.uv1 = uv1;
                this.uv2 = uv2;
                this.uv3 = uv3;
                this.triangleSurfaceArea = triangleSurfaceArea;
                this.pixelTriangleRect = pixelTriangleRect;
                this.meshTextureSize = meshTextureSize;
            }

            private struct ProcessUvToFull3dJob : IJobParallelFor
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

                /// <summary>The first world-space normal of the triangle (read only).</summary>
                [ReadOnly]
                public Vector3 inNormal1;

                /// <summary>The second world-space normal of the triangle (read only).</summary>
                [ReadOnly]
                public Vector3 inNormal2;

                /// <summary>The third world-space normal of the triangle (read only).</summary>
                [ReadOnly]
                public Vector3 inNormal3;

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

                /// <summary>The world-space positions for all pixels in <see cref="inPixelTriangleRect"/>.</summary>
                [NativeDisableUnsafePtrRestriction]
                public Vector3* outWorldPositionsPtr; // native pointer to disable bounds checking.

                /// <summary>The world-space normals for all pixels in <see cref="inPixelTriangleRect"/>.</summary>
                [NativeDisableUnsafePtrRestriction]
                public Vector3* outWorldNormalsPtr; // native pointer to disable bounds checking.

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
                    // [unsafe] new Vector2(xx + inHalf, yy + inHalf)
                    Vector2 uv; _ = &uv;
                    uv.x = xx + inHalf;
                    uv.y = yy + inHalf;

                    // todo: can optimize the calculations as a lot is identical between these two calls.
                    outWorldPositionsPtr[i] = MathEx.UvTo3dFast(inTriangleSurfaceArea, uv, inVertex1, inVertex2, inVertex3, inUv1, inUv2, inUv3);
                    outWorldNormalsPtr[i] = MathEx.UvTo3dFast(inTriangleSurfaceArea, uv, inNormal1, inNormal2, inNormal3, inUv1, inUv2, inUv3);
                }
            }

            public void Execute()
            {
                var texelCount = (pixelTriangleRect.width + 1) * (pixelTriangleRect.height + 1);

                // prepare native memory to store the results.
                worldPositions = new NativeArray<Vector3>(texelCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                worldPositionsPtr = (Vector3*)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(worldPositions);

                worldNormals = new NativeArray<Vector3>(texelCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                worldNormalsPtr = (Vector3*)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(worldNormals);

                // prepare a job that will process all uv coordinates in parallel.
                var processUvToFull3dJob = new ProcessUvToFull3dJob()
                {
                    inVertex1 = vertex1,
                    inVertex2 = vertex2,
                    inVertex3 = vertex3,
                    inNormal1 = normal1,
                    inNormal2 = normal2,
                    inNormal3 = normal3,
                    inUv1 = uv1,
                    inUv2 = uv2,
                    inUv3 = uv3,
                    inTriangleSurfaceArea = triangleSurfaceArea,
                    inPixelTriangleRect = pixelTriangleRect,
                    inMeshTextureSize = meshTextureSize,
                    inHalf = 1.0f / (meshTextureSize * 2f),
                    outWorldPositionsPtr = worldPositionsPtr,
                    outWorldNormalsPtr = worldNormalsPtr,
                };

                // wait here while processing the job on multiple threads (including the main thread).
                processUvToFull3dJob.Schedule(texelCount, 64).Complete();
            }

            public void Dispose()
            {
                if (worldPositions.IsCreated)
                    worldPositions.Dispose();

                if (worldNormals.IsCreated)
                    worldNormals.Dispose();
            }
        }
    }
}