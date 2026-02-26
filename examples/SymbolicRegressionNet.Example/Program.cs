using System;
using System.IO;
using System.Threading.Tasks;
using SymbolicRegressionNet.Sdk;
using SymbolicRegressionNet.Sdk.Data;

namespace SymbolicRegressionNet.Example
{
    class Program
    {
        static async Task Main()
        {
            Console.WriteLine("==================================================");
            Console.WriteLine(" SymbolicRegressionNet - Simple SDK Example ");
            Console.WriteLine("==================================================\n");

            // 1. Prepare your Dataset CSV file.
            // The file should be a standard comma-separated values file:
            // 
            // x, y, target
            // 1.0, 2.0, 5.0
            // 2.0, 3.0, 13.0
            // 3.0, 4.0, 25.0
            // 
            // Optional: The first row can contain header names (like 'x', 'y' and 'target').
            // Every other row should contain numeric floating point values. 

            string csvPath = "path/to/your/dataset.csv";
            
            if (!File.Exists(csvPath))
            {
                Console.WriteLine($"[ERROR] Provide a valid CSV file at: {csvPath}");
                Console.WriteLine("Please update 'csvPath' in Program.cs and run again.");
                return;
            }

            Console.WriteLine($"[1] Loading dataset from: {csvPath}");

            // 2. Load the Dataset
            // By default, `FromCsv` assumes the first row is a header.
            // We specify that our dependent variable (what we want to predict) is named "target".
            using var dataset = Dataset.FromCsv(csvPath, hasHeader: true)
                                       .WithTarget("target");

            Console.WriteLine($"[2] Dataset loaded: {dataset.FeatureCount} features and {dataset.Rows} rows.");

            // 3. Configure the Engine
            var builder = new RegressionBuilder()
                .WithData(dataset)
                .WithPopulationSize(500)
                .WithMaxGenerations(100)
                .WithMaxTreeDepth(6)
                .WithTimeLimit(TimeSpan.FromSeconds(10))
                .WithVerbose(true, 25); // Print progress every 25 generations

            Console.WriteLine("[3] Configured the RegressionBuilder. Starting Evolution...\n");

            // 4. Run the Evolution!
            using var regressor = builder.Build();
            var result = await regressor.FitAsync();

            Console.WriteLine("\n==================================================");
            Console.WriteLine(" Evolution Complete!");
            Console.WriteLine("==================================================");

            // 5. Evaluate the Results
            var bestModel = result.HallOfFame.Best;
            
            if (bestModel != null)
            {
                Console.WriteLine($"\n[Result] Best Formula Found:");
                Console.WriteLine($"  Expression: {bestModel.Expression}");
                Console.WriteLine($"  Accuracy (R²): {bestModel.R2:F6}");
                Console.WriteLine($"  Error (MSE): {bestModel.Mse:E4}");
                Console.WriteLine($"  Complexity: {bestModel.Complexity}");
            }
            else
            {
                Console.WriteLine("\n[Result] No valid models were found within the time limit.");
            }
            
            Console.WriteLine($"\nStats: {result.GenerationsRun} generations ran in {result.ElapsedTime.TotalSeconds:F2} seconds.");
        }
    }
}
