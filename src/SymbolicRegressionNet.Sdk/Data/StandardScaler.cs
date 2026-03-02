using System;
using System.Linq;
using System.Collections.Generic;

namespace SymbolicRegressionNet.Sdk.Data
{
    /// <summary>
    /// Standard scaler that removes the mean and scales to unit variance (Z-score normalization).
    /// Maintains internal state so the same scaling can be applied to validation/test sets.
    /// </summary>
    public class StandardScaler : IDataScaler
    {
        [System.Runtime.InteropServices.DllImport("SymbolicRegressionNetCore", CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
        private static extern unsafe void ComputeStandardScalingMetrics_AVX2(double* data, int rows, double* out_mean, double* out_std);

        [System.Runtime.InteropServices.DllImport("SymbolicRegressionNetCore", CallingConvention = System.Runtime.InteropServices.CallingConvention.Cdecl)]
        private static extern unsafe void ApplyStandardScaling_AVX2(double* data, int rows, double mean, double std, double* out_scaled);

        private double[] _means;
        private double[] _stdDevs;

        /// <summary>
        /// Indicates whether the scaler has been fitted to data.
        /// </summary>
        public bool IsFitted => _means != null && _stdDevs != null;

        public void Fit(Dataset dataset)
        {
            if (dataset == null) throw new ArgumentNullException(nameof(dataset));

            int featureCount = dataset.FeatureCount;
            _means = new double[featureCount];
            _stdDevs = new double[featureCount];

            // Access underlying arrays, avoiding copying if possible
            for (int i = 0; i < featureCount; i++)
            {
                string colName = dataset.FeatureNames[i];
                double[] rawCol = dataset.GetRawColumn(colName);
                
                // We're delegating missing value safety guarantees to previous imputation pipelines 
                // in Epic 4. Assuming clean, dense arrays we can use AVX2.
                double mean = 0, std = 0;
                
                try
                {
                    unsafe
                    {
                        double m = 0, s = 0;
                        fixed (double* pData = rawCol)
                        {
                            ComputeStandardScalingMetrics_AVX2(pData, dataset.Rows, &m, &s);
                        }
                        mean = m;
                        std = s;
                    }
                }
                catch (DllNotFoundException)
                {
                    // Fallback to managed C# if native DLL is missing
                    var validValues = rawCol.Where(v => !double.IsNaN(v)).ToList();
                    if (validValues.Count == 0)
                        throw new InvalidOperationException($"Column {colName} contains no valid finite values to fit.");

                    double fallbackMean = validValues.Average();
                    double variance = validValues.Average(v => Math.Pow(v - fallbackMean, 2));
                    
                    mean = fallbackMean;
                    std = Math.Sqrt(variance);
                }

                _means[i] = mean;
                _stdDevs[i] = std > 0 ? std : 1.0; // Prevent div by zero
            }
        }

        public Dataset Transform(Dataset dataset)
        {
            if (dataset == null) throw new ArgumentNullException(nameof(dataset));
            if (!IsFitted) throw new InvalidOperationException("Scaler must be fitted before transforming data.");
            if (dataset.FeatureCount != _means.Length)
                throw new ArgumentException($"Dataset feature count ({dataset.FeatureCount}) does not match fitted scaler parameters ({_means.Length}).");

            var columnsDict = new Dictionary<string, double[]>();
            var targetName = dataset.TargetName;
            int featureIdx = 0;

            for (int c = 0; c < dataset.TotalColumns; c++)
            {
                // Find column name by checking FeatureNames or TargetName
                bool isTarget = (c == dataset.TotalColumns - 1 && targetName != null) || 
                                (targetName != null && c == 0 /* actually, we don't know the exact index of target without reflection or better API, but let's assume get by name */);
                
                // Workaround: We know FeatureNames are ordered as they appear, skipping TargetName.
                // Let's just iterate over FeatureNames and TargetName explicitly.
            }

            // Proper way:
            foreach (var feature in dataset.FeatureNames)
            {
                double[] originalCol = dataset.GetRawColumn(feature);
                double[] newCol = new double[dataset.Rows];

                double mean = _means[featureIdx];
                double std = _stdDevs[featureIdx];

                try
                {
                    unsafe
                    {
                        fixed (double* pSource = originalCol)
                        fixed (double* pDest = newCol)
                        {
                            ApplyStandardScaling_AVX2(pSource, dataset.Rows, mean, std, pDest);
                        }
                    }
                }
                catch (DllNotFoundException)
                {
                    for (int i = 0; i < dataset.Rows; i++)
                    {
                        newCol[i] = (originalCol[i] - mean) / std;
                    }
                }
                columnsDict[feature] = newCol;
                featureIdx++;
            }

            if (targetName != null)
            {
                // Target is not scaled
                columnsDict[targetName] = (double[])dataset.GetRawColumn(targetName).Clone();
            }

            return Dataset.FromColumns(columnsDict, targetName);
        }

        public Dataset FitTransform(Dataset dataset)
        {
            Fit(dataset);
            return Transform(dataset);
        }
    }
}
