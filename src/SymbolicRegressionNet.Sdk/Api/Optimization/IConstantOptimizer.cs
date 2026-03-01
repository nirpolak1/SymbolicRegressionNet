using SymbolicRegressionNet.Sdk.Data;
using SymbolicRegressionNet.Sdk.Api.Evaluators;

namespace SymbolicRegressionNet.Sdk.Api.Optimization
{
    /// <summary>
    /// Contract for optimizing constant parameters embedded within an abstract syntax tree.
    /// Can be utilized to update non-linear parameters using exact AD gradients.
    /// </summary>
    public interface IConstantOptimizer
    {
        /// <summary>
        /// Attempts to optimize constant nodes in the program to minimize MSE against the dataset.
        /// </summary>
        /// <returns>True if constants were modified and improved fitness.</returns>
        bool OptimizeConstants(Instruction[] program, Dataset dataset, IEvaluator evaluator);
    }
}
