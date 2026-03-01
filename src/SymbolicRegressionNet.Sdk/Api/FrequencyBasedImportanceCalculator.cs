using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SymbolicRegressionNet.Sdk.Api
{
    /// <summary>
    /// Calculates feature importance by analyzing the frequency of variables in the Pareto front,
    /// weighted by the R2 score of the models they appear in.
    /// </summary>
    public class FrequencyBasedImportanceCalculator : IFeatureImportanceCalculator
    {
        public IReadOnlyList<FeatureImportance> Calculate(HallOfFame hallOfFame, int totalVariables, IReadOnlyList<string> variableNames = null)
        {
            if (hallOfFame.Count == 0 || totalVariables <= 0)
                return new List<FeatureImportance>();

            double[] scores = new double[totalVariables];
            double totalWeight = 0;

            foreach (var model in hallOfFame)
            {
                // A very simplistic weight: bounded R2 [0,1]
                // Better models have a higher influence on feature importance.
                double weight = model.R2 > 0 ? model.R2 : 0;
                totalWeight += weight;

                // Find explicit tokens like x0, x1...
                var matches = Regex.Matches(model.Expression, @"x(?<idx>\d+)");
                var uniqueVarsInModel = new HashSet<int>();

                foreach (Match match in matches)
                {
                    if (int.TryParse(match.Groups["idx"].Value, out int idx) && idx < totalVariables)
                    {
                        uniqueVarsInModel.Add(idx);
                    }
                }

                foreach (var idx in uniqueVarsInModel)
                {
                    scores[idx] += weight;
                }
            }

            // Normalize
            var result = new List<FeatureImportance>(totalVariables);
            for (int i = 0; i < totalVariables; i++)
            {
                double normalizedScore = totalWeight > 0 ? scores[i] / totalWeight : 0;
                string name = variableNames != null && i < variableNames.Count ? variableNames[i] : $"x{i}";
                result.Add(new FeatureImportance(i, name, normalizedScore));
            }

            return result.OrderByDescending(f => f.Score).ToList();
        }
    }
}
