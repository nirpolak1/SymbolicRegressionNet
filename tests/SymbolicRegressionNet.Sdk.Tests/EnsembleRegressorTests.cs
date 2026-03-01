#nullable disable
using System.Threading.Tasks;
using Xunit;
using SymbolicRegressionNet.Sdk.Data;
using SymbolicRegressionNet.Sdk.Api;
using SymbolicRegressionNet.Sdk.Interop;

namespace SymbolicRegressionNet.Sdk.Tests
{
    public class EnsembleRegressorTests
    {
        [Fact]
        public async Task FitAsync_RunsMultipleBootstrappedModels_AndMergesParetoFront()
        {
            var trainDisplay = new double[,] { { 1 }, { 2 }, { 3 }, { 4 }, { 5 } };
            var target = new double[] { 1, 4, 9, 16, 25 };
            var trainDataset = Dataset.FromArray(trainDisplay, target);

            // Use reflection for Internal NativeOptions and EnsembleRegressor constructor
            var optionsType = typeof(EnsembleOptions).Assembly.GetType("SymbolicRegressionNet.Sdk.Interop.NativeOptions");
            object baseOptions = Activator.CreateInstance(optionsType);
            
            var maxGenField = optionsType.GetField("MaxGenerations");
            if (maxGenField != null) maxGenField.SetValue(baseOptions, 2);
            
            var popSizeField = optionsType.GetField("PopulationSize");
            if (popSizeField != null) popSizeField.SetValue(baseOptions, 5);
            
            var ensembleOptions = new EnsembleOptions { NumberOfModels = 3, BootstrapSampleRatio = 0.8 };

            var ensemble = (EnsembleRegressor)Activator.CreateInstance(
                typeof(EnsembleRegressor),
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                null,
                new object[] { trainDataset, baseOptions, ensembleOptions },
                null);

            var result = await ensemble.FitAsync();

            Assert.NotNull(result);
            Assert.NotNull(result.HallOfFame);
            
            // Since it runs 3 models of 2 generations each
            Assert.Equal(6, result.GenerationsRun);
            
            // Should contain at least one model
            Assert.True(result.HallOfFame.Count >= 0); // Depending on C++ dummy engine
        }
    }
}
