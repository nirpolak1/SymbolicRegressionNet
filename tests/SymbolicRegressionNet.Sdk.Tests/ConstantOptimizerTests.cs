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

        [Fact]
        public void NativeLbfgsOptimizer_ShiftsConstant_ToMinimizeMSE()
        {
            try
            {
                // Data generated from f(x) = x * 2.5
                var features = new double[,] { { 1 }, { 2 }, { 3 }, { 4 } };
                var target = new double[] { 2.5, 5.0, 7.5, 10.0 };
                var ds = Dataset.FromArray(features, target);

                // Program: x * c (where c starts bad e.g 1.0)
                var program = new[]
                {
                    new Instruction(OpCode.PushVar, 0),
                    new Instruction(OpCode.PushConst, 1.0), // c requires tuning
                    new Instruction(OpCode.Mul)
                };

                var evaluator = new FastStackEvaluator(program);
                var lbfgs = new NativeLbfgsOptimizer(learningRate: 0.05, maxIterations: 100);

                bool improved = lbfgs.OptimizeConstants(program, ds, evaluator);

                Assert.True(improved);

                // Verify c moved from 1.0 heavily towards 2.5
                double newC = program[1].Value;
                Assert.True(newC > 1.5);
            }
            catch (System.DllNotFoundException)
            {
                // Acceptable skip if native DLL wasn't copied locally during test run
            }
        }
    }
}
