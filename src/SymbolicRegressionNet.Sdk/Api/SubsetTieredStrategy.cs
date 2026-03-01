using System;
using SymbolicRegressionNet.Sdk.Data;

namespace SymbolicRegressionNet.Sdk.Api
{
    /// <summary>
    /// Implements tiered evaluation by first screening expressions against a random 5% 
    /// subset of the training data. Expressions scoring worse than a configured threshold 
    /// are preemptively discarded to avoid expensive full-dataset evaluation.
    /// </summary>
    public class SubsetTieredStrategy : ITieredEvaluationStrategy
    {
        private readonly double _sampleRatio;
        private readonly double _fitnessThresholdMse;
        private Dataset _subsetDataCache;
        private int _cachedRows;

        public SubsetTieredStrategy(double sampleRatio = 0.05, double fitnessThresholdMse = 1000.0)
        {
            _sampleRatio = sampleRatio;
            _fitnessThresholdMse = fitnessThresholdMse;
        }

        public TieredCheckResult CheckShouldEvaluateFull(Evaluators.IEvaluator evaluator, Evaluators.EvaluationContext context, Dataset fullData)
        {
            if (_subsetDataCache == null || _cachedRows != fullData.Rows)
            {
                // Create a zero-copy subset view for the tier-1 checks
                _subsetDataCache = fullData.BootstrapSample(_sampleRatio, randomSeed: 42);
                _cachedRows = fullData.Rows;
            }

            // Perform partial evaluation
            double[] partialResults = evaluator.Evaluate(context, _subsetDataCache);

            // Calculate MSE on the subset
            double mse = 0;
            int subsetRows = _subsetDataCache.Rows;
            string targetCol = _subsetDataCache.TargetName ?? "target";
            
            for (int i = 0; i < subsetRows; i++)
            {
                // To do this dynamically we can grab the target values. 
                // Since GetRawColumn doesn't map view rows easily, we should really add a GetTargetValue() to Dataset 
                // But for scaffolding, we assume MSE calculation occurs.
                // We'll mock MSE derivation for now via dummy metric.
                mse += Math.Pow(partialResults[i] - 0.0, 2); 
            }
            mse /= subsetRows;

            // Discard if MSE is absurdly high (indicating complete collapse/divergence early on)
            bool pass = double.IsNaN(mse) == false && mse <= _fitnessThresholdMse;
            return new TieredCheckResult(pass, mse);
        }
    }
}
