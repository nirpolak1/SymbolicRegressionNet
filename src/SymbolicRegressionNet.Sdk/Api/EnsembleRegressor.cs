using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SymbolicRegressionNet.Sdk.Data;
using SymbolicRegressionNet.Sdk.Interop;

namespace SymbolicRegressionNet.Sdk.Api
{
    /// <summary>
    /// Executes multiple independent SymbolicRegression runs on bootstrapped dataset samples
    /// and merges their Pareto fronts into a single global front.
    /// </summary>
    public sealed class EnsembleRegressor
    {
        private readonly Dataset _trainDataset;
        private readonly NativeOptions _baseOptions;
        private readonly EnsembleOptions _ensembleOptions;

        internal EnsembleRegressor(Dataset trainDataset, NativeOptions baseOptions, EnsembleOptions ensembleOptions)
        {
            _trainDataset = trainDataset ?? throw new ArgumentNullException(nameof(trainDataset));
            _baseOptions = baseOptions;
            _ensembleOptions = ensembleOptions;
        }

        /// <summary>
        /// Runs the ensemble regression sequentially.
        /// (Can be parallelized in future updates).
        /// </summary>
        public async Task<RegressionResult> FitAsync(CancellationToken cancellationToken = default)
        {
            var allDiscovered = new List<DiscoveredModel>();
            int totalGenerations = 0;
            var startTime = DateTime.UtcNow;

            for (int i = 0; i < _ensembleOptions.NumberOfModels; i++)
            {
                if (cancellationToken.IsCancellationRequested) break;

                // Bootstrap the dataset
                // Add seed iteration for determinism if original had seed, else random.
                int seed = (int)(_baseOptions.RandomSeed + (ulong)i);
                using var sample = _trainDataset.BootstrapSample(_ensembleOptions.BootstrapSampleRatio, seed);

                // Initialize a new regressor
                var options = _baseOptions;
                options.RandomSeed = (ulong)seed;

                using var regressor = new SymbolicRegressor(sample, null, options, null);
                
                var result = await regressor.FitAsync(cancellationToken);
                
                totalGenerations += result.GenerationsRun;
                allDiscovered.AddRange(result.HallOfFame);
            }

            // Merge HallOfFame by Pareto dominance (R2 vs Complexity)
            var mergedHof = MergeParetoFront(allDiscovered);

            return new RegressionResult(
                mergedHof.Count > 0 ? mergedHof.Best.Expression : string.Empty,
                mergedHof,
                totalGenerations,
                DateTime.UtcNow - startTime
            );
        }

        private HallOfFame MergeParetoFront(List<DiscoveredModel> models)
        {
            // Simple pareto merge: group by complexity, pick best R2 for each complexity
            var bestByComplexity = models
                .GroupBy(m => m.Complexity)
                .Select(g => g.OrderByDescending(x => x.R2).First())
                .OrderBy(m => m.Complexity)
                .ToList();

            var hof = new HallOfFame();
            
            // Only add models strictly better than all simpler models
            double maxR2SoFar = double.NegativeInfinity;
            foreach (var model in bestByComplexity)
            {
                if (model.R2 > maxR2SoFar)
                {
                    // Use reflection/internal method to add, since Add is internal.
                    // But here we are inside SymbolicRegressionNet.Sdk, so Add is available.
                    hof.Add(model);
                    maxR2SoFar = model.R2;
                }
            }

            return hof;
        }
    }
}
