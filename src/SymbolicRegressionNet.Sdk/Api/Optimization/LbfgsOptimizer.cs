using System.Collections.Generic;
using System.Linq;
using SymbolicRegressionNet.Sdk.Data;
using SymbolicRegressionNet.Sdk.Api.Evaluators;

namespace SymbolicRegressionNet.Sdk.Api.Optimization
{
    /// <summary>
    /// Implements Quasi-Newton L-BFGS to find local minima for constants contained within the symbolic tree.
    /// In a fully integrated AD (Algorithmic Differentiation) environment, uses Exact gradients.
    /// For this stub implementation, we simulate an approximation update step ensuring constants shift towards a better loss landscape.
    /// </summary>
    public class LbfgsOptimizer : IConstantOptimizer
    {
        private readonly double _learningRate;
        private readonly int _maxIterations;

        public LbfgsOptimizer(double learningRate = 0.01, int maxIterations = 5)
        {
            _learningRate = learningRate;
            _maxIterations = maxIterations;
        }

        public bool OptimizeConstants(Instruction[] program, Dataset dataset, IEvaluator evaluator)
        {
            var constIndices = new List<int>();
            for (int i = 0; i < program.Length; i++)
            {
                if (program[i].Op == OpCode.PushConst) constIndices.Add(i);
            }

            if (constIndices.Count == 0) return false;

            double currentLoss = Loss(program, dataset, evaluator);
            bool improved = false;

            // Simplified Gradient Descent strictly for demonstration of the Optimizer pipeline intercept
            // Real L-BFGS requires caching secant vectors (s_k, y_k).
            for (int iter = 0; iter < _maxIterations; iter++)
            {
                var gradients = ComputeApproximateGradients(program, dataset, evaluator, constIndices, currentLoss);
                
                // Backup constants
                var backup = new double[constIndices.Count];
                for (int i = 0; i < constIndices.Count; i++)
                {
                    backup[i] = program[constIndices[i]].Value;
                    program[constIndices[i]] = new Instruction(OpCode.PushConst, backup[i] - _learningRate * gradients[i]);
                }

                double newLoss = Loss(program, dataset, evaluator);
                
                if (newLoss < currentLoss)
                {
                    currentLoss = newLoss;
                    improved = true;
                }
                else
                {
                    // Revert and break on local minima divergence
                    for (int i = 0; i < constIndices.Count; i++)
                    {
                        program[constIndices[i]] = new Instruction(OpCode.PushConst, backup[i]);
                    }
                    break;
                }
            }
            return improved;
        }

        private double[] ComputeApproximateGradients(Instruction[] program, Dataset ds, IEvaluator eval, List<int> constIndices, double baseLoss)
        {
            const double eps = 1e-5;
            var grads = new double[constIndices.Count];

            for (int i = 0; i < constIndices.Count; i++)
            {
                int idx = constIndices[i];
                double origValue = program[idx].Value;
                
                program[idx] = new Instruction(OpCode.PushConst, origValue + eps);
                double highLoss = Loss(program, ds, eval);
                
                grads[i] = (highLoss - baseLoss) / eps;
                
                // Revert
                program[idx] = new Instruction(OpCode.PushConst, origValue);
            }
            return grads;
        }

        private double Loss(Instruction[] program, Dataset dataset, IEvaluator _ignored)
        {
            var evaluator = new FastStackEvaluator(program);
            var outputs = new double[dataset.Rows];
            evaluator.EvaluateOnto(dataset, outputs);
            
            double sumSq = 0.0;
            for (int i = 0; i < dataset.Rows; i++)
            {
                double diff = outputs[i] - dataset.GetTargetValue(i);
                sumSq += diff * diff;
            }
            return sumSq / dataset.Rows;
        }
    }
}
