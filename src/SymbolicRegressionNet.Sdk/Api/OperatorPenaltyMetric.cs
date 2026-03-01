using System;
using System.Text.RegularExpressions;

namespace SymbolicRegressionNet.Sdk.Api
{
    /// <summary>
    /// A complexity metric that applies heavy penalties for non-linear,
    /// transcendental, and nested operators present in the expression string.
    /// </summary>
    public class OperatorPenaltyMetric : IComplexityMetric
    {
        public int CalculateComplexity(string expression, int baseComplexity)
        {
            if (string.IsNullOrWhiteSpace(expression)) return baseComplexity;

            int score = baseComplexity; // start with proxy

            // Count occurrences of linear operators (light penalty)
            score += CountString(expression, "+") * 1;
            score += CountString(expression, "-") * 1;
            score += CountString(expression, "*") * 2;

            // Non-linear operators (medium penalty)
            score += CountString(expression, "/") * 3;
            score += CountString(expression, "^") * 4;
            score += CountString(expression, "abs") * 3;

            // Transcendental / heavy structural operators (heavy penalty)
            score += CountString(expression, "sin") * 5;
            score += CountString(expression, "cos") * 5;
            score += CountString(expression, "tan") * 6;
            score += CountString(expression, "exp") * 5;
            score += CountString(expression, "log") * 5;

            return score;
        }

        private int CountString(string source, string match)
        {
            int count = 0;
            int i = 0;
            while ((i = source.IndexOf(match, i, StringComparison.OrdinalIgnoreCase)) != -1)
            {
                i += match.Length;
                count++;
            }
            return count;
        }
    }
}
