using System;
using System.Linq;
using Xunit;
using SymbolicRegressionNet.Sdk.Data;

namespace SymbolicRegressionNet.Sdk.Tests
{
    public class DataScalerTests
    {
        [Fact]
        public void StandardScaler_FitsAndTransformsCorrectly()
        {
            // Train Data: x0 = { 10, 20, 30 }
            // Mean = 20, Variance = ((10-20)^2 + (20-20)^2 + (30-20)^2)/3 = 200 / 3 = 66.666
            // StdDev = Math.Sqrt(66.666) ≈ 8.1649658
            var trainDisplay = new double[,] { { 10 }, { 20 }, { 30 } };
            var target = new double[] { 1, 2, 3 };
            var trainDataset = Dataset.FromArray(trainDisplay, target);

            var scaler = new StandardScaler();
            var scaledTrain = scaler.FitTransform(trainDataset);

            Assert.True(scaler.IsFitted);
            
            double[] scaledX0 = scaledTrain.GetRawColumn("x0");
            Assert.Equal(3, scaledX0.Length);
            
            // Expected values
            double std = Math.Sqrt(200.0 / 3.0);
            Assert.Equal((10 - 20) / std, scaledX0[0], 5);
            Assert.Equal((20 - 20) / std, scaledX0[1], 5);
            Assert.Equal((30 - 20) / std, scaledX0[2], 5);

            // Test Data: x0 = { 40, 50 }
            var testDisplay = new double[,] { { 40 }, { 50 } };
            var testTarget = new double[] { 4, 5 };
            var testDataset = Dataset.FromArray(testDisplay, testTarget);

            // Should use TRAIN mean (20) and std (~8.16)
            var scaledTest = testDataset.Scale(scaler);
            double[] scaledTestX0 = scaledTest.GetRawColumn("x0");
            
            Assert.Equal(2, scaledTestX0.Length);
            Assert.Equal((40 - 20) / std, scaledTestX0[0], 5);
            Assert.Equal((50 - 20) / std, scaledTestX0[1], 5);
        }

        [Fact]
        public void StandardScaler_ZeroVariance_AvoidsNaNs()
        {
            var trainDisplay = new double[,] { { 5 }, { 5 }, { 5 } };
            var trainDataset = Dataset.FromArray(trainDisplay, null);

            var scaler = new StandardScaler();
            var scaled = scaler.FitTransform(trainDataset);

            double[] scaledX0 = scaled.GetRawColumn("x0");
            Assert.All(scaledX0, val => Assert.Equal(0, val)); // (5 - 5) / 1.0
        }
    }
}
