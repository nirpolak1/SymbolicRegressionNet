#nullable disable
using System.Threading.Tasks;
using Xunit;
using SymbolicRegressionNet.Sdk;
using SymbolicRegressionNet.Sdk.Api;
using SymbolicRegressionNet.Sdk.Data;

namespace SymbolicRegressionNet.Sdk.Tests
{
    public class IslandModelTests
    {
        [Fact]
        public async Task IslandModelOrchestrator_RunsMultipleEngines_AndMergesPareto()
        {
            var features = new double[,] { { 1.0 }, { 2.0 } };
            using var dataset = Dataset.FromArray(features, new double[] { 1.0, 4.0 });

            // Create template
            var builder = new RegressionBuilder()
                .WithData(dataset)
                .WithMaxGenerations(1) // Keep fast for test
                .WithPopulationSize(10);
            
            // Run 3 islands
            var orchestrator = new IslandModelOrchestrator(builder, islandCount: 3);

            var combinedResult = await orchestrator.EvolveConcurrentlyAsync();

            // Total generations should be 1 gen * 3 islands = 3
            Assert.Equal(3, combinedResult.GenerationsRun);
            // The unified Pareto front should be correctly instantiated and populated
            Assert.NotNull(combinedResult.HallOfFame);
        }
    }
}
