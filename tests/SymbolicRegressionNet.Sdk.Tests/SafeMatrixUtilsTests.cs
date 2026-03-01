#nullable disable
using System;
using Xunit;
using SymbolicRegressionNet.Sdk.Api.Optimization;

namespace SymbolicRegressionNet.Sdk.Tests
{
    public class SafeMatrixUtilsTests
    {
        [Fact]
        public void SafeInverse2x2_ThrowsIllConditionedMatrixException_OnSingularMatrix()
        {
            // Create a strictly singular matrix (determinant = 0)
            var singularMatrix = new double[,]
            {
                { 1.0, 2.0 },
                { 2.0, 4.0 }
            };

            Assert.Throws<IllConditionedMatrixException>(() => SafeMatrixUtils.SafeInverse2x2(singularMatrix));
        }

        [Fact]
        public void SafeInverse2x2_InvertsValidMatrixSuccessfully()
        {
            // Simple Identity matrix swap, det = (4*3) - (2*1) = 10
            var validMatrix = new double[,]
            {
                { 4.0, 2.0 },
                { 1.0, 3.0 }
            };

            var inverse = SafeMatrixUtils.SafeInverse2x2(validMatrix);

            // Expect a = d/det (3/10)
            Assert.Equal(0.3, inverse[0,0], 5);
            // Expect b = -b/det (-2/10)
            Assert.Equal(-0.2, inverse[0,1], 5);
            // Expect c = -c/det (-1/10)
            Assert.Equal(-0.1, inverse[1,0], 5);
            // Expect d = a/det (4/10)
            Assert.Equal(0.4, inverse[1,1], 5);
        }
    }
}
