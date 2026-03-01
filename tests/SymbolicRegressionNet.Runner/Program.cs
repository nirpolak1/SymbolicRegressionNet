#nullable disable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using SymbolicRegressionNet.Sdk;
using SymbolicRegressionNet.Sdk.Data;

namespace SymbolicRegressionNet.BenchmarksRunner
{
    public class RegistryEntry
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("filename")] public string Filename { get; set; }
        [JsonPropertyName("base_formula")] public string BaseFormula { get; set; }
        [JsonPropertyName("noise_level")] public double NoiseLevel { get; set; }
        [JsonPropertyName("difficulty")] public string Difficulty { get; set; }
        [JsonPropertyName("suite")] public string Suite { get; set; }
        [JsonPropertyName("variables")] public List<string> Variables { get; set; }
    }

    public class BenchmarkResult
    {
        public string Id { get; set; }
        public string Suite { get; set; }
        public string Difficulty { get; set; }
        public double NoiseLevel { get; set; }
        public string BaseFormula { get; set; }
        public string DiscoveredEquation { get; set; }
        public double R2 { get; set; }
        public double MSE { get; set; }
        public int Generations { get; set; }
        public double ElapsedSeconds { get; set; }
        public HallOfFame Front { get; set; }
        public int NumVariables { get; set; }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            bool academic = args.Any(a => a.Equals("--academic", StringComparison.OrdinalIgnoreCase));
            
            if (academic)
                await RunAcademicBenchmarks();
            else
                await RunOriginalBenchmarks();
        }

        // ─── Academic Benchmark Runner ────────────────────────────────────────
        static async Task RunAcademicBenchmarks()
        {
            Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  Academic Symbolic Regression Benchmark Suite                ║");
            Console.WriteLine("║  Nguyen · Keijzer · Vladislavleva · Korns · Pagie · Feynman ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
            Console.WriteLine();

            string datasetsDir = FindDatasetsDir("AcademicDatasets");
            string registryPath = Path.Combine(datasetsDir, "academic_registry.json");

            if (!File.Exists(registryPath))
            {
                Console.WriteLine($"ERROR: Could not find academic_registry.json at {registryPath}");
                Console.WriteLine("Please ensure the datasets directory is correctly located.");
                return;
            }

            string json = File.ReadAllText(registryPath);
            var registry = JsonSerializer.Deserialize<List<RegistryEntry>>(json);

            var results = new List<BenchmarkResult>();
            int count = 0;
            int total = registry.Count;
            var globalTimer = Stopwatch.StartNew();

            foreach (var entry in registry)
            {
                count++;
                string csvPath = Path.Combine(datasetsDir, entry.Filename);
                string suite = entry.Suite ?? entry.Difficulty ?? "Unknown";
                int numVars = entry.Variables?.Count ?? 1;
                
                Console.WriteLine($"\n[{count}/{total}] {suite}/{entry.Id}");
                Console.WriteLine($"  Formula: {entry.BaseFormula}");
                Console.WriteLine($"  Variables: {numVars}");

                try
                {
                    using var dataset = Dataset.FromCsv(csvPath, hasHeader: true).WithTarget("target");

                    var builder = new RegressionBuilder()
                        .WithData(dataset)
                        .WithPopulationSize(500)
                        .WithMaxGenerations(200)
                        .WithMaxTreeDepth(8)
                        .WithTimeLimit(TimeSpan.FromSeconds(30))
                        .WithRandomSeed(42)
                        .WithVerbose(false);

                    using var regressor = builder.Build();
                    var result = await regressor.FitAsync();

                    var bestModel = result.HallOfFame.Best;
                    double bestR2 = bestModel?.R2 ?? -1;
                    double bestMse = bestModel?.Mse ?? -1;
                    string bestEq = bestModel?.Expression ?? "N/A";

                    string grade = bestR2 >= 0.999 ? "★★★ EXACT"
                                 : bestR2 >= 0.99  ? "★★ EXCELLENT"
                                 : bestR2 >= 0.95  ? "★ GOOD"
                                 : bestR2 >= 0.80  ? "~ FAIR"
                                 : "✗ FAIL";

                    results.Add(new BenchmarkResult
                    {
                        Id = entry.Id,
                        Suite = suite,
                        Difficulty = entry.Difficulty ?? suite,
                        NoiseLevel = 0,
                        BaseFormula = entry.BaseFormula,
                        DiscoveredEquation = bestEq,
                        R2 = bestR2,
                        MSE = bestMse,
                        Generations = result.GenerationsRun,
                        ElapsedSeconds = result.ElapsedTime.TotalSeconds,
                        Front = result.HallOfFame,
                        NumVariables = numVars
                    });

                    Console.WriteLine($"  Result: R²={bestR2:F6} | MSE={bestMse:E4} | {grade}");
                    Console.WriteLine($"  Found:  {bestEq}");
                    Console.WriteLine($"  Time:   {result.ElapsedTime.TotalSeconds:F2}s | Gens: {result.GenerationsRun} | Pareto: {result.HallOfFame.Count}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  ERROR: {ex.Message}");
                    results.Add(new BenchmarkResult
                    {
                        Id = entry.Id,
                        Suite = suite,
                        Difficulty = entry.Difficulty ?? suite,
                        NoiseLevel = 0,
                        BaseFormula = entry.BaseFormula,
                        DiscoveredEquation = "ERROR: " + ex.Message,
                        R2 = -1,
                        MSE = -1,
                        Generations = 0,
                        ElapsedSeconds = 0,
                        Front = null,
                        NumVariables = numVars
                    });
                }
            }

            globalTimer.Stop();
            GenerateAcademicReport(results, globalTimer.Elapsed);
        }

        static void GenerateAcademicReport(List<BenchmarkResult> results, TimeSpan totalTime)
        {
            string reportPath = "Benchmark V0.15.md";
            using var w = new StreamWriter(reportPath);

            w.WriteLine("# Benchmark V0.15 (Academic & Standard Execution)");
            w.WriteLine();
            w.WriteLine($"**Date:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            w.WriteLine($"**Engine:** SymbolicRegressionNet V0.15");
            w.WriteLine($"**Total Problems:** {results.Count}");
            w.WriteLine($"**Total Runtime:** {totalTime.TotalMinutes:F1} minutes ({totalTime.TotalSeconds:F0}s)");
            w.WriteLine($"**Settings:** Pop=500, Gen=200, Depth=8, TimeLimit=30s/problem, Seed=42");
            w.WriteLine();

            w.WriteLine("## Overall Scorecard (SRBench Criteria)");
            w.WriteLine();

            int exact     = results.Count(r => r.R2 >= 0.999);
            int excellent = results.Count(r => r.R2 >= 0.99);
            int good      = results.Count(r => r.R2 >= 0.95);
            int fair      = results.Count(r => r.R2 >= 0.80);
            int total     = results.Count;

            w.WriteLine("| Metric | Count | Rate |");
            w.WriteLine("|--------|-------|------|");
            if (total > 0) {
                w.WriteLine($"| R² ≥ 0.999 (Exact Recovery) | {exact} / {total} | {100.0 * exact / total:F1}% |");
                w.WriteLine($"| R² ≥ 0.99 (Excellent) | {excellent} / {total} | {100.0 * excellent / total:F1}% |");
                w.WriteLine($"| R² ≥ 0.95 (Good) | {good} / {total} | {100.0 * good / total:F1}% |");
                w.WriteLine($"| R² ≥ 0.80 (Fair) | {fair} / {total} | {100.0 * fair / total:F1}% |");
                w.WriteLine($"| Mean R² (all problems) | — | {results.Average(r => Math.Max(0, r.R2)):F4} |");
                w.WriteLine($"| Median R² (all problems) | — | {Median(results.Select(r => Math.Max(0, r.R2))):F4} |");
                w.WriteLine($"| Average Time per Problem | — | {results.Average(r => r.ElapsedSeconds):F2}s |");
            }
            w.WriteLine();

            w.WriteLine("## Detailed Per-Suite Results");
            var suites = results.GroupBy(r => r.Suite).OrderBy(g => g.Key);
            foreach (var suite in suites)
            {
                var list = suite.ToList();
                int sExact = list.Count(r => r.R2 >= 0.999);
                int sGood  = list.Count(r => r.R2 >= 0.95);
                double sAvg = list.Average(r => Math.Max(0, r.R2));

                w.WriteLine($"### {suite.Key} ({list.Count} problems)");
                w.WriteLine($"**Exact (R²≥0.999):** {sExact}/{list.Count} ({100.0 * sExact / list.Count:F0}%) | **Good (R²≥0.95):** {sGood}/{list.Count} ({100.0 * sGood / list.Count:F0}%) | **Mean R²:** {sAvg:F4}");
                w.WriteLine("| Problem | Target Formula | R² | MSE | Grade | Discovered Expression | Time |");
                w.WriteLine("|---------|---------------|-----|-----|-------|-----------------------|------|");

                foreach (var r in list)
                {
                    string grade = r.R2 >= 0.999 ? "★★★ Exact"
                                 : r.R2 >= 0.99  ? "★★ Excellent"
                                 : r.R2 >= 0.95  ? "★ Good"
                                 : r.R2 >= 0.80  ? "~ Fair"
                                 : "✗ Fail";

                    string shortExpr = r.DiscoveredEquation?.Length > 60
                        ? r.DiscoveredEquation.Substring(0, 57) + "..."
                        : r.DiscoveredEquation ?? "N/A";

                    w.WriteLine($"| {r.Id} | `{r.BaseFormula}` | {r.R2:F6} | {r.MSE:E2} | {grade} | `{shortExpr}` | {r.ElapsedSeconds:F1}s |");
                }
                w.WriteLine();
            }

            Console.WriteLine($"\nAcademic Report generated: {Path.GetFullPath(reportPath)}");
        }

        static double Median(IEnumerable<double> values)
        {
            var sorted = values.OrderBy(v => v).ToList();
            if (sorted.Count == 0) return 0;
            int mid = sorted.Count / 2;
            return sorted.Count % 2 == 0 ? (sorted[mid - 1] + sorted[mid]) / 2.0 : sorted[mid];
        }

        // ─── Original Benchmark Runner ───────────────────────────
        static async Task RunOriginalBenchmarks()
        {
            Console.WriteLine("Starting SymbolicRegressionNet Benchmarks...");

            string datasetsDir = FindDatasetsDir("Datasets");
            string registryPath = Path.Combine(datasetsDir, "registry.json");

            if (!File.Exists(registryPath))
            {
                Console.WriteLine($"Could not find registry.json at {registryPath}");
                return;
            }

            string json = File.ReadAllText(registryPath);
            var registry = JsonSerializer.Deserialize<List<RegistryEntry>>(json);

            var results = new List<BenchmarkResult>();
            int count = 0;
            int total = registry.Count;

            foreach (var entry in registry)
            {
                count++;
                string csvPath = Path.Combine(datasetsDir, entry.Filename);
                Console.WriteLine($"[{count}/{total}] Benchmarking {entry.Id} ({entry.Difficulty}, Noise: {entry.NoiseLevel}) - {entry.BaseFormula}");

                using var dataset = Dataset.FromCsv(csvPath, hasHeader: true).WithTarget("target");

                var builder = new RegressionBuilder()
                    .WithData(dataset)
                    .WithPopulationSize(200)
                    .WithMaxGenerations(100)
                    .WithMaxTreeDepth(7)
                    .WithTimeLimit(TimeSpan.FromSeconds(10))
                    .WithVerbose(true, 50);

                try
                {
                    using var regressor = builder.Build();
                    var result = await regressor.FitAsync();

                    var bestModel = result.HallOfFame.Best;
                    double bestR2 = bestModel?.R2 ?? -1;
                    double bestMse = bestModel?.Mse ?? -1;
                    string bestEq = bestModel?.Expression ?? "N/A";

                    results.Add(new BenchmarkResult
                    {
                        Id = entry.Id,
                        Difficulty = entry.Difficulty,
                        NoiseLevel = entry.NoiseLevel,
                        BaseFormula = entry.BaseFormula,
                        DiscoveredEquation = bestEq,
                        R2 = bestR2,
                        MSE = bestMse,
                        Generations = result.GenerationsRun,
                        ElapsedSeconds = result.ElapsedTime.TotalSeconds,
                        Front = result.HallOfFame
                    });

                    Console.WriteLine($"        -> R2: {bestR2:F4} | Time: {result.ElapsedTime.TotalSeconds:F2}s");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"        -> Error: {ex.Message}");
                    results.Add(new BenchmarkResult
                    {
                        Id = entry.Id,
                        Difficulty = entry.Difficulty,
                        NoiseLevel = entry.NoiseLevel,
                        BaseFormula = entry.BaseFormula,
                        DiscoveredEquation = "ERROR",
                        R2 = -1,
                        MSE = -1,
                        Generations = 0,
                        ElapsedSeconds = 0,
                        Front = null
                    });
                }
            }

            GenerateOriginalReport(results);
        }

        static string FindDatasetsDir(string subDir)
        {
            string absPath = Path.GetFullPath(Path.Combine(@"c:\Nir - Personal\Repos\SymbolicRegressionNet.Benchmarks\Benchmarks", subDir));
            if (Directory.Exists(absPath))
                return absPath;

            return Path.GetFullPath(Path.Combine("Benchmarks", subDir));
        }

        static void GenerateOriginalReport(List<BenchmarkResult> results)
        {
            // We append to the V0.15 report file!
            string reportPath = "Benchmark V0.15.md";
            using var writer = new StreamWriter(reportPath, append: true);

            writer.WriteLine("---");
            writer.WriteLine("# Standard Datasets Benchmark Report");
            writer.WriteLine();
            writer.WriteLine($"**Total Datasets:** {results.Count}");
            writer.WriteLine($"**Average Time per Dataset:** {results.Average(r => r.ElapsedSeconds):F2} seconds");
            writer.WriteLine();

            writer.WriteLine("## Summary by Difficulty");
            writer.WriteLine("| Difficulty | Avg R2 | Avg R2 (No-Noise) | Success Rate (R2 > 0.95) |");
            writer.WriteLine("|------------|--------|-------------------|--------------------------| ");

            var byDiff = results.GroupBy(r => r.Difficulty);
            foreach (var group in byDiff)
            {
                double avgR2 = group.Average(r => r.R2 > 0 ? r.R2 : 0);
                double zeroNoiseR2 = group.Where(r => r.NoiseLevel == 0).Average(r => r.R2 > 0 ? r.R2 : 0);
                double successRate = group.Count(r => r.R2 > 0.95) / (double)group.Count() * 100;
                writer.WriteLine($"| {group.Key} | {avgR2:F4} | {zeroNoiseR2:F4} | {successRate:F1}% |");
            }

            Console.WriteLine($"\nOriginal Benchmark complete. Report appended to: {Path.GetFullPath(reportPath)}");
        }
    }
}
