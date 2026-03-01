using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using SymbolicRegressionNet.Sdk;
using SymbolicRegressionNet.Sdk.Data;
using SymbolicRegressionNet.Sdk.Interop;
using System.Threading.Tasks;

namespace SymbolicRegressionNet.Benchmarks
{
    [MemoryDiagnoser]
    public class RegressionBenchmark
    {
        private Dataset _trainDataset;
        private object _options;

        [GlobalSetup]
        public void Setup()
        {
            // Simulate a medium-sized random dataset (e.g. 100 rows, 5 features)
            double[,] x = new double[100, 5];
            double[] y = new double[100];
            var rng = new System.Random(42);
            for (int i = 0; i < 100; i++)
            {
                y[i] = rng.NextDouble();
                for (int j = 0; j < 5; j++) x[i, j] = rng.NextDouble();
            }

            _trainDataset = Dataset.FromArray(x, y);

            // Access internal NativeOptions struct via Reflection or if public.
            // Since this is a separate assembly, we use reflection to populate standard benchmark settings
            var optionsType = typeof(SymbolicRegressor).Assembly.GetType("SymbolicRegressionNet.Sdk.Interop.NativeOptions");
            object options = System.Activator.CreateInstance(optionsType);
            
            // Fast run for benchmark (e.g., benchmark overhead of engine creation and stepping 20 generations)
            optionsType.GetField("MaxGenerations").SetValue(options, 20);
            optionsType.GetField("PopulationSize").SetValue(options, 50);

            _options = options; // Keep as object for reflection
        }

        [Benchmark]
        public async Task RunRegressionEndToEnd()
        {
            // Use Reflection to hit internal constructor, or mock access if internals aren't visible
            var regressor = (SymbolicRegressor)System.Activator.CreateInstance(
                typeof(SymbolicRegressor),
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                null,
                new object[] { _trainDataset, null, _options, null },
                null);

            await regressor.FitAsync();
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<RegressionBenchmark>();
        }
    }
}
