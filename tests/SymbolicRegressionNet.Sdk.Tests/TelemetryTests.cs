#nullable disable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using SymbolicRegressionNet.Sdk.Data;

namespace SymbolicRegressionNet.Sdk.Tests
{
    public class TelemetryTests
    {
        [Fact]
        public async Task FitAsync_ReportsTelemetryWithTimingMetrics()
        {
            // Use reflection since NativeOptions and the SymbolicRegressor construct are internal
            var optionsType = typeof(SymbolicRegressor).Assembly.GetType("SymbolicRegressionNet.Sdk.Interop.NativeOptions");
            object options = Activator.CreateInstance(optionsType);
            
            // Set fields dynamically to prevent C++ engine access violations from zero-initialized values
            var maxGenField = optionsType.GetField("MaxGenerations");
            if (maxGenField != null) maxGenField.SetValue(options, 5);

            var popSizeField = optionsType.GetField("PopulationSize");
            if (popSizeField != null) popSizeField.SetValue(options, 10);

            var mutRateField = optionsType.GetField("MutationRate");
            if (mutRateField != null) mutRateField.SetValue(options, 0.1);

            var crossRateField = optionsType.GetField("CrossoverRate");
            if (crossRateField != null) crossRateField.SetValue(options, 0.7);

            var train = Dataset.FromArray(new double[,] { { 1 }, { 2 } }, new double[] { 1, 2 });
            
            using var regressor = (SymbolicRegressor)Activator.CreateInstance(
                typeof(SymbolicRegressor),
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                null,
                new object[] { train, null, options, null },
                null);
            
            var reports = new List<GenerationReport>();
            
            // AddActionHandler executes synchronously before moving to the next generation
            regressor.AddActionHandler(report => reports.Add(report));

            await regressor.FitAsync();
            
            Assert.Equal(5, reports.Count);
            
            // Check that telemetry has timing populated
            foreach (var report in reports)
            {
                Assert.True(report.EvaluationsPerSecond >= 0);
                Assert.NotNull(report.EstimatedTimeRemaining);
                Assert.True(report.EstimatedTimeRemaining.Value.TotalSeconds >= 0);
            }
        }
    }
}
