using System;
using System.Collections.Generic;
using SymbolicRegressionNet.Sdk.Data;
using SymbolicRegressionNet.Sdk.Api.Evaluators;

namespace SymbolicRegressionNet.Sdk.Api
{
    /// <summary>
    /// Evaluates an expression over the domain bounds of the dataset using Interval Arithmetic.
    /// If the expression produces completely unbounded limits or NaN domains, it fails.
    /// </summary>
    public class IntervalPruningStrategy : ITieredEvaluationStrategy
    {
        private readonly Instruction[] _compiledProgram;
        private Interval[] _featureBounds;

        public IntervalPruningStrategy(Instruction[] compiledProgram)
        {
            _compiledProgram = compiledProgram;
        }

        public TieredCheckResult CheckShouldEvaluateFull(Evaluators.IEvaluator evaluator, Evaluators.EvaluationContext context, Dataset fullData)
        {
            if (_featureBounds == null)
            {
                InitializeBounds(fullData);
            }

            int stackPtr = 0;
            Interval[] stack = new Interval[_compiledProgram.Length];

            foreach (var inst in _compiledProgram)
            {
                switch (inst.Op)
                {
                    case OpCode.PushConst:
                        stack[stackPtr++] = new Interval(inst.Value, inst.Value);
                        break;
                    case OpCode.PushVar:
                        stack[stackPtr++] = _featureBounds[(int)inst.Value];
                        break;
                    case OpCode.Add:
                        stackPtr--;
                        stack[stackPtr - 1] = Interval.Add(stack[stackPtr - 1], stack[stackPtr]);
                        break;
                    case OpCode.Sub:
                        stackPtr--;
                        stack[stackPtr - 1] = Interval.Sub(stack[stackPtr - 1], stack[stackPtr]);
                        break;
                    case OpCode.Mul:
                        stackPtr--;
                        stack[stackPtr - 1] = Interval.Mul(stack[stackPtr - 1], stack[stackPtr]);
                        break;
                    case OpCode.Div:
                        stackPtr--;
                        stack[stackPtr - 1] = Interval.Div(stack[stackPtr - 1], stack[stackPtr]);
                        break;
                    case OpCode.Sin:
                        stack[stackPtr - 1] = Interval.Sin(stack[stackPtr - 1]);
                        break;
                }

                if (stackPtr > 0 && !stack[stackPtr - 1].IsValid())
                {
                    // Fast fail: early mathematical invalidity detected
                    return new TieredCheckResult(false, double.PositiveInfinity);
                }
            }

            Interval result = stack[0];
            bool bounded = !double.IsInfinity(result.Min) && !double.IsInfinity(result.Max) && result.IsValid();

            return new TieredCheckResult(bounded, 0.0);
        }

        private void InitializeBounds(Dataset fullData)
        {
            int cols = fullData.FeatureCount;
            _featureBounds = new Interval[cols];
            for (int c = 0; c < cols; c++)
            {
                double min = double.MaxValue;
                double max = double.MinValue;
                for (int r = 0; r < fullData.Rows; r++)
                {
                    double val = fullData.GetFeatureValue(r, c);
                    if (val < min) min = val;
                    if (val > max) max = val;
                }
                _featureBounds[c] = new Interval(min, max);
            }
        }
    }
}
