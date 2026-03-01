namespace SymbolicRegressionNet.Sdk.Api
{
    /// <summary>
    /// Configuration options for ensemble strategies (bagging) over the symbolic regressor.
    /// </summary>
    public struct EnsembleOptions
    {
        /// <summary>
        /// The number of independent SymbolicRegressor instances to run.
        /// Defaults to 10.
        /// </summary>
        public int NumberOfModels { get; set; }

        /// <summary>
        /// The ratio of the dataset size to sample for each bootstrap iteration.
        /// E.g. 1.0 means sample exactly N rows with replacement.
        /// Defaults to 1.0.
        /// </summary>
        public double BootstrapSampleRatio { get; set; }

        /// <summary>
        /// Creates a new instance of EnsembleOptions with default values.
        /// </summary>
        public EnsembleOptions()
        {
            NumberOfModels = 10;
            BootstrapSampleRatio = 1.0;
        }
    }
}
