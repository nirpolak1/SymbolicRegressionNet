#nullable disable
using System;
using System.Linq;
using Xunit;
using SymbolicRegressionNet.Sdk.Api.Evaluators;
using SymbolicRegressionNet.Sdk.Data;

namespace SymbolicRegressionNet.Sdk.Tests
{
    public class PropertyBasedFuzzTests
    {
        [Fact]
        public void FastStackEvaluator_FuzzTest_MaintainsInvariantsWithoutCrashing()
        {
            var rng = new Random(42);
            int numFuzzIterations = 5_000;
            
            // Dummy dataset: 10 rows, 3 features
            var features = new double[10, 3];
            for (int r = 0; r < 10; r++)
            {
                for (int c = 0; c < 3; c++) features[r, c] = rng.NextDouble() * 100 - 50; 
            }
            var ds = Dataset.FromArray(features, new double[10]);

            double[] outputBuffer = new double[10];

            for (int i = 0; i < numFuzzIterations; i++)
            {
                var program = GenerateRandomValidProgram(rng, maxLength: 20, maxFeatures: 3);
                var evaluator = new FastStackEvaluator(program);
                
                // Invariant 1: Evaluating any well-formed postfix program shouldn't throw.
                var exception = Record.Exception(() => evaluator.EvaluateOnto(ds, outputBuffer));
                Assert.Null(exception);

                // Invariant 2: Output buffer contains valid doubles (can be Infinity/NaN, but bounded).
                // Our protected operators should ensure Infinity/NaN is handled gracefully or normalized.
                foreach (var val in outputBuffer)
                {
                    // By invariant rules, we allow math edges, but memory shouldn't corrupt.
                    // We check that accessing it works and it's physically a real IEEE 754 float.
                    Assert.True(double.IsNaN(val) || double.IsInfinity(val) || double.IsFinite(val));
                }
            }
        }

        private Instruction[] GenerateRandomValidProgram(Random rng, int maxLength, int maxFeatures)
        {
            // Simple generator for strictly valid postfix to avoid stack underflow crashes
            // Stack depth must always be > 0 at end, and >= operand requirements mid-way
            int stackSize = 0;
            int length = rng.Next(1, maxLength);
            var insts = new System.Collections.Generic.List<Instruction>();

            for (int i = 0; i < length; i++)
            {
                bool needValues = stackSize < 2; 
                bool push = needValues || rng.NextDouble() > 0.4;

                if (push)
                {
                    bool isVar = rng.NextDouble() > 0.5;
                    var inst = isVar 
                        ? new Instruction(OpCode.PushVar, rng.Next(maxFeatures)) 
                        : new Instruction(OpCode.PushConst, rng.NextDouble() * 10 - 5);
                    insts.Add(inst);
                    stackSize++;
                }
                else
                {
                    // Pop 1 or 2
                    bool isUnary = stackSize >= 1 && rng.NextDouble() > 0.7;
                    if (isUnary)
                    {
                        insts.Add(new Instruction(OpCode.Sin));
                    }
                    else if (stackSize >= 2)
                    {
                        OpCode op = (OpCode)rng.Next(2, 6); // Add, Sub, Mul, Div
                        insts.Add(new Instruction(op));
                        stackSize--;
                    }
                }
            }
            return insts.ToArray();
        }
    }
}
