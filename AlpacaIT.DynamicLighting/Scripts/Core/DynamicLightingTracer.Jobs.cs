using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    // implements several classes to help process jobs.

    internal partial class DynamicLightingTracer
    {
        /// <summary>
        /// Uses the Unity job system to schedule raycasts. It will let the main thread prepare more
        /// raycasts while background threads execute them.
        /// </summary>
        private unsafe class RaycastProcessor
        {
            /// <summary>
            /// The amount of commands that will be stored before scheduling it on the job system.
            /// </summary>
            private const int batchSize = 1024 * 1024; // 1 MiB per byte stored.

            /// <summary>The <see cref="RaycastCommand"/> to be scheduled on the job system.</summary>
            private NativeArray<RaycastCommand> nativeRaycastCommandsAccumulator = new NativeArray<RaycastCommand>(batchSize, Allocator.TempJob, NativeArrayOptions.UninitializedMemory); // 52 MiB

            /// <summary>Native collection used by the currently active jobs.</summary>
            private NativeArray<RaycastCommand> nativeRaycastCommands = new NativeArray<RaycastCommand>(batchSize, Allocator.TempJob, NativeArrayOptions.UninitializedMemory); // 52 MiB

            /// <summary>Native collection used by the currently active jobs.</summary>
            private NativeArray<RaycastHit> nativeRaycastHits = new NativeArray<RaycastHit>(batchSize, Allocator.TempJob, NativeArrayOptions.UninitializedMemory); // 44 MiB

            /// <summary>Additional information per <see cref="RaycastCommand"/> for the results.</summary>
            private RaycastCommandMeta[] raycastCommandsMeta = new RaycastCommandMeta[batchSize]; // 24 MiB

            /// <summary>Additional information per <see cref="RaycastCommand"/> for the results.</summary>
            private RaycastCommandMeta[] raycastCommandsMetaAccumulator = new RaycastCommandMeta[batchSize]; // 24 MiB

            /// <summary>
            /// The amount of items in our collections, so that we do not have to check <see cref="List{T}.Count"/>.
            /// </summary>
            private int countAccumulator;

            /// <summary>The amount of items in the active job collections.</summary>
            private int count;

            /// <summary>The <see cref="JobHandle"/> pointing towards an active job.</summary>
            private JobHandle jobHandle;

            /// <summary>Whether the <see cref="jobHandle"/> has been scheduled.</summary>
            private bool wasActive = false;

            /// <summary>Contains the pixels of the lightmap.</summary>
            public uint* pixelsLightmap;
            public int lightmapSize;

            /// <summary>Samples the <see cref="Material"/> color and texture.</summary>
            public MaterialSampler materialSampler;

            public void Add(RaycastCommand raycastCommand, RaycastCommandMeta raycastCommandMeta)
            {
                nativeRaycastCommandsAccumulator[countAccumulator] = raycastCommand;
                raycastCommandsMetaAccumulator[countAccumulator] = raycastCommandMeta;
                countAccumulator++;

                // wait until we filled up our internal lists.
                if (countAccumulator >= batchSize)
                {
                    Execute();
                }
            }

            private void Execute()
            {
                // our internal lists have been filled up but the jobs may still be busy.
                if (wasActive)
                {
                    wasActive = false;

                    // we can help expedite the work by processing work on the main thread too,
                    // effectively waiting here until we are done.
                    jobHandle.Complete();

                    // process the results.
                    ProcessResults();
                }

                if (countAccumulator > 0)
                {
                    // remember that we are active now.
                    wasActive = true;

                    // copy the accumulators into the working arrays.
                    NativeArray<RaycastCommand>.Copy(nativeRaycastCommandsAccumulator, nativeRaycastCommands, countAccumulator);
                    Array.Copy(raycastCommandsMetaAccumulator, raycastCommandsMeta, countAccumulator);

                    // schedule the batched raycast commands on the job system but do not wait here.
                    jobHandle = RaycastCommand.ScheduleBatch(nativeRaycastCommands.GetSubArray(0, countAccumulator), nativeRaycastHits, 256);
                    count = countAccumulator;
                    countAccumulator = 0;

                    // the main thread will continue accumulating more work while we are busy processing
                    // raycasts. hopefully by the time we finish, new raycasts will already be prepared.
                    JobHandle.ScheduleBatchedJobs(); // this will begin executing the scheduled raycasts.
                }
            }

            public void Complete()
            {
                // process any active work and begin scheduling remaining work.
                Execute();

                if (wasActive)
                {
                    // finish the remaining work.
                    jobHandle.Complete();

                    // process the results.
                    ProcessResults();

                    // we are no longer active.
                    wasActive = false;
                }
            }

            private void ProcessResults()
            {
                uint* p = pixelsLightmap;
                int pixelsLightmapSize = lightmapSize;

                for (int i = 0; i < count; i++)
                {
                    var meta = raycastCommandsMeta[i];
                    var hit = nativeRaycastHits[i];

#if !UNITY_2021_2_OR_NEWER
                    if (hit.distance == 0f && hit.point.Equals(Vector3.zero))
#else
                    if (hit.colliderInstanceID == 0)
#endif
                    {
                        p[meta.y * pixelsLightmapSize + meta.x] |= (uint)1 << ((int)meta.lightChannel);

                        // skip further processing when this light is direct illumination only.
                        if (meta.illuminationSamples == null)
                            continue;

                        if (UnityEngine.Random.value < 0.01f)
                        {
                            var size = Mathf.FloorToInt(Mathf.Max(1.0f, meta.lightRadius * 4f));

                            var voxelPosition = (meta.world - meta.lightPosition) * 4f;
                            voxelPosition += new Vector3(size * 0.5f, size * 0.5f, size * 0.5f);

                            if (voxelPosition.x < 0f || voxelPosition.y < 0f || voxelPosition.z < 0f)
                            {
                                continue;
                            }

                            if (voxelPosition.x >= meta.illuminatedVoxels.Width || voxelPosition.y >= meta.illuminatedVoxels.Height || voxelPosition.z >= meta.illuminatedVoxels.Depth)
                            {
                                continue;
                            }

                            if (meta.illuminatedVoxels[Mathf.FloorToInt(voxelPosition.x), Mathf.FloorToInt(voxelPosition.y), Mathf.FloorToInt(voxelPosition.z)])
                                continue;

                            //Debug.DrawLine(meta.world, meta.world + Vector3.up * 0.05f, Color.blue, 30f);
                            // store this illuminated sample for the bounce lighting pass.
                            var color = materialSampler.Sample(0, 0);
                            meta.illuminationSamples.Add(new IlluminationSample(meta.world, meta.normal, new Vector3(color.r, color.g, color.b)));

                            meta.illuminatedVoxels[Mathf.FloorToInt(voxelPosition.x), Mathf.FloorToInt(voxelPosition.y), Mathf.FloorToInt(voxelPosition.z)] = true;
                        }
                    }
                }
            }

            public void Dispose()
            {
                if (wasActive) throw new Exception("Unable to Dispose of an active " + nameof(DynamicLightingTracer));

                if (nativeRaycastCommands.IsCreated)
                    nativeRaycastCommands.Dispose();

                if (nativeRaycastCommandsAccumulator.IsCreated)
                    nativeRaycastCommandsAccumulator.Dispose();

                if (nativeRaycastHits.IsCreated)
                    nativeRaycastHits.Dispose();
            }
        }
    }
}