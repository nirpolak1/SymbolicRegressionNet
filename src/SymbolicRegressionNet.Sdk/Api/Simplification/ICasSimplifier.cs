namespace SymbolicRegressionNet.Sdk.Api.Simplification
{
    /// <summary>
    /// Contract for a Computer Algebra System (CAS) module to shrink or mathematically simplify an equation.
    /// </summary>
    public interface ICasSimplifier
    {
        /// <summary>
        /// Simplifies a mathematical equation string, removing identities and redundant subtrees.
        /// </summary>
        string Simplify(string equation);
    }
}
