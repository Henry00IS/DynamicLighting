using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;

namespace AlpacaIT.DynamicLighting
{
    internal partial class DynamicLightingTracer
    {
        /// <summary>This step processes a <see cref="Mesh"/> for fast data access and raycasting in the scene.</summary>
        public class ProcessMeshDataStep : IStep
        {
            /// <summary>The transformation matrix to transform local vertex coordinates to world-space.</summary>
            private readonly Matrix4x4 localToWorldMatrix;

            /// <summary>The mesh to be processed.</summary>
            private readonly Mesh mesh;

            /// <summary>The pixel density per square meter (see <see cref="DynamicLightManager.pixelDensityPerSquareMeter"/>).</summary>
            private readonly int pixelDensityPerSquareMeter;

            /// <summary>The maximum texture size allowed for raytracing.</summary>
            private readonly int maximumTextureSize;

            /// <summary>The texture size required to evenly distribute texels over this mesh (read only).</summary>
            public int textureSize;

            /// <summary>The surface area of the mesh in meters squared (read only).</summary>
            public float surfaceArea;

            /// <summary>Gets whether the mesh has lightmap coordinates (read only).</summary>
            public bool hasLightmapCoordinates;

            /// <summary>Gets the triangle count of the mesh i.e. <see cref="meshTriangles"/> / 3 (read only).</summary>
            public int triangleCount;

            /// <summary>The original unmodified triangle indices of the mesh (read only).</summary>
            public NativeArray<int> meshTriangles;

            /// <summary>The original unmodified UV1 coordinates of the mesh (read only).</summary>
            public NativeArray<Vector2> meshUv1;

            /// <summary>The vertices of the mesh in world-space coordinates (read only).</summary>
            public NativeArray<Vector3> worldVertices;

            /// <summary>
            /// The flat triangle normals each calculated using a plane of 3 vertices (i.e. not the
            /// normals stored inside of the mesh data). Degenerate triangles have have a normal of
            /// <see cref="Vector3.zero"/> (read only).
            /// </summary>
            public NativeArray<Vector3> triangleNormals;

            /// <summary>
            /// The bounding boxes encompassing the triangles in UV1 coordinates. The flag <see
            /// cref="hasLightmapCoordinates"/> must be true for this to be set (read only).
            /// </summary>
            public NativeArray<PixelTriangleRect> triangleUv1BoundingBoxes;

            /// <summary>Creates a new instance of <see cref="ProcessMeshDataStep"/>.</summary>
            /// <param name="localToWorldMatrix">
            /// The transformation matrix to transform local vertex coordinates to world-space.
            /// </param>
            /// <param name="mesh">The mesh to be processed.</param>
            /// <param name="pixelDensityPerSquareMeter">
            /// The pixel density per square meter (see <see cref="DynamicLightManager.pixelDensityPerSquareMeter"/>).
            /// </param>
            /// <param name="maximumTextureSize">The maximum texture size allowed for raytracing.</param>
            public ProcessMeshDataStep(Matrix4x4 localToWorldMatrix, Mesh mesh, int pixelDensityPerSquareMeter, int maximumTextureSize)
            {
                this.localToWorldMatrix = localToWorldMatrix;
                this.mesh = mesh;
                this.pixelDensityPerSquareMeter = pixelDensityPerSquareMeter;
                this.maximumTextureSize = maximumTextureSize;
            }

            private struct ProcessVerticesToWorldJob : IJobParallelFor
            {
                /// <summary>The transformation matrix to transform local vertex coordinates to world-space (read only).</summary>
                [ReadOnly]
                public Matrix4x4 inLocalToWorldMatrix;

                /// <summary>The original mesh vertices (read only).</summary>
                [ReadOnly]
                public NativeArray<Vector3> inMeshVertices;

                /// <summary>The vertices of the mesh in world-space coordinates.</summary>
                public NativeArray<Vector3> outWorldVertices;

                public void Execute(int i)
                {
                    // convert the vertices to world positions.
                    outWorldVertices[i] = inLocalToWorldMatrix.MultiplyPoint(inMeshVertices[i]);
                }
            }

            private struct ProcessTriangleNormalsJob : IJobParallelFor
            {
                /// <summary>The vertices of the mesh in world-space coordinates (read only).</summary>
                [ReadOnly]
                public NativeArray<Vector3> inWorldVertices;

                /// <summary>The original mesh triangles (read only).</summary>
                [ReadOnly]
                public NativeArray<int> inMeshTriangles;

                /// <summary>
                /// The flat triangle normals each calculated using a plane of 3 vertices (i.e. not
                /// the normals stored inside of the mesh data). Degenerate triangles have have a
                /// normal of <see cref="Vector3.zero"/>.
                /// </summary>
                public NativeArray<Vector3> outTriangleNormals;

                public void Execute(int i)
                {
                    // every triangle uses 3 vertices.
                    var triangleIndex = i * 3;

                    var vertex1 = inMeshTriangles[triangleIndex];
                    var vertex2 = inMeshTriangles[triangleIndex + 1];
                    var vertex3 = inMeshTriangles[triangleIndex + 2];

                    // we use the world-space mesh vertices due to rotation in the scene.
                    var world1 = inWorldVertices[vertex1];
                    var world2 = inWorldVertices[vertex2];
                    var world3 = inWorldVertices[vertex3];

                    // calculate the triangle normals (identical equation to using a plane).
                    outTriangleNormals[i] = Vector3.Normalize(Vector3.Cross(world2 - world1, world3 - world1));
                }
            }

            private struct ProcessTriangleUv1BoundingBoxesJob : IJobParallelFor
            {
                /// <summary>The texture size required to evenly distribute texels over this mesh (read only).</summary>
                [ReadOnly]
                public int inTextureSize;

                /// <summary>The original mesh triangles (read only).</summary>
                [ReadOnly]
                public NativeArray<int> inMeshTriangles;

                /// <summary>The original mesh uv1 coordinates (read only).</summary>
                [ReadOnly]
                public NativeArray<Vector2> inMeshUv1;

                /// <summary>The bounding boxes encompassing the triangles in UV1-pixel coordinates.</summary>
                public NativeArray<PixelTriangleRect> outTriangleUv1BoundingBoxes;

                public void Execute(int i)
                {
                    // every triangle uses 3 vertices.
                    var triangleIndex = i * 3;

                    var vertex1 = inMeshTriangles[triangleIndex];
                    var vertex2 = inMeshTriangles[triangleIndex + 1];
                    var vertex3 = inMeshTriangles[triangleIndex + 2];

                    // calculate the texture size needed for the uv1 bounds of the triangle.
                    var lm1 = inMeshUv1[vertex1];
                    var lm2 = inMeshUv1[vertex2];
                    var lm3 = inMeshUv1[vertex3];
                    outTriangleUv1BoundingBoxes[i] = new PixelTriangleRect(inTextureSize, MathEx.ComputeTriangleBoundingBox(lm1, lm2, lm3));
                }
            }

            public void Execute()
            {
                // todo: zombie, please make us use the native mesh data directly.

                // read the original mesh data into memory.
                var managedMeshVertices = mesh.vertices;
                var managedMeshTriangles = mesh.triangles;
                var managedMeshUv1 = mesh.uv2;

                // load the original mesh data into native memory.
                var nativeVertices = new NativeArray<Vector3>(managedMeshVertices, Allocator.TempJob);
                meshTriangles = new NativeArray<int>(managedMeshTriangles, Allocator.TempJob);
                meshUv1 = new NativeArray<Vector2>(managedMeshUv1, Allocator.TempJob);

                // calculate the triangle count.
                triangleCount = managedMeshTriangles.Length / 3;

                // [waiting job] convert mesh vertices to world space.
                {
                    // prepare native memory to store the results.
                    worldVertices = new NativeArray<Vector3>(managedMeshVertices.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

                    // prepare a job that will process all vertices in parallel.
                    var processVerticesJob = new ProcessVerticesToWorldJob()
                    {
                        inLocalToWorldMatrix = localToWorldMatrix,
                        inMeshVertices = nativeVertices,
                        outWorldVertices = worldVertices,
                    };

                    // wait here while processing the job on multiple threads (including the main thread).
                    processVerticesJob.Schedule(managedMeshVertices.Length, 64).Complete();
                }

                // [job] compute mesh normals from the world-space vertices.
                JobHandle processTrianglesJobHandle;
                {
                    // prepare native memory to store the results.
                    triangleNormals = new NativeArray<Vector3>(triangleCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

                    // prepare a job that will process all triangles in parallel.
                    var processTrianglesJob = new ProcessTriangleNormalsJob()
                    {
                        inWorldVertices = worldVertices,
                        inMeshTriangles = meshTriangles,
                        outTriangleNormals = triangleNormals,
                    };

                    // begin processing the job on multiple threads.
                    processTrianglesJobHandle = processTrianglesJob.Schedule(triangleNormals.Length, 64);
                }

                // begin working on the job above while we prepare the next job.
                JobHandle.ScheduleBatchedJobs(); // main thread continues:

                // [main thread task] calculate the surface area of the mesh.
                surfaceArea = 0;
                for (int i = 0; i < managedMeshTriangles.Length; i += 3)
                {
                    var vertex1 = managedMeshTriangles[i];
                    var vertex2 = managedMeshTriangles[i + 1];
                    var vertex3 = managedMeshTriangles[i + 2];
                    // calculate the surface area of the mesh by adding the surface area of every triangle.
                    surfaceArea += MathEx.CalculateSurfaceAreaOfTriangle(worldVertices[vertex1], worldVertices[vertex2], worldVertices[vertex3]);
                }

                // [main thread task] calculate the texture size required for this mesh.
                textureSize = MathEx.SurfaceAreaToTextureSize(surfaceArea, pixelDensityPerSquareMeter);
                if (textureSize > maximumTextureSize)
                    textureSize = maximumTextureSize;
                // ensure there is at least one pixel (when taken -1 for array index).
                if (textureSize <= 1)
                    textureSize = 2;

                // [main thread task] check whether the mesh has lightmap coordinates.
                hasLightmapCoordinates = managedMeshUv1.Length > 0;

                if (hasLightmapCoordinates)
                {
                    // [job] compute uv1 triangle bounding boxes.
                    JobHandle processTriangleUv1BoundingBoxesJobHandle;
                    {
                        // prepare native memory to store the results.
                        triangleUv1BoundingBoxes = new NativeArray<PixelTriangleRect>(triangleCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

                        // prepare a job that will process all triangles in parallel.
                        var processTriangleUv1BoundingBoxesJob = new ProcessTriangleUv1BoundingBoxesJob()
                        {
                            inTextureSize = textureSize,
                            inMeshTriangles = meshTriangles,
                            inMeshUv1 = meshUv1,
                            outTriangleUv1BoundingBoxes = triangleUv1BoundingBoxes,
                        };

                        // begin processing the job on multiple threads.
                        processTriangleUv1BoundingBoxesJobHandle = processTriangleUv1BoundingBoxesJob.Schedule(triangleNormals.Length, 64);
                    }

                    // wait here until the jobs above have finished.
                    JobHandle.CompleteAll(ref processTrianglesJobHandle, ref processTriangleUv1BoundingBoxesJobHandle);
                }
                else
                {
                    // wait here until the jobs above have finished.
                    processTrianglesJobHandle.Complete();
                }

                // dispose of the native memory.
                nativeVertices.Dispose();
            }

            public void Dispose()
            {
                // dispose of the native memory.
                worldVertices.Dispose();
                meshTriangles.Dispose();
                meshUv1.Dispose();
                triangleNormals.Dispose();

                if (hasLightmapCoordinates)
                    triangleUv1BoundingBoxes.Dispose();
            }

            /// <summary>Gets the 3 vertices that make up the triangle at the given triangle index.</summary>
            /// <param name="triangleIndex">The index of the triangle in the mesh.</param>
            /// <returns>The 3 vertices that make up the triangle.</returns>
            public (Vector3 a, Vector3 b, Vector3 c) GetTriangleVertices(int triangleIndex)
            {
                triangleIndex *= 3;
                var v1 = worldVertices[meshTriangles[triangleIndex]];
                var v2 = worldVertices[meshTriangles[triangleIndex + 1]];
                var v3 = worldVertices[meshTriangles[triangleIndex + 2]];
                return (v1, v2, v3);
            }

            /// <summary>
            /// Gets the 3 vertex UV1 coordinates for the triangle at the given triangle index. The flag
            /// <see cref="hasLightmapCoordinates"/> must be true before calling this function.
            /// </summary>
            /// <param name="triangleIndex">The index of the triangle in the mesh.</param>
            /// <returns>The 3 vertex UV1 coordinates associated with the triangle.</returns>
            public (Vector3 a, Vector3 b, Vector3 c) GetTriangleUv1(int triangleIndex)
            {
                triangleIndex *= 3;
                var v1 = meshUv1[meshTriangles[triangleIndex]];
                var v2 = meshUv1[meshTriangles[triangleIndex + 1]];
                var v3 = meshUv1[meshTriangles[triangleIndex + 2]];
                return (v1, v2, v3);
            }
        }
    }
}