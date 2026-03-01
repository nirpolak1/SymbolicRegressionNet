using Xunit;
using SymbolicRegressionNet.Sdk.Api.Evaluators;
using SymbolicRegressionNet.Sdk.Api.Grammar;

namespace SymbolicRegressionNet.Sdk.Tests
{
    public class GrammarConstraintTests
    {
        [Fact]
        public void SimplePcfgGrammar_BlocksNestedTrig()
        {
            var grammar = new SimplePcfgGrammar(maxNestedTrigDepth: 1);

            // Valid: sin(x) -> x sin
            var validProgram = new[]
            {
                new Instruction(OpCode.PushVar, 0),
                new Instruction(OpCode.Sin)
            };

            // Invalid: sin(sin(x)) -> x sin sin
            var invalidProgram = new[]
            {
                new Instruction(OpCode.PushVar, 0),
                new Instruction(OpCode.Sin),
                new Instruction(OpCode.Sin)
            };

            Assert.True(grammar.IsValidSyntax(validProgram));
            Assert.False(grammar.IsValidSyntax(invalidProgram));
        }

        [Fact]
        public void SimplePcfgGrammar_AcceptsInterleavedTrig()
        {
            var grammar = new SimplePcfgGrammar(maxNestedTrigDepth: 1);

            // Valid: sin(x) + sin(y) -> x sin y sin +
            var validProgram = new[]
            {
                new Instruction(OpCode.PushVar, 0),
                new Instruction(OpCode.Sin),
                new Instruction(OpCode.PushVar, 1),
                new Instruction(OpCode.Sin),
                new Instruction(OpCode.Add)
            };

            Assert.True(grammar.IsValidSyntax(validProgram));
        }
    }
}
