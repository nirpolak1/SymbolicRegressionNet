using Xunit;
using SymbolicRegressionNet.Sdk.Api;
using System.Collections.Generic;

namespace SymbolicRegressionNet.Sdk.Tests
{
    public class FeatureImportanceTests
    {
        [Fact]
        public void FrequencyBasedImportance_CalculatesCorrectly()
        {
            var calculator = new FrequencyBasedImportanceCalculator();
            var hof = new HallOfFame();

            // x0 is in both models (R2: 0.9 and 0.5 = 1.4 total weights)
            // x1 is in one model (R2: 0.9 = 0.9 total weights)
            // x2 is in one model (R2: 0.5 = 0.5 total weights)
            // total weight = 1.4

            var addMethod = typeof(HallOfFame).GetMethod("Add", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            addMethod.Invoke(hof, new object[] { new DiscoveredModel("sin(x0) + x1", 0.01, 0.9, 5) });
            addMethod.Invoke(hof, new object[] { new DiscoveredModel("cos(x0) * x2", 0.05, 0.5, 5) });

            var result = calculator.Calculate(hof, 3, new[] { "A", "B", "C" });

            Assert.Equal(3, result.Count);

            // x0 should have highest score (1.4 / 1.4 = 1.0)
            Assert.Equal(0, result[0].VariableIndex);
            Assert.Equal("A", result[0].VariableName);
            Assert.Equal(1.0, result[0].Score, 5);

            // x1 should have second highest score (0.9 / 1.4 = 0.64285)
            Assert.Equal(1, result[1].VariableIndex);
            Assert.Equal("B", result[1].VariableName);
            Assert.Equal(0.9 / 1.4, result[1].Score, 5);

            // x2 should have lowest score (0.5 / 1.4 = 0.35714)
            Assert.Equal(2, result[2].VariableIndex);
            Assert.Equal("C", result[2].VariableName);
            Assert.Equal(0.5 / 1.4, result[2].Score, 5);
        }

        [Fact]
        public void FrequencyBasedImportance_HandlesEmptyHallOfFame()
        {
            var calculator = new FrequencyBasedImportanceCalculator();
            var hof = new HallOfFame();

            var result = calculator.Calculate(hof, 3);
            Assert.Empty(result);
        }
    }
}
