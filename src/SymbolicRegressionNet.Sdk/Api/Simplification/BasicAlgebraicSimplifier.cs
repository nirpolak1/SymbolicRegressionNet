using System;
using System.Text.RegularExpressions;

namespace SymbolicRegressionNet.Sdk.Api.Simplification
{
    public class BasicAlgebraicSimplifier : ICasSimplifier
    {
        public string Simplify(string equation)
        {
            if (string.IsNullOrWhiteSpace(equation)) return equation;

            string current = equation.Replace(" ", "");
            string previous;

            do
            {
                previous = current;

                // Strip obvious redundant parens: (x0) -> x0
                current = Regex.Replace(current, @"\(([a-zA-Z0-9_]+)\)", "$1");

                // x - x -> 0 (simplified regex detecting exact variable match subtraction)
                current = Regex.Replace(current, @"([a-zA-Z0-9_]+)-\1", "0");
                
                // x / x -> 1
                current = Regex.Replace(current, @"([a-zA-Z0-9_]+)/\1", "1");

                // 0 * X -> 0
                current = Regex.Replace(current, @"0\*[a-zA-Z0-9_]+|[a-zA-Z0-9_]+\*0", "0");
                
                // 1 * X -> X
                current = Regex.Replace(current, @"1\*([a-zA-Z0-9_]+)|([a-zA-Z0-9_]+)\*1", "$1$2");
                
                // X + 0 -> X
                current = Regex.Replace(current, @"\+0|0\+", "");

                // --X -> +X
                current = current.Replace("--", "+");

                // Handle leading/trailing pluses that may have been exposed
                current = current.Trim('+');

                if (string.IsNullOrEmpty(current)) current = "0";

            } while (current != previous);

            return current;
        }
    }
}
