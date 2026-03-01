using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace SymbolicRegressionNet.Sdk.Reporting
{
    public abstract class BaseExporter : IExpressionExporter
    {
        public abstract string FormatName { get; }

        public string ExportAll(HallOfFame hallOfFame, IReadOnlyList<string> variableNames = null)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"// Pareto Front Export: {FormatName}");
            foreach (var model in hallOfFame)
            {
                sb.AppendLine($"// MSE: {model.Mse:F6}, R2: {model.R2:F6}, Complexity: {model.Complexity}");
                sb.AppendLine(Export(model.Expression, variableNames));
                sb.AppendLine();
            }
            return sb.ToString();
        }

        public abstract string Export(string expression, IReadOnlyList<string> variableNames = null);

        protected string ReplaceVariables(string expression, IReadOnlyList<string> variableNames)
        {
            if (variableNames == null || variableNames.Count == 0) return expression;

            return Regex.Replace(expression, @"x(?<idx>\d+)", match =>
            {
                if (int.TryParse(match.Groups["idx"].Value, out int idx) && idx < variableNames.Count)
                {
                    return variableNames[idx];
                }
                return match.Value;
            });
        }
    }

    public class PythonExporter : BaseExporter
    {
        public override string FormatName => "Python (NumPy)";

        public override string Export(string expression, IReadOnlyList<string> variableNames = null)
        {
            var expr = ReplaceVariables(expression, variableNames);
            // Replace math functions with numpy equivalents
            expr = Regex.Replace(expr, @"\bsin\(", "np.sin(");
            expr = Regex.Replace(expr, @"\bcos\(", "np.cos(");
            expr = Regex.Replace(expr, @"\btan\(", "np.tan(");
            expr = Regex.Replace(expr, @"\bexp\(", "np.exp(");
            expr = Regex.Replace(expr, @"\blog\(", "np.log(");
            expr = Regex.Replace(expr, @"\bsqrt\(", "np.sqrt(");
            expr = Regex.Replace(expr, @"\bpow\(", "np.power(");
            return expr;
        }
    }

    public class CCodeExporter : BaseExporter
    {
        public override string FormatName => "C++";

        public override string Export(string expression, IReadOnlyList<string> variableNames = null)
        {
            var expr = ReplaceVariables(expression, variableNames);
            expr = Regex.Replace(expr, @"\bsin\(", "std::sin(");
            expr = Regex.Replace(expr, @"\bcos\(", "std::cos(");
            expr = Regex.Replace(expr, @"\btan\(", "std::tan(");
            expr = Regex.Replace(expr, @"\bexp\(", "std::exp(");
            expr = Regex.Replace(expr, @"\blog\(", "std::log(");
            expr = Regex.Replace(expr, @"\bsqrt\(", "std::sqrt(");
            expr = Regex.Replace(expr, @"\bpow\(", "std::pow(");
            return expr;
        }
    }

    public class LatexExporter : BaseExporter
    {
        public override string FormatName => "LaTeX";

        public override string Export(string expression, IReadOnlyList<string> variableNames = null)
        {
            var expr = ReplaceVariables(expression, variableNames);
            expr = expr.Replace("*", @" \cdot ");
            // Basic replacement for math functions
            expr = Regex.Replace(expr, @"\bsin\(", @"\sin(");
            expr = Regex.Replace(expr, @"\bcos\(", @"\cos(");
            expr = Regex.Replace(expr, @"\btan\(", @"\tan(");
            expr = Regex.Replace(expr, @"\bexp\(", @"\exp(");
            expr = Regex.Replace(expr, @"\blog\(", @"\log(");
            // A true LaTeX fractions parser requires a tree, but we can do a naive wrapper for demo
            return $"$$ {expr} $$";
        }
    }
}
