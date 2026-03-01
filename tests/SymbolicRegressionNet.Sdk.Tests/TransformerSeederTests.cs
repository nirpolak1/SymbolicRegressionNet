using System.Collections.Generic;
using System.Linq;
using Xunit;
using SymbolicRegressionNet.Sdk.Api.Seeding;
using SymbolicRegressionNet.Sdk.Data;

namespace SymbolicRegressionNet.Sdk.Tests
{
    public class TransformerSeederTests
    {
        [Fact]
        public void TransformerSeeder_GeneratesExpectedPriors()
        {
            var ds = Dataset.FromArray(new double[,] { { 1, 2 } }, new double[] { 3 });
            var seeder = new TransformerSeeder();

            var seeds = seeder.GenerateSeeds(ds, 3);
            
            Assert.Equal(3, seeds.Count);
            Assert.Contains("x0 + x1", seeds);
            Assert.Contains("sin(x0) * c", seeds);
            Assert.Contains("(x0 / x1) + c", seeds);
        }
    }
}
