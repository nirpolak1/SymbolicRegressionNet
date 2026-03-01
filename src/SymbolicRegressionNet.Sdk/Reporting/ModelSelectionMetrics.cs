using System;

namespace SymbolicRegressionNet.Sdk.Reporting
{
    /// <summary>
    /// Provides statistical criteria calculations for model selection on the Pareto front.
    /// </summary>
    public static class ModelSelectionMetrics
    {
        /// <summary>
        /// Calculates the Akaike Information Criterion (AIC).
        /// </summary>
        /// <param name="n">Number of observations (data rows).</param>
        /// <param name="mse">Mean Squared Error of the model.</param>
        /// <param name="k">Number of parameters (model complexity).</param>
        /// <returns>The calculated AIC score.</returns>
        public static double CalculateAic(int n, double mse, int k)
        {
            if (n <= 0 || double.IsNaN(mse) || mse < 0) return double.NaN;
            
            // To prevent -Infinity when MSE is exactly 0
            double safeMse = mse == 0.0 ? double.Epsilon : mse;
            return n * Math.Log(safeMse) + 2.0 * k;
        }

        /// <summary>
        /// Calculates the Bayesian Information Criterion (BIC).
        /// </summary>
        /// <param name="n">Number of observations (data rows).</param>
        /// <param name="mse">Mean Squared Error of the model.</param>
        /// <param name="k">Number of parameters (model complexity).</param>
        /// <returns>The calculated BIC score.</returns>
        public static double CalculateBic(int n, double mse, int k)
        {
            if (n <= 0 || double.IsNaN(mse) || mse < 0) return double.NaN;
            double safeMse = mse == 0.0 ? double.Epsilon : mse;
            return n * Math.Log(safeMse) + k * Math.Log(n);
        }
    }
}
