using System;
using Xunit;
using SymbolicRegressionNet.Sdk.Reporting;

namespace SymbolicRegressionNet.Sdk.Tests
{
    public class ModelSelectionMetricsTests
    {
        [Fact]
        public void CalculateAic_ComputesCorrectly()
        {
            int n = 100;
            double mse = 0.5;
            int k = 3;

            double result = ModelSelectionMetrics.CalculateAic(n, mse, k);
            double expected = n * Math.Log(mse) + 2.0 * k;

            Assert.Equal(expected, result, 5);
        }

        [Fact]
        public void CalculateBic_ComputesCorrectly()
        {
            int n = 100;
            double mse = 0.5;
            int k = 3;

            double result = ModelSelectionMetrics.CalculateBic(n, mse, k);
            double expected = n * Math.Log(mse) + k * Math.Log(n);

            Assert.Equal(expected, result, 5);
        }

        [Fact]
        public void Calculate_HandlesZeroMseGracefully()
        {
            int n = 10;
            double mse = 0.0;
            int k = 1;

            double aic = ModelSelectionMetrics.CalculateAic(n, mse, k);
            double bic = ModelSelectionMetrics.CalculateBic(n, mse, k);

            Assert.NotEqual(double.NegativeInfinity, aic);
            Assert.NotEqual(double.NegativeInfinity, bic);
        }
    }
}
