namespace SymbolicRegressionNet.Sdk.Api.Evaluators
{
    /// <summary>
    /// Contextual information provided to the hardware evaluator during regression search.
    /// </summary>
    public readonly struct EvaluationContext
    {
        /// <summary>
        /// A string representation of the parsed math expression or its AST node sequence.
        /// </summary>
        public string ExpressionData { get; }

        /// <summary>
        /// Total number of variables the model uses.
        /// </summary>
        public int NumberOfVariables { get; }

        public EvaluationContext(string expressionData, int numberOfVariables)
        {
            ExpressionData = expressionData;
            NumberOfVariables = numberOfVariables;
        }
    }
}
