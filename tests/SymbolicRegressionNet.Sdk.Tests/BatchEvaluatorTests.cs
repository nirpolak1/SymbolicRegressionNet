using System.Collections.Generic;
using Xunit;
using SymbolicRegressionNet.Sdk.Api.Evaluators;
using SymbolicRegressionNet.Sdk.Data;

namespace SymbolicRegressionNet.Sdk.Tests
{
    public class BatchEvaluatorTests
    {
        [Fact]
        public void StubGpuBatchEvaluator_EvaluatesMultiplePrograms_Correctly()
        {
            // Simple dataset: 2 rows, 1 feature mapped as f(x) = x
            var features = new double[,] { { 5.0 }, { 10.0 } };
            var target = new double[] { 5.0, 10.0 };
            var dataset = Dataset.FromArray(features, target);

            // Program 0: x0 + 1 => { 6.0, 11.0 }
            var program0 = new[]
            {
                new Instruction(OpCode.PushVar, 0),
                new Instruction(OpCode.PushConst, 1.0),
                new Instruction(OpCode.Add)
            };

            // Program 1: x0 * 2 => { 10.0, 20.0 }
            var program1 = new[]
            {
                new Instruction(OpCode.PushVar, 0),
                new Instruction(OpCode.PushConst, 2.0),
                new Instruction(OpCode.Mul)
            };

            var batch = new List<Instruction[]> { program0, program1 };
            var resultsBuffer = new double[2, 2]; // 2 programs, 2 rows

            var batchEvaluator = new StubGpuBatchEvaluator();
            batchEvaluator.EvaluateBatch(batch, dataset, resultsBuffer);

            // Verify Program 0
            Assert.Equal(6.0, resultsBuffer[0, 0]);
            Assert.Equal(11.0, resultsBuffer[0, 1]);

            // Verify Program 1
            Assert.Equal(10.0, resultsBuffer[1, 0]);
            Assert.Equal(20.0, resultsBuffer[1, 1]);
        }
    }
}
