using System.Collections.Generic;

namespace SymbolicRegressionNet.Sdk.Reporting
{
    /// <summary>
    /// Defines a contract for exporting symbolic regression models into various target formats
    /// (e.g., LaTeX, Python, C++ Code).
    /// </summary>
    public interface IExpressionExporter
    {
        /// <summary>
        /// Gets the name of the target format (e.g., "LaTeX", "Python", "C++").
        /// </summary>
        string FormatName { get; }

        /// <summary>
        /// Exports a single DiscoveredModel expression string into the target format.
        /// </summary>
        /// <param name="expression">The raw infix expression string (e.g., "x0 * sin(x1)").</param>
        /// <param name="variableNames">Optional mapping of variable indices (x0, x1) to friendly names (e.g., "Age", "Weight").</param>
        /// <returns>The formatted target expression.</returns>
        string Export(string expression, IReadOnlyList<string> variableNames = null);

        /// <summary>
        /// Exports an entire Hall of Fame Pareto front into the target format.
        /// </summary>
        /// <param name="hallOfFame">The Pareto front of models.</param>
        /// <param name="variableNames">Optional mapping of variable names.</param>
        /// <returns>A formatted document or block containing all models.</returns>
        string ExportAll(HallOfFame hallOfFame, IReadOnlyList<string> variableNames = null);
    }
}
