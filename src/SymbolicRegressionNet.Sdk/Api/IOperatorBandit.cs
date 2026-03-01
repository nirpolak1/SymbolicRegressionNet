using System;
using System.Collections.Generic;

namespace SymbolicRegressionNet.Sdk.Api
{
    public enum GeneticOperator
    {
        Crossover,
        PointMutation,
        SubtreeMutation,
        HoistMutation
    }

    /// <summary>
    /// Contract for reinforcement learning algorithms tracking the success
    /// of different genetic operators to dynamically adapt application probabilities.
    /// </summary>
    public interface IOperatorBandit
    {
        /// <summary>
        /// Selects the next operator to apply based on learned exploration/exploitation.
        /// </summary>
        GeneticOperator SelectOperator();

        /// <summary>
        /// Receives feedback (e.g. fitness improvement delta) to update internal action values.
        /// </summary>
        void ObserveReward(GeneticOperator op, double reward);

        /// <summary>
        /// Gets the current learned probability distribution for analytics tracking.
        /// </summary>
        IReadOnlyDictionary<GeneticOperator, double> GetProbabilities();
    }
}
