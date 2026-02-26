using System;
using System.Collections.Generic;
using System.Linq;

namespace SymbolicRegressionNet.Sdk.Data
{
    /// <summary>
    /// Utility for splitting a dataset into training and validation subsets using zero-copy views.
    /// </summary>
    public static class Splitter
    {
        public static (Dataset Train, Dataset Validation) Split(Dataset dataset, SplitStrategy strategy)
        {
            return strategy switch
            {
                RandomSplit rs => SplitRandom(dataset, rs),
                TimeSeriesSplit ts => SplitTimeSeries(dataset, ts),
                _ => throw new ArgumentException("Unsupported split strategy")
            };
        }

        public static IEnumerable<(Dataset Train, Dataset Validation)> CrossValidate(Dataset dataset, KFoldSplit strategy)
        {
            int N = dataset.Rows;
            int[] indices = Enumerable.Range(0, N).ToArray();
            
            var rng = new Random(strategy.Seed);
            Shuffle(indices, rng);

            int k = strategy.K;
            if (k < 2 || k > N) throw new ArgumentException("K must be >= 2 and <= number of rows.");

            int foldSize = N / k;
            int remainder = N % k;

            int currentStart = 0;
            for (int i = 0; i < k; i++)
            {
                int currentFoldSize = foldSize + (i < remainder ? 1 : 0);
                
                var valIndices = new int[currentFoldSize];
                Array.Copy(indices, currentStart, valIndices, 0, currentFoldSize);

                var trainIndices = new int[N - currentFoldSize];
                if (currentStart > 0)
                {
                    Array.Copy(indices, 0, trainIndices, 0, currentStart);
                }
                if (currentStart + currentFoldSize < N)
                {
                    Array.Copy(indices, currentStart + currentFoldSize, trainIndices, currentStart, N - (currentStart + currentFoldSize));
                }

                currentStart += currentFoldSize;

                yield return (dataset.CreateView(trainIndices), dataset.CreateView(valIndices));
            }
        }

        private static (Dataset Train, Dataset Validation) SplitRandom(Dataset dataset, RandomSplit strategy)
        {
            int N = dataset.Rows;
            int[] indices = Enumerable.Range(0, N).ToArray();
            
            var rng = new Random(strategy.Seed);
            Shuffle(indices, rng);

            int trainSize = (int)(N * strategy.TrainRatio);
            
            int[] trainIndices = new int[trainSize];
            Array.Copy(indices, 0, trainIndices, 0, trainSize);

            int valSize = N - trainSize;
            int[] valIndices = new int[valSize];
            Array.Copy(indices, trainSize, valIndices, 0, valSize);

            return (dataset.CreateView(trainIndices), dataset.CreateView(valIndices));
        }

        private static (Dataset Train, Dataset Validation) SplitTimeSeries(Dataset dataset, TimeSeriesSplit strategy)
        {
            int N = dataset.Rows;
            double[] timeCol = dataset.GetRawColumn(strategy.TimeColumn);

            // Sort indices based on the time column values
            int[] indices = Enumerable.Range(0, N).ToArray();
            Array.Sort(indices, (a, b) => timeCol[a].CompareTo(timeCol[b]));

            int trainSize = (int)(N * strategy.TrainRatio);
            
            int[] trainIndices = new int[trainSize];
            Array.Copy(indices, 0, trainIndices, 0, trainSize);

            int valSize = N - trainSize;
            int[] valIndices = new int[valSize];
            Array.Copy(indices, trainSize, valIndices, 0, valSize);

            return (dataset.CreateView(trainIndices), dataset.CreateView(valIndices));
        }

        private static void Shuffle<T>(T[] array, Random rng)
        {
            int n = array.Length;
            while (n > 1) 
            {
                int k = rng.Next(n--);
                T temp = array[n];
                array[n] = array[k];
                array[k] = temp;
            }
        }
    }
}
