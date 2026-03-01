using System.Collections.Generic;
using Xunit;
using SymbolicRegressionNet.Sdk.Api.Optimization;
using SymbolicRegressionNet.Sdk.Api.Evaluators;
using SymbolicRegressionNet.Sdk.Data;

namespace SymbolicRegressionNet.Sdk.Tests
{
    public class ConstantOptimizerTests
    {
        [Fact]
        public void LbfgsOptimizer_ShiftsConstant_ToMinimizeMSE()
        {
            // Data generated from f(x) = x + 3.14
            var features = new double[,] { { 1 }, { 2 }, { 3 } };
            var target = new double[] { 4.14, 5.14, 6.14 };
            var ds = Dataset.FromArray(features, target);

            // Program: x + c (where c starts bad, e.g., 0)
            var program = new[]
            {
                new Instruction(OpCode.PushVar, 0),
                new Instruction(OpCode.PushConst, 0.0), // c requires tuning
                new Instruction(OpCode.Add)
            };

            var evaluator = new FastStackEvaluator(program);
            var lbfgs = new LbfgsOptimizer(learningRate: 0.1, maxIterations: 50);

            bool improved = lbfgs.OptimizeConstants(program, ds, evaluator);

            Assert.True(improved);
            // Verify c got closer to 3.14 from 0.0
            double newC = program[1].Value;
            Assert.True(newC > 0.1); 
        }
    }
}
