using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>
    /// Uses the Unity job system to schedule raycasts. It will let the main thread prepare more
    /// raycasts while background threads execute them.
    /// </summary>
    internal unsafe class RaycastProcessor
    {
        /// <summary>
        /// The amount of commands that will be stored before scheduling it on the job system.
        /// </summary>
        private const int batchSize = 256 * 256;

        /// <summary>The <see cref="RaycastCommand"/> accumulator array to be scheduled on the job system OR the array used by the currently active job.</summary>
        private NativeArray<RaycastCommand> nativeRaycastCommandsA = new NativeArray<RaycastCommand>(batchSize, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

        /// <summary>The <see cref="RaycastCommand"/> accumulator array to be scheduled on the job system OR the array used by the currently active job.</summary>
        private NativeArray<RaycastCommand> nativeRaycastCommandsB = new NativeArray<RaycastCommand>(batchSize, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

        /// <summary>The <see cref="RaycastCommand"/> accumulator array and array used by currently active jobs that keep swapping places.</summary>
        private RaycastCommandSwapper nativeRaycastCommands;

        /// <summary>Native collection used by the currently active jobs.</summary>
        private NativeArray<RaycastHit> nativeRaycastHits = new NativeArray<RaycastHit>(batchSize, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

        /// <summary>Additional information per <see cref="RaycastCommand"/> for the results.</summary>
        private RaycastHandler[] raycastCommandsHandlersA = new RaycastHandler[batchSize];

        /// <summary>Additional information per <see cref="RaycastCommand"/> for the results.</summary>
        private RaycastHandler[] raycastCommandsHandlersB = new RaycastHandler[batchSize];

        /// <summary>The <see cref="RaycastHandler"/> accumulator array and array used by processing results that keep swapping places.</summary>
        private RaycastHandlerSwapper raycastCommandsHandlers;

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

        // unsafe speedups:

        private RaycastHit* nativeRaycastHitsPtr;

        public RaycastProcessor()
        {
            nativeRaycastCommands = new RaycastCommandSwapper(nativeRaycastCommandsA, nativeRaycastCommandsB);
            raycastCommandsHandlers = new RaycastHandlerSwapper(raycastCommandsHandlersA, raycastCommandsHandlersB);
            nativeRaycastHitsPtr = (RaycastHit*)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(nativeRaycastHits);
        }

        public void Add(RaycastCommand raycastCommand, RaycastHandler raycastCommandMeta)
        {
            raycastCommandMeta.raycastsExpected++;
            nativeRaycastCommands.aPtr[countAccumulator] = raycastCommand;
            raycastCommandsHandlers.a[countAccumulator] = raycastCommandMeta;
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

                // swap the accumulators with the working arrays.
                nativeRaycastCommands.Swap();
                raycastCommandsHandlers.Swap();

                // schedule the batched raycast commands on the job system but do not wait here.
                jobHandle = RaycastCommand.ScheduleBatch(nativeRaycastCommands.b.GetSubArray(0, countAccumulator), nativeRaycastHits, 256);
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
            for (int i = 0; i < count; i++)
            {
                var handler = raycastCommandsHandlers.b[i];
                var hit = &nativeRaycastHitsPtr[i];

#if !UNITY_2021_2_OR_NEWER
                if (hit->distance == 0f && hit->point.Equals(Vector3.zero))
#else
                if (hit->colliderInstanceID == 0)
#endif
                {
                    handler.OnRaycastMiss();
                }

                handler.OnRaycastProcessed();
            }
        }

        public void Dispose()
        {
            if (wasActive) throw new Exception("Unable to Dispose of an active " + nameof(RaycastProcessor));

            if (nativeRaycastCommandsA.IsCreated)
                nativeRaycastCommandsA.Dispose();

            if (nativeRaycastCommandsB.IsCreated)
                nativeRaycastCommandsB.Dispose();

            if (nativeRaycastHits.IsCreated)
                nativeRaycastHits.Dispose();
        }
    }
}