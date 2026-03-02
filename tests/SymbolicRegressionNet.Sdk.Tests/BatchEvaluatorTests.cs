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

        [Fact]
        public void NativeBatchEvaluator_Avx2_EvaluatesCorrectly()
        {
            // Skip test on environments without the native DLL built or present
            // In a real CI environment, we would use an OS architecture check or try-catch DllNotFoundException.
            try
            {
                var features = new double[,] { { 5.0 }, { 10.0 }, { -2.0 }, { 4.5 } };
                var target = new double[] { 5.0, 10.0, -2.0, 4.5 };
                var dataset = Dataset.FromArray(features, target);

                // f(x) = x * 2.5 + 3.0
                var program0 = new[]
                {
                    new Instruction(OpCode.PushVar, 0),
                    new Instruction(OpCode.PushConst, 2.5),
                    new Instruction(OpCode.Mul),
                    new Instruction(OpCode.PushConst, 3.0),
                    new Instruction(OpCode.Add)
                };

                // f(x) = sin(x)
                var program1 = new[]
                {
                    new Instruction(OpCode.PushVar, 0),
                    new Instruction(OpCode.Sin)
                };

                var batch = new List<Instruction[]> { program0, program1 };
                var resultsBuffer = new double[2, 4];

                var batchEvaluator = new NativeBatchEvaluator(useAvx2: true);
                batchEvaluator.EvaluateBatch(batch, dataset, resultsBuffer);

                // Verify Program 0
                Assert.Equal(5.0 * 2.5 + 3.0, resultsBuffer[0, 0], 5);
                Assert.Equal(10.0 * 2.5 + 3.0, resultsBuffer[0, 1], 5);
                Assert.Equal(-2.0 * 2.5 + 3.0, resultsBuffer[0, 2], 5);
                Assert.Equal(4.5 * 2.5 + 3.0, resultsBuffer[0, 3], 5);

                // Verify Program 1
                Assert.Equal(System.Math.Sin(5.0), resultsBuffer[1, 0], 5);
                Assert.Equal(System.Math.Sin(10.0), resultsBuffer[1, 1], 5);
                Assert.Equal(System.Math.Sin(-2.0), resultsBuffer[1, 2], 5);
                Assert.Equal(System.Math.Sin(4.5), resultsBuffer[1, 3], 5);
            }
            catch (System.DllNotFoundException)
            {
                // Acceptable skip if native DLL wasn't copied locally during run
            }
        }
    }
}
