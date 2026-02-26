namespace SymbolicRegressionNet.Sdk.Data
{
    /// <summary>
    /// Specifies the strategy for handling missing values when parsing data.
    /// </summary>
    public enum MissingValueStrategy
    {
        ThrowOnMissing,
        ReplaceWithMean,
        ReplaceWithMedian,
        DropRow
    }
}
