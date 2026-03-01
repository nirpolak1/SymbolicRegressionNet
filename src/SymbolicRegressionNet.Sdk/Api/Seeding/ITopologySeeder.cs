using System.Collections.Generic;
using SymbolicRegressionNet.Sdk.Data;

namespace SymbolicRegressionNet.Sdk.Api.Seeding
{
    /// <summary>
    /// Contract for generating educated guesses (prior topographies) to jump-start the genetic algorithm
    /// instead of initiating entirely with random walk trees.
    /// </summary>
    public interface ITopologySeeder
    {
        /// <summary>
        /// Given a dataset and the expected number of seeds, generates a list of strong expression strings.
        /// </summary>
        IReadOnlyList<string> GenerateSeeds(Dataset dataset, int count);
    }
}
