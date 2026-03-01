using System;
using System.Threading.Tasks;
using SymbolicRegressionNet.Sdk.Data;

namespace SymbolicRegressionNet.Sdk.Api.Evaluators
{
    /// <summary>
    /// A slow but robust fallback CPU-evaluator using standard C# scalar loops.
    /// Used as a reference implementation or fallback when SIMD/ILGPU are unavailable.
    /// </summary>
    public sealed class FallbackCpuEvaluator : IEvaluator
    {
        public string EngineName => "C# Scalar CPU";

        public double[] Evaluate(EvaluationContext context, Dataset testData)
        {
            if (string.IsNullOrWhiteSpace(context.ExpressionData))
                throw new ArgumentException("Empty expression.", nameof(context));

            double[] results = new double[testData.Rows];
            
            // For architecture scaffolding, we mock evaluation as 0.0s. 
            // Real implementation would parse context.ExpressionData into a scalar tree and run.
            for (int i = 0; i < testData.Rows; i++)
            {
                results[i] = 0.0; 
            }

            return results;
        }

        public Task<double[]> EvaluateAsync(EvaluationContext context, Dataset testData)
        {
            return Task.Run(() => Evaluate(context, testData));
        }
    }
}
