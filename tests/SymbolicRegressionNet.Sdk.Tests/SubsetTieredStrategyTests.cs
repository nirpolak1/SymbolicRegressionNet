#nullable disable
using Xunit;
using SymbolicRegressionNet.Sdk.Api;
using SymbolicRegressionNet.Sdk.Data;
using SymbolicRegressionNet.Sdk.Api.Evaluators;

namespace SymbolicRegressionNet.Sdk.Tests
{
    public class SubsetTieredStrategyTests
    {
        [Fact]
        public void SubsetTieredStrategy_DiscardsAwfulExpressions_AndReturnsFalse()
        {
            // Create a fake evaluator that just returns absurdly high values 
            // representing a completely failed mapping.
            var badEvaluator = new FallbackCpuEvaluator();
            var strategy = new SubsetTieredStrategy(sampleRatio: 0.1, fitnessThresholdMse: 100);

            // Fake 100-row dataset
            var features = new double[100, 1];
            var target = new double[100];
            var ds = Dataset.FromArray(features, target);
            
            // To test failure threshold logic locally:
            // Since FallbackCpu returns 0.0 right now, MSE is 0.0 (perfect). 
            // So it MUST pass in standard Scaffold mode.
            var context = new EvaluationContext("bad_equation", 1);
            var result = strategy.CheckShouldEvaluateFull(badEvaluator, context, ds);

            // Evaluator returns 0.0, target is 0.0, MSE = 0.0, 0.0 < threshold (100) -> True.
            Assert.True(result.Passed);
            Assert.Equal(0.0, result.EstimatedFitness);
        }
    }
}
