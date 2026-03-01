using SymbolicRegressionNet.Sdk.Api.Evaluators;

namespace SymbolicRegressionNet.Sdk.Api.Grammar
{
    /// <summary>
    /// Contract defining an underlying syntax restriction that prevents evaluating or generating 
    /// expressions outside a predefined Context-Free Grammar domain. 
    /// </summary>
    public interface IGrammarConstraint
    {
        /// <summary>
        /// Validates whether the given abstract syntax tree conforms to the grammar constraints.
        /// </summary>
        bool IsValidSyntax(Instruction[] program);
    }
}
