using System;
using System.Collections.Generic;
using System.Linq;

namespace SymbolicRegressionNet.Sdk.Api
{
    /// <summary>
    /// An Epsilon-Greedy Multi-Armed Bandit algorithm.
    /// Balances exploring new operators with a small probability (Epsilon) 
    /// while exploiting the best historically empirical operator most of the time.
    /// </summary>
    public class EpsilonGreedyBandit : IOperatorBandit
    {
        private readonly double _epsilon;
        private readonly Random _rng;

        private readonly Dictionary<GeneticOperator, double> _qValues;
        private readonly Dictionary<GeneticOperator, int> _counts;

        public EpsilonGreedyBandit(double epsilon = 0.1, int? seed = null)
        {
            _epsilon = epsilon;
            _rng = seed.HasValue ? new Random(seed.Value) : new Random();
            
            _qValues = new Dictionary<GeneticOperator, double>();
            _counts = new Dictionary<GeneticOperator, int>();

            foreach (GeneticOperator op in Enum.GetValues(typeof(GeneticOperator)))
            {
                // Optimistic initialization to encourage early exploration of all arms.
                _qValues[op] = 1.0; 
                _counts[op] = 0;
            }
        }

        public GeneticOperator SelectOperator()
        {
            if (_rng.NextDouble() < _epsilon)
            {
                // Explore
                var ops = _qValues.Keys.ToArray();
                return ops[_rng.Next(ops.Length)];
            }
            else
            {
                // Exploit (argmax Q)
                return _qValues.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
            }
        }

        public void ObserveReward(GeneticOperator op, double reward)
        {
            _counts[op]++;
            // Incremental update rule: Q_{n+1} = Q_n + alpha(R - Q_n). Using empirical mean alpha = 1/N.
            double alpha = 1.0 / _counts[op];
            _qValues[op] += alpha * (reward - _qValues[op]);
        }

        public IReadOnlyDictionary<GeneticOperator, double> GetProbabilities()
        {
            double sumQ = _qValues.Values.Sum(v => Math.Max(0.0, v)); // Clamp negatives for probability dist if needed
            if (sumQ <= 0) sumQ = 1e-9;
            return _qValues.ToDictionary(k => k.Key, v => Math.Max(0.0, v.Value) / sumQ);
        }
    }
}
