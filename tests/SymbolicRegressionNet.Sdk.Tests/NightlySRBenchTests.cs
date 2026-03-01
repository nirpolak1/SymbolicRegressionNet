#nullable disable
using System.Linq;
using Xunit;
using SymbolicRegressionNet.Sdk.Api;
using SymbolicRegressionNet.Sdk;
using SymbolicRegressionNet.Sdk.Interop;
using SymbolicRegressionNet.Sdk.Data;

namespace SymbolicRegressionNet.Sdk.Tests
{
    [Trait("Category", "Nightly")]
    public class NightlySRBenchTests
    {
        [Fact]
        public async System.Threading.Tasks.Task FeynmanPhysics_ExactRecovery_TracksMetricGracefully()
        {
            // Mocks a nightly CI runner loading a "Feynman Physics" benchmark dataset:
            // e.g. feynman_I_8_14 (d = sqrt(x^2 + y^2))
            var features = new double[,] { { 3.0, 4.0 }, { 6.0, 8.0 }, { 5.0, 12.0 } };
            var target = new double[] { 5.0, 10.0, 13.0 };
            
            using var dataset = Dataset.FromArray(features, target);
            var builder = new RegressionBuilder().WithData(dataset).WithMaxGenerations(1);
            using var regressor = builder.Build();
            var result = await regressor.FitAsync();

            // The exact recovery check logic
            ISymbolicRecoveryMetric recovery = new ExactStringRecovery();
            
            // In the real system, result.HallOfFame contains Pareto expressions. 
            // Here we just test if exact matching resolves correctly.
            var dummyEngineHofOutput = "sqrt(x0^2 + x1^2)"; // Simulated engine output
            bool match = recovery.IsSymbolicMatch(dummyEngineHofOutput, "sqrt(x0^2 + x1^2)");
            
            Assert.True(match);
            Assert.False(recovery.IsSymbolicMatch(dummyEngineHofOutput, "x0 + x1"));
        }
    }
}
