#nullable disable
using Xunit;
using SymbolicRegressionNet.Sdk.Api;
using SymbolicRegressionNet.Sdk.Api.Evaluators;
using SymbolicRegressionNet.Sdk.Data;

namespace SymbolicRegressionNet.Sdk.Tests
{
    public class IntervalPruningTests
    {
        [Fact]
        public void IntervalPruning_AllowsStableFunctions_AndRejectsUnstable()
        {
            // Simple dataset mapped to roughly [0.1, 10.0]
            var features = new double[,] { { 0.1 }, { 5.0 }, { 10.0 } };
            var ds = Dataset.FromArray(features, new double[] { 0, 0, 0 });
            var context = new EvaluationContext("mock", 1);
            var evaluator = new FallbackCpuEvaluator();

            // Program 1: x + 1 (Stable)
            var stableProgram = new Instruction[]
            {
                new Instruction(OpCode.PushVar, 0),
                new Instruction(OpCode.PushConst, 1.0),
                new Instruction(OpCode.Add)
            };
            var stablePruner = new IntervalPruningStrategy(stableProgram);
            
            var stableResult = stablePruner.CheckShouldEvaluateFull(evaluator, context, ds);
            Assert.True(stableResult.Passed);

            // Program 2: 1 / (x - x) -> 1 / 0 (Unstable div by zero -> Infinity)
            var unstableProgram = new Instruction[]
            {
                new Instruction(OpCode.PushConst, 1.0),
                new Instruction(OpCode.PushVar, 0),
                new Instruction(OpCode.PushVar, 0),
                new Instruction(OpCode.Sub),
                new Instruction(OpCode.Div)
            };
            var unstablePruner = new IntervalPruningStrategy(unstableProgram);

            var unstableResult = unstablePruner.CheckShouldEvaluateFull(evaluator, context, ds);
            Assert.False(unstableResult.Passed);
        }
    }
}
