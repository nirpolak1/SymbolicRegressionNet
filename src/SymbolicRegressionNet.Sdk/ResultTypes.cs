using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SymbolicRegressionNet.Sdk
{
    /// <summary>
    /// Represents telemetry data for a single elapsed generation.
    /// </summary>
    public sealed record GenerationReport(
        int Generation,
        double BestMse,
        double BestR2,
        string BestEquation,
        int ParetoFrontSize
    );

    /// <summary>
    /// Represents a single discovered symbolic expression and its fitness metrics.
    /// </summary>
    public sealed record DiscoveredModel(
        string Expression,
        double Mse,
        double R2,
        int Complexity
    );

    /// <summary>
    /// Holds the collection of Pareto-optimal models discovered during the search.
    /// Implements IReadOnlyList for easy iteration and LINQ querying.
    /// </summary>
    public sealed class HallOfFame : IReadOnlyList<DiscoveredModel>
    {
        private readonly List<DiscoveredModel> _models = new();

        /// <summary>
        /// Gets the best discovered model based on R2 score.
        /// </summary>
        public DiscoveredModel Best => _models.OrderByDescending(m => m.R2).FirstOrDefault();

        public int Count => _models.Count;
        public DiscoveredModel this[int index] => _models[index];
        public IEnumerator<DiscoveredModel> GetEnumerator() => _models.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        internal void Add(DiscoveredModel model) => _models.Add(model);

        /// <summary>
        /// Retrieves the model with the highest R2 score that does not exceed the specified complexity budget.
        /// </summary>
        /// <param name="maxNodes">The maximum allowed tree nodes (complexity limit).</param>
        public DiscoveredModel GetBestByComplexity(int maxNodes)
        {
            return _models
                .Where(m => m.Complexity <= maxNodes)
                .OrderByDescending(m => m.R2)
                .FirstOrDefault();
        }

        /// <summary>
        /// Exports the Hall of Fame front to a CSV file.
        /// </summary>
        /// <param name="filePath">Target CSV file path.</param>
        public void ExportCsv(string filePath)
        {
            using var writer = new StreamWriter(filePath);
            writer.WriteLine("Complexity,MSE,R2,Expression");
            foreach (var model in _models.OrderBy(m => m.Complexity))
            {
                // Escape commas in the expression by quoting it.
                writer.WriteLine($"{model.Complexity},{model.Mse},{model.R2},\"{model.Expression.Replace("\"", "\"\"")}\"");
            }
        }
    }

    /// <summary>
    /// Represents the final immutable result of a symbolic regression run.
    /// </summary>
    public sealed record RegressionResult(
        string BestExpression,
        HallOfFame HallOfFame,
        int GenerationsRun,
        TimeSpan ElapsedTime
    );
}
