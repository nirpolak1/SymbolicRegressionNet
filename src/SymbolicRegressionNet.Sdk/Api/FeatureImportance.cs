using System.Collections.Generic;

namespace SymbolicRegressionNet.Sdk.Api
{
    /// <summary>
    /// Represents the inferred importance of a feature (variable) after a symbolic regression run.
    /// </summary>
    public readonly struct FeatureImportance
    {
        /// <summary>
        /// The index of the variable (e.g., 0 for x0).
        /// </summary>
        public int VariableIndex { get; }

        /// <summary>
        /// The optional name of the variable.
        /// </summary>
        public string VariableName { get; }

        /// <summary>
        /// The computed importance score natively scaled between 0.0 and 1.0.
        /// </summary>
        public double Score { get; }

        public FeatureImportance(int variableIndex, string variableName, double score)
        {
            VariableIndex = variableIndex;
            VariableName = variableName;
            Score = score;
        }

        public override string ToString() => $"{VariableName ?? $"x{VariableIndex}"}: {Score:P2}";
    }

    /// <summary>
    /// Contract for calculating feature importance scores from a discovered Pareto front of expressions.
    /// </summary>
    public interface IFeatureImportanceCalculator
    {
        /// <summary>
        /// Calculates importance scores for each feature present in the data.
        /// </summary>
        /// <param name="hallOfFame">The Pareto front of optimal models.</param>
        /// <param name="totalVariables">The total number of variables in the dataset.</param>
        /// <param name="variableNames">Optional mapping of variable names.</param>
        /// <returns>A list of features ordered by importance descending.</returns>
        IReadOnlyList<FeatureImportance> Calculate(HallOfFame hallOfFame, int totalVariables, IReadOnlyList<string> variableNames = null);
    }
}
