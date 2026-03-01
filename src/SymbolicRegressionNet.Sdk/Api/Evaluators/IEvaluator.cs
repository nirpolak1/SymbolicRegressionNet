using SymbolicRegressionNet.Sdk.Data;
using System.Threading.Tasks;

namespace SymbolicRegressionNet.Sdk.Api.Evaluators
{
    /// <summary>
    /// Defines the hardware-agnostic contract for bulk-evaluating symbolic equations against a dataset.
    /// </summary>
    public interface IEvaluator
    {
        /// <summary>
        /// Gets the display name of the evaluator hardware engine (e.g. "SIMD-CPU", "ILGPU").
        /// </summary>
        string EngineName { get; }

        /// <summary>
        /// Evaluates the expression synchronously over the provided dataset features.
        /// </summary>
        /// <returns>An array containing the evaluated targets corresponding to each dataset row.</returns>
        double[] Evaluate(EvaluationContext context, Dataset testData);

        /// <summary>
        /// Evaluates the expression asynchronously over the provided dataset features.
        /// </summary>
        Task<double[]> EvaluateAsync(EvaluationContext context, Dataset testData);
    }
}
