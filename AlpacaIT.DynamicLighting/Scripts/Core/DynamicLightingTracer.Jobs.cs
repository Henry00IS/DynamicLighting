using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
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
        private class RaycastProcessor
        {
            /// <summary>
            /// The amount of commands that will be stored before scheduling it on the job system.
            /// </summary>
            private const int batchSize = 1024 * 1024; // 1 MiB per byte stored.

            /// <summary>The <see cref="RaycastCommand"/> to be scheduled on the job system.</summary>
            private List<RaycastCommand> raycastCommands = new List<RaycastCommand>(batchSize); // 44 MiB

            /// <summary>Additional information per <see cref="RaycastCommand"/> for the results.</summary>
            private List<RaycastCommandMeta> raycastCommandsMeta = null;

            /// <summary>Additional information per <see cref="RaycastCommand"/> for the results.</summary>
            private List<RaycastCommandMeta> raycastCommandsMetaAccumulator = new List<RaycastCommandMeta>(batchSize); // 24 MiB

            /// <summary>Native collection used by the currently active jobs.</summary>
            private NativeArray<RaycastHit> nativeRaycastResults;

            /// <summary>Native collection used by the currently active jobs.</summary>
            private NativeArray<RaycastCommand> nativeRaycastCommands;

            /// <summary>
            /// The amount of items in our collections, so that we do not have to check <see cref="List{T}.Count"/>.
            /// </summary>
            private int count;

            /// <summary>The <see cref="JobHandle"/> pointing towards an active job.</summary>
            private JobHandle jobHandle;

            /// <summary>Whether the <see cref="jobHandle"/> has been scheduled.</summary>
            private bool wasActive = false;

            /// <summary>Called whenever a raycast hit nothing.</summary>
            public Action<RaycastCommandMeta> processRaycastResult;

            public void Add(RaycastCommand raycastCommand, RaycastCommandMeta raycastCommandMeta)
            {
                raycastCommands.Add(raycastCommand);
                raycastCommandsMetaAccumulator.Add(raycastCommandMeta);
                count++;

                // wait until we filled up our internal lists.
                if (count >= batchSize)
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

                    // clean up the native memory.
                    nativeRaycastResults.Dispose();
                    nativeRaycastCommands.Dispose();
                }

                if (count > 0)
                {
                    // remember that we are active now.
                    wasActive = true;

                    // pass the raycast commands to native memory for the job system.
                    nativeRaycastResults = new NativeArray<RaycastHit>(count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                    nativeRaycastCommands = new NativeArray<RaycastCommand>(raycastCommands.ToArray(), Allocator.TempJob);

                    // clear the managed memory and let it be used again to accumulate data.
                    raycastCommands.Clear();
                    raycastCommandsMeta = new List<RaycastCommandMeta>(raycastCommandsMetaAccumulator);
                    raycastCommandsMetaAccumulator.Clear();

                    // schedule the batched raycast commands on the job system but do not wait here.
                    jobHandle = RaycastCommand.ScheduleBatch(nativeRaycastCommands, nativeRaycastResults, 256);
                    count = 0;

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

                    // clean up the native memory.
                    nativeRaycastResults.Dispose();
                    nativeRaycastCommands.Dispose();

                    // we are no longer active.
                    wasActive = false;
                }
            }

            private void ProcessResults()
            {
                for (int i = 0; i < nativeRaycastResults.Length; i++)
                {
                    var meta = raycastCommandsMeta[i];
                    var hit = nativeRaycastResults[i];

#if !UNITY_2021_2_OR_NEWER
                    if (hit.distance == 0f && hit.point.Equals(Vector3.zero))
#else
                    if (hit.colliderInstanceID == 0)
#endif
                    {
                        processRaycastResult(meta);
                    }
                }
            }
        }
    }
}