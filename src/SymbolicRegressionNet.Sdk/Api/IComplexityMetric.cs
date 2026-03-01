namespace SymbolicRegressionNet.Sdk.Api
{
    /// <summary>
    /// Contract for evaluating the structural complexity of a discovered expression.
    /// </summary>
    public interface IComplexityMetric
    {
        /// <summary>
        /// Calculates the complexity score of a given mathematical string expression.
        /// Higher scores indicate more complex models.
        /// </summary>
        /// <param name="expression">The string representation of the model (e.g., 'sin(x0) + 2.5').</param>
        /// <param name="baseComplexity">A fallback or base complexity value (e.g., node count proxy from engine).</param>
        /// <returns>The calculated complexity score.</returns>
        int CalculateComplexity(string expression, int baseComplexity);
    }
}
