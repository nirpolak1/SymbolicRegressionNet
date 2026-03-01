using System;
using System.Collections.Generic;
using SymbolicRegressionNet.Sdk.Data;
using SymbolicRegressionNet.Sdk.Api.Evaluators;

namespace SymbolicRegressionNet.Sdk.Api
{
    /// <summary>
    /// Checks expressions against dimensional (unit) constraints before running costly evaluations.
    /// If an expression violates additive dimension matching or misaligns with target dimensions, it is discarded.
    /// </summary>
    public class DimensionalTieredStrategy : ITieredEvaluationStrategy
    {
        private readonly Instruction[] _compiledProgram;
        private readonly DimensionalType _targetDimension;
        private readonly IReadOnlyDictionary<int, DimensionalType> _featureDimensions;

        public DimensionalTieredStrategy(
            Instruction[] compiledProgram, 
            DimensionalType targetDimension,
            IReadOnlyDictionary<int, DimensionalType> featureDimensions)
        {
            _compiledProgram = compiledProgram;
            _targetDimension = targetDimension;
            _featureDimensions = featureDimensions ?? new Dictionary<int, DimensionalType>();
        }

        public TieredCheckResult CheckShouldEvaluateFull(IEvaluator evaluator, EvaluationContext context, Dataset fullData)
        {
            int stackPtr = 0;
            DimensionalType[] stack = new DimensionalType[_compiledProgram.Length];

            foreach (var inst in _compiledProgram)
            {
                switch (inst.Op)
                {
                    case OpCode.PushConst:
                        // Assuming constants derive the dimension needed or are purely dimensionless. We assign dimensionless.
                        stack[stackPtr++] = DimensionalType.Dimensionless;
                        break;
                    case OpCode.PushVar:
                        stack[stackPtr++] = _featureDimensions.TryGetValue((int)inst.Value, out var dt) ? dt : DimensionalType.Unknown;
                        break;
                    case OpCode.Add:
                    case OpCode.Sub:
                        stackPtr--;
                        var left = stack[stackPtr - 1];
                        var right = stack[stackPtr];
                        if (left.Equals(DimensionalType.Unknown) || right.Equals(DimensionalType.Unknown))
                        {
                            stack[stackPtr - 1] = DimensionalType.Unknown;
                        }
                        else if (!left.Equals(right))
                        {
                            // Fast fail: cannot add meters and kilograms
                            return new TieredCheckResult(false, double.PositiveInfinity);
                        }
                        // DimensionalType remains unchanged for addition
                        break;
                    case OpCode.Mul:
                        stackPtr--;
                        stack[stackPtr - 1] *= stack[stackPtr];
                        break;
                    case OpCode.Div:
                        stackPtr--;
                        stack[stackPtr - 1] /= stack[stackPtr];
                        break;
                    case OpCode.Sin:
                        // Sine requires a dimensionless argument. Fast fail otherwise.
                        if (!stack[stackPtr - 1].Equals(DimensionalType.Dimensionless) && !stack[stackPtr - 1].Equals(DimensionalType.Unknown))
                        {
                            return new TieredCheckResult(false, double.PositiveInfinity);
                        }
                        stack[stackPtr - 1] = DimensionalType.Dimensionless;
                        break;
                }
            }

            // Root must match target
            DimensionalType finalDim = stack[0];
            bool valid = finalDim.Equals(_targetDimension) || finalDim.Equals(DimensionalType.Unknown);

            return new TieredCheckResult(valid, valid ? 0.0 : double.PositiveInfinity);
        }
    }
}
