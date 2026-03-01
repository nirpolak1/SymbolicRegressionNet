using System.Collections.Generic;
using SymbolicRegressionNet.Sdk.Data;

namespace SymbolicRegressionNet.Sdk.Api.Evaluators
{
    /// <summary>
    /// Represents an evaluator capable of processing multiple programs simultaneously against a dataset,
    /// enabling SIMD (Single Instruction, Multiple Data) or SIMT (Single Instruction, Multiple Threads)
    /// architectures like AVX2 CPU vectorization or ILGPU/ComputeSharp GPU execution.
    /// </summary>
    public interface IBatchEvaluator
    {
        /// <summary>
        /// Evaluates a batch of distinct programs simultaneously. 
        /// </summary>
        /// <param name="programs">The list of instruction programs (abstract syntax trees).</param>
        /// <param name="data">The dataset to evaluate against.</param>
        /// <param name="resultsBuffer">A 2D output buffer [programIndex, rowIndex] to store evaluations without allocating. Must be sized appropriately.</param>
        void EvaluateBatch(IReadOnlyList<Instruction[]> programs, Dataset data, double[,] resultsBuffer);
    }
}
