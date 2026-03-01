#nullable disable
using System.Collections.Generic;
using Xunit;
using SymbolicRegressionNet.Sdk.Api;
using SymbolicRegressionNet.Sdk.Api.Evaluators;
using SymbolicRegressionNet.Sdk.Data;

namespace SymbolicRegressionNet.Sdk.Tests
{
    public class DimensionalAnalysisTests
    {
        [Fact]
        public void DimensionalTieredStrategy_RejectsAdditionMismatches()
        {
            var ds = Dataset.FromArray(new double[,] { { 1 }, { 2 } }, new double[] { 1, 2 });
            var eval = new FallbackCpuEvaluator();
            var ctx = new EvaluationContext("mock", 1);
            
            // Feature 0 is length, Feature 1 is mass
            var dict = new Dictionary<int, DimensionalType>
            {
                { 0, new DimensionalType(1, 0, 0) }, // meters
                { 1, new DimensionalType(0, 1, 0) }  // kg
            };

            // x0 + x1 (meters + kg -> impossible)
            var program = new[]
            {
                new Instruction(OpCode.PushVar, 0),
                new Instruction(OpCode.PushVar, 1),
                new Instruction(OpCode.Add)
            };

            var strategy = new DimensionalTieredStrategy(program, DimensionalType.Dimensionless, dict);
            var result = strategy.CheckShouldEvaluateFull(eval, ctx, ds);

            Assert.False(result.Passed);
        }

        [Fact]
        public void DimensionalTieredStrategy_AcceptsConsistentDerivations()
        {
            var ds = Dataset.FromArray(new double[,] { { 1 }, { 2 } }, new double[] { 1, 2 });
            var eval = new FallbackCpuEvaluator();
            var ctx = new EvaluationContext("mock", 1);
            
            // Feature 0 is length (m), Feature 1 is time (s)
            var dict = new Dictionary<int, DimensionalType>
            {
                { 0, new DimensionalType(1, 0, 0) }, 
                { 1, new DimensionalType(0, 0, 1) }  
            };

            // Target is velocity (m/s) -> Length=1, Time=-1
            var targetDimension = new DimensionalType(1, 0, -1);

            // x0 / x1 -> (m / s)
            var program = new[]
            {
                new Instruction(OpCode.PushVar, 0),
                new Instruction(OpCode.PushVar, 1),
                new Instruction(OpCode.Div)
            };

            var strategy = new DimensionalTieredStrategy(program, targetDimension, dict);
            var result = strategy.CheckShouldEvaluateFull(eval, ctx, ds);

            Assert.True(result.Passed);
        }
    }
}
