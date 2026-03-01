using Xunit;
using SymbolicRegressionNet.Sdk.Api.Simplification;

namespace SymbolicRegressionNet.Sdk.Tests
{
    public class BasicAlgebraicSimplifierTests
    {
        [Fact]
        public void Simplifier_Collapses_VariableMinusVariable()
        {
            var simplifier = new BasicAlgebraicSimplifier();
            // Expected x0 - x0 -> 0
            var result = simplifier.Simplify("x0 - x0");
            Assert.Equal("0", result);
        }

        [Fact]
        public void Simplifier_Collapses_VariableDivVariable()
        {
            var simplifier = new BasicAlgebraicSimplifier();
            // Expected x1 / x1 -> 1
            var result = simplifier.Simplify("x1/x1");
            Assert.Equal("1", result);
        }

        [Fact]
        public void Simplifier_MultiplicationByZero_YieldsZero()
        {
            var simplifier = new BasicAlgebraicSimplifier();
            var result1 = simplifier.Simplify("x0*0");
            Assert.Equal("0", result1);

            var result2 = simplifier.Simplify("0*x1");
            Assert.Equal("0", result2);
        }

        [Fact]
        public void Simplifier_AdditionIdentity()
        {
            var simplifier = new BasicAlgebraicSimplifier();
            var result1 = simplifier.Simplify("x0+0");
            Assert.Equal("x0", result1);

            var result2 = simplifier.Simplify("0+x1");
            Assert.Equal("x1", result2);
        }
    }
}
