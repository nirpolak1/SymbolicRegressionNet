using System.Collections.Generic;

namespace SymbolicRegressionNet.Sdk.Api
{
    /// <summary>
    /// Represents an evaluated individual in the population for selection purposes.
    /// </summary>
    public readonly struct SelectionCandidate
    {
        public string Equation { get; }
        public double Fitness { get; }
        public int Complexity { get; }

        public SelectionCandidate(string equation, double fitness, int complexity)
        {
            Equation = equation;
            Fitness = fitness;
            Complexity = complexity;
        }
    }

    /// <summary>
    /// Strategy for selecting parents for the next generation.
    /// </summary>
    public interface ISelectionStrategy
    {
        /// <summary>
        /// Selects a specified number of candidates from the population.
        /// </summary>
        IReadOnlyList<SelectionCandidate> Select(IReadOnlyList<SelectionCandidate> population, int count);
    }
}
