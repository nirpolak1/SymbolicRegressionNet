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
                // Note: GetRawColumn expects the original dataset column name
                string colName = dataset.FeatureNames[i];
                double[] rawCol = dataset.GetRawColumn(colName);

                // Compute mean and std dev ignoring NaNs if dataset has missing values un-imputed
                var validValues = rawCol.Where(v => !double.IsNaN(v)).ToList();
                if (validValues.Count == 0)
                    throw new InvalidOperationException($"Column {colName} contains no valid finite values to fit.");

                double mean = validValues.Average();
                double variance = validValues.Average(v => Math.Pow(v - mean, 2));
                double std = Math.Sqrt(variance);

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

                for (int i = 0; i < dataset.Rows; i++)
                {
                    newCol[i] = (originalCol[i] - mean) / std;
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
