using System;
using System.IO.Compression;
using Unity.Collections;
using Unity.Jobs;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>Decompresses a <see cref="CompressedUInt32"/> on the Unity Job System.</summary>
    internal class DecompressGZipStreamHandle<T> : IDisposable where T : struct
    {
        /// <summary>Pointer to the input compressed data.</summary>
        private JobArrayPointer inCompressed;

        /// <summary>Pointer to the output decompressed data.</summary>
        private JobArrayPointer outDecompressed;

        /// <summary>The <see cref="JobHandle"/> on the Unity Job System.</summary>
        private JobHandle jobHandle;

        /// <summary>Once the job has completed contains the decompressed data.</summary>
        private NativeArrayStream<T> decompressedStream;

        /// <summary>Whether this job has been scheduled on the Unity Job System.</summary>
        private bool scheduled;

        /// <summary>
        /// Creates a new instance of <see cref="DecompressGZipStreamHandle{T}"/>.
        /// </summary>
        /// <param name="compressed">The compressed <see cref="GZipStream"/> data.</param>
        /// <param name="length">The length of the decompressed data in bytes (or more).</param>
        public DecompressGZipStreamHandle(byte[] compressed, int length)
        {
            // create a pointer to the compressed data.
            inCompressed = JobArrayPointer.Create(compressed);

            // allocate a native array stream to write decompressed data into.
            decompressedStream = new NativeArrayStream<T>(length);
            outDecompressed = JobArrayPointer.Create(decompressedStream);
        }

        /// <summary>Begins decompressing the data on the Unity Job System.</summary>
        /// <param name="dependsOn">
        /// The dependency of the job. Dependencies ensure that a job executes on worker threads
        /// after the dependency has completed execution, and that two jobs reading or writing to
        /// same data do not run in parallel.
        /// </param>
        public void Schedule(JobHandle dependsOn = default)
        {
            if (scheduled) return;
            scheduled = true;
            jobHandle = new DecompressGZipStreamJob<uint>(inCompressed, outDecompressed).Schedule(dependsOn);
        }

        /// <summary>Ensures that the decompression job has completed.</summary>
        public void Complete()
        {
            Schedule();
            jobHandle.Complete();
        }

        /// <summary>Gets whether the decompression job has finished.</summary>
        public bool isCompleted => scheduled && jobHandle.IsCompleted;

        /// <summary>Gets the decompressed data (and ensures the job has completed).</summary>
        public NativeArrayStream<T> stream
        {
            get
            {
                // we must ensure that the job has completed before reading.
                Complete();
                return decompressedStream;
            }
        }

        #region Fancy Functions

        /// <summary>The length of the decompressed data in bytes.</summary>
        public int length => stream.length;

        /// <summary>Gets the decompressed data as <see cref="NativeArray{T}"/>.</summary>
        /// <returns>The decompressed data as <see cref="NativeArray{T}"/>.</returns>
        public NativeArray<T> GetNativeArray() => stream.GetNativeArray();

        #endregion Fancy Functions

        protected virtual void Dispose(bool disposing)
        {
            // we must ensure that the job has completed to prevent writing to dangling pointers.
            Complete();

            // job array pointers must be disposed of manually as they are structs.
            inCompressed.Dispose();
            outDecompressed.Dispose();

            // dispose of the decompressed data stream.
            decompressedStream.Dispose();
        }

        #region Dispose Pattern

        ~DecompressGZipStreamHandle()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion Dispose Pattern
    }
}