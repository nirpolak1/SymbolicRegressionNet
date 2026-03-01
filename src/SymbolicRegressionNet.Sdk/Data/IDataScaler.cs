namespace SymbolicRegressionNet.Sdk.Data
{
    /// <summary>
    /// Contract for a stateful data scaler that learns parameters from a dataset
    /// and applies them to identical or new dataset views.
    /// </summary>
    public interface IDataScaler
    {
        /// <summary>
        /// Learns scaling parameters (e.g., mean and standard deviation) from the given dataset.
        /// </summary>
        /// <param name="dataset">The dataset to learn from.</param>
        void Fit(Dataset dataset);

        /// <summary>
        /// Transforms the given dataset using the previously learned parameters.
        /// Returns a new dataset view with normalized columns.
        /// </summary>
        /// <param name="dataset">The dataset to scale.</param>
        /// <returns>A new dataset with scaled values.</returns>
        Dataset Transform(Dataset dataset);

        /// <summary>
        /// Fits parameters and then transforms the dataset in one step.
        /// </summary>
        Dataset FitTransform(Dataset dataset);
    }
}
