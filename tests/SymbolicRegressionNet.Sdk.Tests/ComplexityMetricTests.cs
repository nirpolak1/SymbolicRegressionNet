using Xunit;
using SymbolicRegressionNet.Sdk.Api;

namespace SymbolicRegressionNet.Sdk.Tests
{
    public class ComplexityMetricTests
    {
        [Fact]
        public void OperatorPenaltyMetric_CalculatesCorrectComplexity()
        {
            var metric = new OperatorPenaltyMetric();
            int baseK = 1;

            // Simple line: x0 + x1 (two additions = +2 points on top of base)
            string expr1 = "x0 + x1";
            int score1 = metric.CalculateComplexity(expr1, baseK);
            Assert.Equal(1 + 1, score1); // + counts as 1

            // Non linear: x0 * x1 + sin(x0)
            // * is +2, + is +1, sin is +5 => 1 + 2 + 1 + 5 = 9
            string expr2 = "x0 * x1 + sin(x0)";
            int score2 = metric.CalculateComplexity(expr2, baseK);
            Assert.Equal(1 + 2 + 1 + 5, score2);

            // Nested transedental: exp(sin(x0)) + abs(x1)
            // exp = +5, sin = +5, + = +1, abs = +3 => 1 + 5 + 5 + 1 + 3 = 15
            string expr3 = "exp(sin(x0)) + abs(x1)";
            int score3 = metric.CalculateComplexity(expr3, baseK);
            Assert.Equal(1 + 5 + 5 + 1 + 3, score3);
        }
    }
}
