using System;

namespace SymbolicRegressionNet.Sdk.Api.Optimization
{
    public class IllConditionedMatrixException : Exception
    {
        public IllConditionedMatrixException(string message) : base(message) { }
    }

    /// <summary>
    /// Utilities for robust numerical optimization steps (e.g. Levenberg-Marquardt).
    /// </summary>
    public static class SafeMatrixUtils
    {
        /// <summary>
        /// Inverts a 2x2 matrix with safeguards against ill-conditioned or singular states.
        /// Returns a safe pseudo-inverse or throws a specialized exception to abort tuning.
        /// </summary>
        public static double[,] SafeInverse2x2(double[,] matrix, double conditionThreshold = 1e-12)
        {
            if (matrix.GetLength(0) != 2 || matrix.GetLength(1) != 2)
                throw new ArgumentException("Matrix must be 2x2.");

            double a = matrix[0, 0], b = matrix[0, 1];
            double c = matrix[1, 0], d = matrix[1, 1];

            double det = (a * d) - (b * c);

            if (Math.Abs(det) < conditionThreshold)
            {
                throw new IllConditionedMatrixException($"Matrix determinant {det} is below singularity threshold {conditionThreshold}. Optimization aborted to prevent parameter explosion.");
            }

            double invDet = 1.0 / det;

            return new double[,]
            {
                {  d * invDet, -b * invDet },
                { -c * invDet,  a * invDet }
            };
        }
    }
}
