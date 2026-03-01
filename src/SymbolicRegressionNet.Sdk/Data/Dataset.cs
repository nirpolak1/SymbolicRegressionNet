using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using SymbolicRegressionNet.Sdk.Interop;

namespace SymbolicRegressionNet.Sdk.Data
{
    /// <summary>
    /// Represents a dataset for symbolic regression. Supports zero-copy views,
    /// CSV loading, normalization, and pinning for native interop.
    /// </summary>
    public sealed class Dataset : IDisposable
    {
        private readonly double[][] _columns;
        private readonly string[] _columnNames;
        private readonly int[] _rowIndices;
        private readonly int _targetIndex;

        /// <summary>
        /// Number of rows in this dataset (or view).
        /// </summary>
        public int Rows => _rowIndices?.Length ?? _columns[0].Length;

        /// <summary>
        /// Total number of variables (features + target).
        /// </summary>
        public int TotalColumns => _columns.Length;

        /// <summary>
        /// Number of feature columns (excludes the target, if any).
        /// </summary>
        public int FeatureCount => _targetIndex >= 0 ? TotalColumns - 1 : TotalColumns;

        /// <summary>
        /// Names of the feature columns.
        /// </summary>
        public IReadOnlyList<string> FeatureNames
        {
            get
            {
                var names = new List<string>(FeatureCount);
                for (int i = 0; i < TotalColumns; i++)
                {
                    if (i != _targetIndex)
                    {
                        names.Add(_columnNames[i]);
                    }
                }
                return names;
            }
        }

        /// <summary>
        /// Name of the target column, or null if no target is set.
        /// </summary>
        public string TargetName => _targetIndex >= 0 ? _columnNames[_targetIndex] : null;

        /// <summary>
        /// Gets a feature value for a specific row index, mapping virtual view indices transparently.
        /// </summary>
        public double GetFeatureValue(int row, int featureIndex)
        {
            int realRow = _rowIndices != null ? _rowIndices[row] : row;
            int colIndex = (_targetIndex >= 0 && featureIndex >= _targetIndex) ? featureIndex + 1 : featureIndex;
            return _columns[colIndex][realRow];
        }

        /// <summary>
        /// Gets the target value for a specific row index, mapping virtual view indices transparently.
        /// </summary>
        public double GetTargetValue(int row)
        {
            if (_targetIndex < 0) throw new InvalidOperationException("Dataset has no target column specified.");
            int realRow = _rowIndices != null ? _rowIndices[row] : row;
            return _columns[_targetIndex][realRow];
        }

        internal Dataset(double[][] columns, string[] columnNames, int[] rowIndices, int targetIndex = -1)
        {
            _columns = columns;
            _columnNames = columnNames;
            _rowIndices = rowIndices;
            _targetIndex = targetIndex;
        }

        // ─── Factory Methods ───

        /// <summary>
        /// Loads a dataset from a structured CSV file.
        /// </summary>
        public static Dataset FromCsv(string filePath, bool hasHeader = true, MissingValueStrategy missingStrategy = MissingValueStrategy.ThrowOnMissing)
        {
            var lines = File.ReadAllLines(filePath);
            if (lines.Length == 0) throw new ArgumentException("CSV file is empty.");

            char[] delimiters = { ',', '\t', ';' };
            
            int startRow = hasHeader ? 1 : 0;
            if (startRow >= lines.Length) throw new ArgumentException("CSV file has no data rows.");

            string[] headerTokens = lines[0].Split(delimiters);
            int colCount = headerTokens.Length;
            string[] columnNames = new string[colCount];

            if (hasHeader)
            {
                for (int i = 0; i < colCount; i++) columnNames[i] = headerTokens[i].Trim();
            }
            else
            {
                for (int i = 0; i < colCount; i++) columnNames[i] = $"x{i}";
                headerTokens = lines[0].Split(delimiters);
            }

            int dataRowCount = lines.Length - startRow;
            var columns = new List<double>[colCount];
            for (int i = 0; i < colCount; i++) columns[i] = new List<double>(dataRowCount);

            for (int r = startRow; r < lines.Length; r++)
            {
                var rowStr = lines[r];
                if (string.IsNullOrWhiteSpace(rowStr)) continue;

                var cells = rowStr.Split(delimiters);
                bool dropRow = false;
                double[] parsedRow = new double[colCount];

                for (int c = 0; c < colCount; c++)
                {
                    string cell = c < cells.Length ? cells[c].Trim() : "";

                    if (string.IsNullOrEmpty(cell) || cell.Equals("NA", StringComparison.OrdinalIgnoreCase) || cell.Equals("NaN", StringComparison.OrdinalIgnoreCase))
                    {
                        if (missingStrategy == MissingValueStrategy.ThrowOnMissing)
                            throw new FormatException($"Missing value at row {r}, column {c}");
                        if (missingStrategy == MissingValueStrategy.DropRow)
                        {
                            dropRow = true;
                            break;
                        }
                        parsedRow[c] = double.NaN; // To be replaced later
                    }
                    else if (double.TryParse(cell, NumberStyles.Any, CultureInfo.InvariantCulture, out double val))
                    {
                        parsedRow[c] = val;
                    }
                    else
                    {
                        throw new FormatException($"Non-numeric value '{cell}' at row {r}, column {c}");
                    }
                }

                if (!dropRow)
                {
                    for (int c = 0; c < colCount; c++)
                    {
                        columns[c].Add(parsedRow[c]);
                    }
                }
            }

            double[][] finalColumns = new double[colCount][];
            for (int c = 0; c < colCount; c++)
            {
                finalColumns[c] = columns[c].ToArray();
                HandleMissingValues(finalColumns[c], missingStrategy);
            }

            return new Dataset(finalColumns, columnNames, null);
        }

        private static void HandleMissingValues(double[] column, MissingValueStrategy strategy)
        {
            if (strategy == MissingValueStrategy.DropRow || strategy == MissingValueStrategy.ThrowOnMissing)
                return;

            var validValues = column.Where(v => !double.IsNaN(v)).ToList();
            if (validValues.Count == 0) return;

            double replacement = 0;
            if (strategy == MissingValueStrategy.ReplaceWithMean)
                replacement = validValues.Average();
            else if (strategy == MissingValueStrategy.ReplaceWithMedian)
            {
                validValues.Sort();
                replacement = validValues[validValues.Count / 2];
            }

            for (int i = 0; i < column.Length; i++)
            {
                if (double.IsNaN(column[i])) column[i] = replacement;
            }
        }

        /// <summary>
        /// Creates a dataset from a row-major 2D feature array and a target array.
        /// </summary>
        public static Dataset FromArray(double[,] features, double[] target)
        {
            int rows = features.GetLength(0);
            int cols = features.GetLength(1);

            if (target != null && target.Length != rows)
                throw new ArgumentException("Target length must match features row count.");

            int totalCols = target != null ? cols + 1 : cols;
            double[][] columns = new double[totalCols][];
            string[] names = new string[totalCols];

            for (int c = 0; c < cols; c++)
            {
                columns[c] = new double[rows];
                names[c] = $"x{c}";
                for (int r = 0; r < rows; r++)
                {
                    columns[c][r] = features[r, c];
                }
            }

            int targetIndex = -1;
            if (target != null)
            {
                targetIndex = cols;
                columns[targetIndex] = (double[])target.Clone();
                names[targetIndex] = "target";
            }

            return new Dataset(columns, names, null, targetIndex);
        }

        /// <summary>
        /// Creates a dataset from a dictionary of column arrays.
        /// </summary>
        public static Dataset FromColumns(IReadOnlyDictionary<string, double[]> columnsDict, string targetColumn)
        {
            int colCount = columnsDict.Count;
            if (colCount == 0) throw new ArgumentException("Dictionary is empty.");

            double[][] columns = new double[colCount][];
            string[] names = new string[colCount];

            int rows = -1;
            int idx = 0;
            int targetIndex = -1;

            foreach (var kvp in columnsDict)
            {
                if (rows == -1) rows = kvp.Value.Length;
                else if (kvp.Value.Length != rows) throw new ArgumentException("All columns must have the same length.");

                columns[idx] = (double[])kvp.Value.Clone();
                names[idx] = kvp.Key;

                if (kvp.Key == targetColumn) targetIndex = idx;
                idx++;
            }

            if (targetColumn != null && targetIndex == -1)
                throw new ArgumentException($"Target column '{targetColumn}' not found in dictionary.");

            return new Dataset(columns, names, null, targetIndex);
        }

        // ─── Fluent Configuration ───

        /// <summary>
        /// Sets the target column by name.
        /// </summary>
        public Dataset WithTarget(string columnName)
        {
            int idx = Array.IndexOf(_columnNames, columnName);
            if (idx < 0) throw new ArgumentException($"Column '{columnName}' not found.");
            return new Dataset(_columns, _columnNames, _rowIndices, idx);
        }

        /// <summary>
        /// Drops specified columns and returns a new view.
        /// </summary>
        public Dataset Drop(params string[] columnNames)
        {
            var namesToDrop = new HashSet<string>(columnNames);
            int newColCount = _columnNames.Count(n => !namesToDrop.Contains(n));

            double[][] newColumns = new double[newColCount][];
            string[] newNames = new string[newColCount];

            int idx = 0;
            int newTargetIdx = -1;
            for (int i = 0; i < TotalColumns; i++)
            {
                if (!namesToDrop.Contains(_columnNames[i]))
                {
                    newColumns[idx] = _columns[i];
                    newNames[idx] = _columnNames[i];
                    if (i == _targetIndex) newTargetIdx = idx;
                    idx++;
                }
            }

            return new Dataset(newColumns, newNames, _rowIndices, newTargetIdx);
        }

        /// <summary>
        /// Scales this dataset using the specified stateful data scaler.
        /// Useful for applying the same scaling parameters to both training and test datasets.
        /// </summary>
        public Dataset Scale(IDataScaler scaler)
        {
            if (scaler == null) throw new ArgumentNullException(nameof(scaler));
            // If the scaler hasn't been fitted yet, we assume the user wants to fit it on THIS dataset.
            var standardScaler = scaler as StandardScaler;
            if (standardScaler != null && !standardScaler.IsFitted)
            {
                return scaler.FitTransform(this);
            }
            return scaler.Transform(this);
        }

        /// <summary>
        /// Normalizes feature columns in place, returning a new view (or modifying existing if not a view).
        /// To strictly follow zero-copy over original, standard practice might copy or modify in place.
        /// We clone columns that are modified.
        /// </summary>
        public Dataset Normalize(NormalizationMethod method = NormalizationMethod.ZScore)
        {
            double[][] newCols = new double[TotalColumns][];
            for (int c = 0; c < TotalColumns; c++)
            {
                if (c == _targetIndex) 
                {
                    newCols[c] = _columns[c];
                    continue;
                }

                double[] colData = MaterializeColumn(c);
                if (method == NormalizationMethod.ZScore)
                {
                    double mean = colData.Average();
                    double stdDev = Math.Sqrt(colData.Average(v => Math.Pow(v - mean, 2)));
                    if (stdDev > 0)
                    {
                        for (int i = 0; i < colData.Length; i++) colData[i] = (colData[i] - mean) / stdDev;
                    }
                }
                else if (method == NormalizationMethod.MinMax)
                {
                    double min = colData.Min();
                    double max = colData.Max();
                    double range = max - min;
                    if (range > 0)
                    {
                        for (int i = 0; i < colData.Length; i++) colData[i] = (colData[i] - min) / range;
                    }
                }
                newCols[c] = colData;
            }

            return new Dataset(newCols, _columnNames, null, _targetIndex); // Normalization materializes view
        }

        private double[] MaterializeColumn(int colIndex)
        {
            if (_rowIndices == null) return (double[])_columns[colIndex].Clone();
            
            double[] arr = new double[_rowIndices.Length];
            for(int i = 0; i < _rowIndices.Length; i++)
            {
                arr[i] = _columns[colIndex][_rowIndices[i]];
            }
            return arr;
        }

        /// <summary>
        /// Creates a zero-copy dataset view over specified row indices.
        /// </summary>
        internal Dataset CreateView(int[] rowIndices)
        {
            return new Dataset(_columns, _columnNames, rowIndices, _targetIndex);
        }

        /// <summary>
        /// Creates a bootstrap sample of the dataset by randomly sampling rows with replacement.
        /// This creates a lightweight, zero-copy view over the same underlying arrays.
        /// </summary>
        /// <param name="sampleRatio">Ratio of rows to sample. 1.0 = sample exactly N rows.</param>
        /// <param name="randomSeed">Optional seed for deterministic sampling.</param>
        public Dataset BootstrapSample(double sampleRatio = 1.0, int? randomSeed = null)
        {
            if (sampleRatio <= 0) throw new ArgumentOutOfRangeException(nameof(sampleRatio));

            int totalRows = Rows;
            int numSamples = (int)Math.Max(1, Math.Round(totalRows * sampleRatio));

            var rng = randomSeed.HasValue ? new Random(randomSeed.Value) : new Random();
            int[] newIndices = new int[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                // If this dataset is ALREADY a view, we need to pick from the current view's indices
                int randomVirtualIdx = rng.Next(totalRows);
                newIndices[i] = _rowIndices != null ? _rowIndices[randomVirtualIdx] : randomVirtualIdx;
            }

            return CreateView(newIndices);
        }

        // ─── Interop ───

        /// <summary>
        /// Pins the dataset arrays in memory for native C++ consumption.
        /// Unpins upon disposal.
        /// </summary>
        public PinnedData Pin()
        {
            PinnedBuffer<double>[] featureBuffers = new PinnedBuffer<double>[FeatureCount];
            PinnedBuffer<double> targetBuffer = null;

            int fIdx = 0;
            for (int i = 0; i < TotalColumns; i++)
            {
                double[] arrayToPin = _rowIndices == null ? _columns[i] : MaterializeColumn(i);

                if (i == _targetIndex)
                {
                    targetBuffer = new PinnedBuffer<double>(arrayToPin);
                }
                else
                {
                    featureBuffers[fIdx++] = new PinnedBuffer<double>(arrayToPin);
                }
            }

            return new PinnedData(featureBuffers, targetBuffer, Rows, FeatureCount);
        }

        /// <summary>
        /// Gets the raw underlying array for a named column (used by Splitter and Scalers).
        /// </summary>
        public double[] GetRawColumn(string columnName)
        {
            int idx = Array.IndexOf(_columnNames, columnName);
            if (idx < 0) throw new ArgumentException($"Column '{columnName}' not found.");
            return _columns[idx];
        }

        public void Dispose()
        {
            // Dataset itself doesn't hold unmanaged resources, PinnedData does.
        }
    }
}
