namespace AlpacaIT.DynamicLighting
{
    internal partial class DynamicLightingTracer
    {
        /// <summary>Represents a raytracing step to be processed.</summary>
        public interface IStep
        {
            /// <summary>Executes this step and returns once finished.</summary>
            public void Execute();
        }
    }
}