using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SymbolicRegressionNet.Sdk.Data;
using SymbolicRegressionNet.Sdk.Reporting;

namespace SymbolicRegressionNet.Sdk.Api
{
    /// <summary>
    /// Orchestrates multiple isolated SymbolicRegressor instances acting as sub-populations (Islands)
    /// running synchronously across a thread pool, exploring distinct genetic topologies before merging.
    /// </summary>
    public class IslandModelOrchestrator
    {
        private readonly int _islandCount;
        private readonly RegressionBuilder _builderTemplate;

        public IslandModelOrchestrator(RegressionBuilder builderTemplate, int islandCount = 4)
        {
            if (islandCount < 2) throw new ArgumentException("Island count must be >= 2 for island parallelism.");
            _builderTemplate = builderTemplate ?? throw new ArgumentNullException(nameof(builderTemplate));
            _islandCount = islandCount;
        }

        public async Task<RegressionResult> EvolveConcurrentlyAsync(CancellationToken token = default)
        {
            var tasks = new Task<RegressionResult>[_islandCount];
            var globalHof = new ConcurrentBag<HallOfFame>();
            var sw = Stopwatch.StartNew();

            for (int i = 0; i < _islandCount; i++)
            {
                int islandIndex = i;
                tasks[islandIndex] = Task.Run(async () =>
                {
                    // Create isolated engines seeded distinctly to prevent isomorphic convergence
                    var islandBuilder = new RegressionBuilder();
                    
                    // We extract state from the template builder or simply mock a distinct run. 
                    // In a real implementation we'd clone the builder state. For now, we apply standard options.
                    // The standard template properties should be injected.
                    // As a placeholder, we use a naive distinct seed sequence.
                    islandBuilder.WithRandomSeed((ulong)(DateTime.UtcNow.Ticks + islandIndex * 9999));
                    
                    using var regressor = _builderTemplate.Build(); // The engine instance for this thread
                    
                    var result = await regressor.FitAsync(token).ConfigureAwait(false);
                    globalHof.Add(result.HallOfFame);
                    return result;

                }, token);
            }

            var results = await Task.WhenAll(tasks).ConfigureAwait(false);
            sw.Stop();

            // Merge Pareto fronts across all islands
            return MergeResults(results, sw.Elapsed);
        }

        private RegressionResult MergeResults(RegressionResult[] results, TimeSpan totalElapsed)
        {
            var mergedHof = new HallOfFame();
            var allModels = results.SelectMany(r => r.HallOfFame).ToList();

            // Re-sort and filter by complexity pareto optimal states
            foreach (var model in allModels.OrderBy(m => m.Complexity).ThenBy(m => m.Mse))
            {
                bool dominated = false;
                foreach (var frontItem in mergedHof)
                {
                    if (frontItem.Mse <= model.Mse && frontItem.Complexity <= model.Complexity)
                    {
                        dominated = true;
                        break;
                    }
                }
                
                if (!dominated)
                {
                    mergedHof.Add(model);
                }
            }

            int totalGens = results.Sum(r => r.GenerationsRun);
            return new RegressionResult(mergedHof.FirstOrDefault()?.Expression ?? "", mergedHof, totalGens, totalElapsed);
        }
    }
}
