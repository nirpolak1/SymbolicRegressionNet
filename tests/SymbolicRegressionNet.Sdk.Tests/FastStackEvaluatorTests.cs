#nullable disable
using Xunit;
using SymbolicRegressionNet.Sdk.Api.Evaluators;
using SymbolicRegressionNet.Sdk.Data;

namespace SymbolicRegressionNet.Sdk.Tests
{
    public class FastStackEvaluatorTests
    {
        [Fact]
        public void FastStackEvaluator_EvaluatesCorrectlyAndReturnsBuffer()
        {
            // Create a small 2-row, 2-feature dataset
            var dataset = Dataset.FromArray(new double[,] {
                { 1.0, 2.0 },
                { 3.0, 4.0 }
            }, new double[] { 0, 0 });
            
            // Postfix: x0 x1 * sin
            // In instructions: PushVar(0), PushVar(1), Mul, Sin
            var program = new Instruction[]
            {
                new Instruction(OpCode.PushVar, 0),
                new Instruction(OpCode.PushVar, 1),
                new Instruction(OpCode.Mul),
                new Instruction(OpCode.Sin)
            };

            var evaluator = new FastStackEvaluator(program);

            double[] results = new double[2];
            evaluator.EvaluateOnto(dataset, results);

            // Row 1: sin(1.0 * 2.0) = sin(2) = 0.909297
            Assert.Equal(System.Math.Sin(2.0), results[0], 5);
            
            // Row 2: sin(3.0 * 4.0) = sin(12) = -0.536573
            Assert.Equal(System.Math.Sin(12.0), results[1], 5);
        }
    }
}
