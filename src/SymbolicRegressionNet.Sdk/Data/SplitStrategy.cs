namespace SymbolicRegressionNet.Sdk.Data
{
    /// <summary>
    /// Base class for all dataset splitting strategies.
    /// </summary>
    public abstract record SplitStrategy;

    /// <summary>
    /// Randomly splits a dataset into training and validation sets.
    /// </summary>
    /// <param name="TrainRatio">The proportion of the dataset to include in the train split (0.0 to 1.0).</param>
    /// <param name="Seed">The seed used by the random number generator.</param>
    public sealed record RandomSplit(double TrainRatio = 0.8, int Seed = 42) : SplitStrategy;

    /// <summary>
    /// Splits a dataset sequentially based on a time column, preserving temporal order.
    /// </summary>
    /// <param name="TimeColumn">The name of the column containing chronological data.</param>
    /// <param name="TrainRatio">The proportion of the dataset to include in the early (train) split.</param>
    public sealed record TimeSeriesSplit(string TimeColumn, double TrainRatio = 0.8) : SplitStrategy;

    /// <summary>
    /// Generates K consecutive splits for cross-validation.
    /// </summary>
    /// <param name="K">Number of folds. Must be at least 2.</param>
    /// <param name="Seed">The seed used to shuffle data before splitting.</param>
    public sealed record KFoldSplit(int K = 5, int Seed = 42) : SplitStrategy;
}
