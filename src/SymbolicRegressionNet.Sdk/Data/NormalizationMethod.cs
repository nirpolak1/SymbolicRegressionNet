namespace SymbolicRegressionNet.Sdk.Data
{
    /// <summary>
    /// Specifies the method used to normalize feature columns.
    /// </summary>
    public enum NormalizationMethod
    {
        /// <summary>
        /// Standardizes features by removing the mean and scaling to unit variance.
        /// </summary>
        ZScore,
        
        /// <summary>
        /// Scales features to lie between 0 and 1.
        /// </summary>
        MinMax
    }
}
