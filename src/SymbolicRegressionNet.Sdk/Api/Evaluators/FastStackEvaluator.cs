using System;
using System.Buffers;
using System.Threading.Tasks;
using SymbolicRegressionNet.Sdk.Data;

namespace SymbolicRegressionNet.Sdk.Api.Evaluators
{
    public enum OpCode : byte
    {
        PushVar,
        PushConst,
        Add,
        Sub,
        Mul,
        Div,
        Sin
    }

    public readonly struct Instruction
    {
        public OpCode Op { get; }
        public double Value { get; }

        public Instruction(OpCode op, double value = 0)
        {
            Op = op;
            Value = value;
        }
    }

    /// <summary>
    /// A robust, zero-allocation evaluator using ArrayPool to avoid GC pauses on the hot path.
    /// Evaluates postfix representation of mathematical expressions over the dataset.
    /// </summary>
    public sealed class FastStackEvaluator : IEvaluator
    {
        public string EngineName => "ArrayPool Fast Stack";

        private readonly Instruction[] _compiledProgram;

        public FastStackEvaluator(Instruction[] compiledProgram)
        {
            _compiledProgram = compiledProgram;
        }

        public double[] Evaluate(EvaluationContext context, Dataset testData)
        {
            double[] results = new double[testData.Rows];
            EvaluateOnto(testData, results);
            return results;
        }

        public void EvaluateOnto(Dataset testData, double[] outputBuffer)
        {
            // Rent a stack from the pool to avoid allocating per-row
            // The stack size depends on the max depth of the expression. For simplicity, we rent a large enough buffer.
            int maxStackNeeded = _compiledProgram.Length;
            double[] stack = ArrayPool<double>.Shared.Rent(maxStackNeeded);

            ReadOnlySpan<Instruction> program = _compiledProgram;

            try
            {
                for (int i = 0; i < testData.Rows; i++)
                {
                    int stackPtr = 0;

                    foreach (var inst in program)
                    {
                        switch (inst.Op)
                        {
                            case OpCode.PushConst:
                                stack[stackPtr++] = inst.Value;
                                break;
                            case OpCode.PushVar:
                                stack[stackPtr++] = testData.GetFeatureValue(i, (int)inst.Value);
                                break;
                            case OpCode.Add:
                                stackPtr--;
                                stack[stackPtr - 1] = stack[stackPtr - 1] + stack[stackPtr];
                                break;
                            case OpCode.Sub:
                                stackPtr--;
                                stack[stackPtr - 1] = stack[stackPtr - 1] - stack[stackPtr];
                                break;
                            case OpCode.Mul:
                                stackPtr--;
                                stack[stackPtr - 1] = stack[stackPtr - 1] * stack[stackPtr];
                                break;
                            case OpCode.Div:
                                stackPtr--;
                                double denom = stack[stackPtr];
                                // Strict invariant check: Math.Abs < 1e-9 -> 1.0
                                stack[stackPtr - 1] = Math.Abs(denom) <= 1e-9 ? 1.0 : stack[stackPtr - 1] / denom;
                                break;
                            case OpCode.Sin:
                                stack[stackPtr - 1] = Math.Sin(stack[stackPtr - 1]);
                                break;
                        }
                    }

                    outputBuffer[i] = stack[0];
                }
            }
            finally
            {
                ArrayPool<double>.Shared.Return(stack);
            }
        }

        public Task<double[]> EvaluateAsync(EvaluationContext context, Dataset testData)
        {
            return Task.Run(() => Evaluate(context, testData));
        }
    }
}
