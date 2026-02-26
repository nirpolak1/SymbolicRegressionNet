using System;
using SymbolicRegressionNet.Sdk.Data;
using SymbolicRegressionNet.Sdk.Interop;

namespace SymbolicRegressionNet.Sdk
{
    /// <summary>
    /// A fluent builder for configuring and creating a SymbolicRegressor.
    /// </summary>
    public sealed class RegressionBuilder
    {
        private Dataset _dataset;
        private Dataset _valDataset;

        private int _populationSize = 200;
        private int _maxGenerations = 50;
        private int _maxTreeDepth = 6;
        private int _tournamentSize = 7;
        private double _crossoverRate = 0.9;
        private double _mutationRate = 0.1;
        private uint _functionsMask = uint.MaxValue; // All operators enabled by default
        private ulong _randomSeed;
        private TimeSpan? _timeLimit;
        private bool _verbose = false;
        private int _verboseStep = 10;

        public RegressionBuilder()
        {
            _randomSeed = (ulong)DateTime.UtcNow.Ticks;
        }

        /// <summary>
        /// Sets the dataset to use for training.
        /// </summary>
        public RegressionBuilder WithData(Dataset dataset)
        {
            _dataset = dataset ?? throw new ArgumentNullException(nameof(dataset));
            return this;
        }

        /// <summary>
        /// Splits the active dataset into training and validation sets.
        /// </summary>
        /// <param name="testRatio">The proportion of the data to use for validation.</param>
        public RegressionBuilder SplitData(float testRatio = 0.2f)
        {
            if (_dataset == null)
            {
                throw new InvalidOperationException("Call WithData() before SplitData().");
            }

            if (testRatio < 0f || testRatio >= 1f)
            {
                throw new ArgumentException("Test ratio must be between 0 and 1.");
            }

            double trainRatio = 1.0 - testRatio;
            var strategy = new RandomSplit(trainRatio, (int)_randomSeed);
            var (train, val) = Splitter.Split(_dataset, strategy);
            
            _dataset = train;
            _valDataset = val;

            return this;
        }

        /// <summary>
        /// Sets the population size.
        /// </summary>
        public RegressionBuilder WithPopulationSize(int size)
        {
            _populationSize = size;
            return this;
        }

        /// <summary>
        /// Sets the maximum number of generations to run.
        /// </summary>
        public RegressionBuilder WithMaxGenerations(int generations)
        {
            _maxGenerations = generations;
            return this;
        }

        /// <summary>
        /// Sets the maximum tree depth for discovered expressions.
        /// </summary>
        public RegressionBuilder WithMaxTreeDepth(int depth)
        {
            _maxTreeDepth = depth;
            return this;
        }

        /// <summary>
        /// Sets the tournament size for selection.
        /// </summary>
        public RegressionBuilder WithTournamentSize(int size)
        {
            _tournamentSize = size;
            return this;
        }

        /// <summary>
        /// Sets the crossover rate.
        /// </summary>
        public RegressionBuilder WithCrossoverRate(double rate)
        {
            _crossoverRate = rate;
            return this;
        }

        /// <summary>
        /// Sets the mutation rate.
        /// </summary>
        public RegressionBuilder WithMutationRate(double rate)
        {
            _mutationRate = rate;
            return this;
        }

        /// <summary>
        /// Sets the random seed for reproducibility.
        /// </summary>
        public RegressionBuilder WithRandomSeed(ulong seed)
        {
            _randomSeed = seed;
            return this;
        }

        /// <summary>
        /// Sets the mask of enabled functions/operators.
        /// </summary>
        public RegressionBuilder WithFunctions(uint mask)
        {
            _functionsMask = mask;
            return this;
        }

        /// <summary>
        /// Sets an optional time limit for the regression search.
        /// </summary>
        public RegressionBuilder WithTimeLimit(TimeSpan limit)
        {
            _timeLimit = limit;
            return this;
        }

        /// <summary>
        /// Enables formatted console logging of the Pareto front during training.
        /// </summary>
        public RegressionBuilder WithVerbose(bool enable = true, int step = 10)
        {
            _verbose = enable;
            _verboseStep = Math.Max(1, step);
            return this;
        }

        /// <summary>
        /// Validates the configuration and builds the regressor.
        /// </summary>
        public SymbolicRegressor Build()
        {
            if (_dataset == null) throw new ArgumentException("Dataset is required. Call WithData() before Build().");
            if (_populationSize <= 0) throw new ArgumentException("Population size must be > 0.");
            if (_maxGenerations <= 0) throw new ArgumentException("Max generations must be > 0.");
            if (_maxTreeDepth < 2 || _maxTreeDepth > 20) throw new ArgumentException("Tree depth must be between 2 and 20.");
            if (_tournamentSize <= 0) throw new ArgumentException("Tournament size must be > 0.");
            if (_crossoverRate < 0 || _crossoverRate > 1) throw new ArgumentException("Crossover rate must be between 0 and 1.");
            if (_mutationRate < 0 || _mutationRate > 1) throw new ArgumentException("Mutation rate must be between 0 and 1.");

            var options = new NativeOptions
            {
                PopulationSize = _populationSize,
                MaxGenerations = _maxGenerations,
                MaxTreeDepth = _maxTreeDepth,
                TournamentSize = _tournamentSize,
                CrossoverRate = _crossoverRate,
                MutationRate = _mutationRate,
                FunctionsMask = _functionsMask,
                RandomSeed = _randomSeed
            };

            var regressor = new SymbolicRegressor(_dataset, _valDataset, options, _timeLimit);

            if (_verbose)
            {
                regressor.AddActionHandler(report =>
                {
                    if (report.Generation == 1 || report.Generation == _maxGenerations || report.Generation % _verboseStep == 0)
                    {
                        var hof = regressor.GetHallOfFame();
                        PrintVerboseLog(report, hof);
                    }
                });
            }

            return regressor;
        }

        private static void PrintVerboseLog(GenerationReport report, HallOfFame hof)
        {
            Console.WriteLine(new string('-', 80));
            Console.WriteLine($"Generation {report.Generation,-5} | Best MSE: {report.BestMse,-10:E4} | Best R2: {report.BestR2,-7:F4} | Pareto Size: {report.ParetoFrontSize}");
            Console.WriteLine(new string('-', 80));
            Console.WriteLine($"{"Complexity",-12} | {"MSE",-12} | {"R2",-8} | {"Equation"}");
            Console.WriteLine(new string('-', 80));
            
            foreach (var model in hof)
            {
                Console.WriteLine($"{model.Complexity,-12} | {model.Mse,-12:E4} | {model.R2,-8:F4} | {model.Expression}");
            }
            Console.WriteLine(new string('-', 80));
            Console.WriteLine();
        }
    }
}
