using System.IO.Compression;
using Unity.Jobs;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>Job that decompresses a <see cref="GZipStream"/> as <typeparamref name="T"/>.</summary>
    internal unsafe struct DecompressGZipStreamJob<T> : IJob where T : struct
    {
        /// <summary>The input compressed data to be decompressed.</summary>
        private JobArrayPointer inCompressed;

        /// <summary>The output decompressed data (length must be at least the decompressed size).</summary>
        private JobArrayPointer outDecompressed;

        /// <summary>Creates a new instance of <see cref="InternalDecompressUInt32Job"/>.</summary>
        /// <param name="inCompressed">The input compressed data to be decompressed.</param>
        /// <param name="outDecompressed">The output decompressed data.</param>
        public DecompressGZipStreamJob(JobArrayPointer inCompressed, JobArrayPointer outDecompressed)
        {
            this.inCompressed = inCompressed;
            this.outDecompressed = outDecompressed;
        }

        /// <summary>Do not call this method directly, it is called by <see cref="IJob"/>.</summary>
        public readonly void Execute()
        {
            using var compressed = new NativeArrayStream<byte>(inCompressed.pointer, inCompressed.length);
            using var decompressed = new NativeArrayStream<T>(outDecompressed.pointer, outDecompressed.length);
            using var gzip = new GZipStream(compressed, CompressionMode.Decompress);
            gzip.CopyTo(decompressed);
        }
    }
}