using UnityEngine;

namespace AlpacaIT.DynamicLighting.Internal
{
    /// <summary>
    /// Uses <see cref="Time.realtimeSinceStartup"/> to measure the elapsed time for benchmarking purposes.
    /// </summary>
    internal class BenchmarkTimer
    {
        /// <summary>The last tracked time.</summary>
        private double time;

        /// <summary>The total time elapsed since creation of this instance.</summary>
        private double timeAccumulator;

        /// <summary>
        /// Ensures that <see cref="Stop"/> is not called before <see cref="Begin"/> or multiple times.
        /// </summary>
        private bool wasBeginCalled = false;

        /// <summary>
        /// Gets the total time elapsed between all calls of <see cref="Begin"/> and <see cref="Stop"/>.
        /// </summary>
        public double totalTime => timeAccumulator;

        /// <summary>Begins measuring the time from this point forward.</summary>
        public void Begin()
        {
            if (wasBeginCalled) throw new System.Exception("Unable to call Begin() again before calling Stop()");
            wasBeginCalled = true;

            time = Time.realtimeSinceStartupAsDouble;
        }

        /// <summary>
        /// Returns the time elapsed since the last call to <see cref="Begin"/> and adds it to the
        /// <see cref="totalTime"/> elapsed.
        /// </summary>
        /// <returns>The time elapsed in seconds as a floating point number.</returns>
        public double Stop()
        {
            if (!wasBeginCalled) throw new System.Exception("Unable to call Stop() before calling Begin()");
            wasBeginCalled = false;

            var elapsed = Time.realtimeSinceStartupAsDouble - time;
            timeAccumulator += elapsed;
            return elapsed;
        }

        /// <summary>
        /// Returns the total time elapsed as a string in a trimmed hh:mm:ss(suffix) format.
        /// </summary>
        /// <returns>The string representation of the elapsed time.</returns>
        public override string ToString()
        {
            if (totalTime < 60.0)
                return System.TimeSpan.FromSeconds(totalTime).ToString("s\\.fff\\s");
            else if (totalTime < 3600.0)
                return System.TimeSpan.FromSeconds(totalTime).ToString("m\\:ss\\.fff\\m");
            else
                return System.TimeSpan.FromSeconds(totalTime).ToString("h\\:mm\\:ss\\.fff\\h");
        }
    }
}