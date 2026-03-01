using System;

namespace SymbolicRegressionNet.Sdk.Api
{
    /// <summary>
    /// Contract for evaluating if a generated equation symbolically matches the ground truth.
    /// Useful for benchmarks like SRBench tracking exact recovery of physics laws.
    /// </summary>
    public interface ISymbolicRecoveryMetric
    {
        bool IsSymbolicMatch(string candidateEquation, string targetEquation);
    }

    /// <summary>
    /// Basic symbolic recovery via exact string matching (stripped of whitespace).
    /// Advanced implementations might use a Computer Algebra System (CAS).
    /// </summary>
    public class ExactStringRecovery : ISymbolicRecoveryMetric
    {
        public bool IsSymbolicMatch(string candidate, string target)
        {
            if (string.IsNullOrWhiteSpace(candidate) || string.IsNullOrWhiteSpace(target))
                return false;

            string cLog = candidate.Replace(" ", "").ToLowerInvariant();
            string tLog = target.Replace(" ", "").ToLowerInvariant();

            return cLog == tLog;
        }
    }
}
