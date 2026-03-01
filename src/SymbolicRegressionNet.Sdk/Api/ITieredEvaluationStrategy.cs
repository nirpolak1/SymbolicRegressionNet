using SymbolicRegressionNet.Sdk.Data;

namespace SymbolicRegressionNet.Sdk.Api
{
    /// <summary>
    /// Represents the result of a tiered evaluation check.
    /// </summary>
    public struct TieredCheckResult
    {
        public bool Passed { get; }
        public double EstimatedFitness { get; }

        public TieredCheckResult(bool passed, double estimatedFitness)
        {
            Passed = passed;
            EstimatedFitness = estimatedFitness;
        }
    }

    /// <summary>
    /// Strategy to filter out poorly performing individuals using a small subset
    /// of data before evaluating them against the full rigorous dataset.
    /// </summary>
    public interface ITieredEvaluationStrategy
    {
        /// <summary>
        /// Checks if an individual should proceed to full evaluation based on a preliminary test.
        /// </summary>
        TieredCheckResult CheckShouldEvaluateFull(Evaluators.IEvaluator evaluator, Evaluators.EvaluationContext context, Dataset fullData);
    }
}
