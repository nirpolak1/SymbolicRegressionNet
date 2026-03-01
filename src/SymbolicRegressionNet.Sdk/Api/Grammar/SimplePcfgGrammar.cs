using System.Collections.Generic;
using SymbolicRegressionNet.Sdk.Api.Evaluators;

namespace SymbolicRegressionNet.Sdk.Api.Grammar
{
    /// <summary>
    /// A lightweight implementation evaluating structural constraints against postfix syntax sequences.
    /// Specifically restricts nested trigonometric functions which commonly drive genetic bloat without predictive resolution.
    /// </summary>
    public class SimplePcfgGrammar : IGrammarConstraint
    {
        private readonly int _maxNestedTrigDepth;

        public SimplePcfgGrammar(int maxNestedTrigDepth = 1)
        {
            _maxNestedTrigDepth = maxNestedTrigDepth;
        }

        public bool IsValidSyntax(Instruction[] program)
        {
            // Validating syntax limits on a postfix stack
            // A genuine PCFG requires simulating the AST construction stack to trace 
            // the parenthood of branches. For this mock, we enforce exact adjacency rules.

            for (int i = 0; i < program.Length; i++)
            {
                if (i > 0 && program[i].Op == OpCode.Sin && program[i - 1].Op == OpCode.Sin)
                {
                    // Immediate nested trig: sin(sin(..)) is a very typical bloat vector
                    return false; 
                }
            }

            return true;
        }
    }
}
