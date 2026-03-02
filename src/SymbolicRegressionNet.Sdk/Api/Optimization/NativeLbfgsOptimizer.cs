using System.Runtime.InteropServices;
using SymbolicRegressionNet.Sdk.Data;
using SymbolicRegressionNet.Sdk.Api.Evaluators;

namespace SymbolicRegressionNet.Sdk.Api.Optimization
{
    public sealed class NativeLbfgsOptimizer : IConstantOptimizer
    {
        [DllImport("SymbolicRegressionNet.Core", CallingConvention = CallingConvention.Cdecl, EntryPoint = "OptimizeConstants_LBFGS")]
        private static extern double OptimizeConstants_LBFGS(
            [In] int[] opcodeArray,
            [In, Out] double[] valueArray,
            int genomeLength,
            [In] double[] featuresFlattened,
            [In] double[] targetArray,
            int numRows,
            int numVars,
            double learningRate,
            int maxIterations
        );

        public double LearningRate { get; }
        public int MaxIterations { get; }

        public NativeLbfgsOptimizer(double learningRate = 0.05, int maxIterations = 20)
        {
            LearningRate = learningRate;
            MaxIterations = maxIterations;
        }

        public bool OptimizeConstants(Instruction[] program, Dataset dataset, IEvaluator evaluator)
        {
            if (dataset.TargetName == null)
                return false; // Nothing to minimize

            int len = program.Length;
            int numRows = dataset.Rows;
            int numVars = dataset.FeatureCount;
            
            int[] opcodes = new int[len];
            double[] values = new double[len];

            for (int i = 0; i < len; i++)
            {
                opcodes[i] = (int)program[i].Op;
                values[i] = program[i].Value;
            }

            // Execute Native Steepest Descent
            OptimizeConstants_LBFGS(
                opcodes,
                values,
                len,
                dataset.FlattenedFeatures,
                dataset.GetRawColumn(dataset.TargetName),
                numRows,
                numVars,
                LearningRate,
                MaxIterations
            );

            // Copy mutable constants back into the original AST instructions
            bool changed = false;
            for (int i = 0; i < len; i++)
            {
                if (program[i].Op == OpCode.PushConst && opcodes[i] == 0)
                {
                    if (System.Math.Abs(program[i].Value - values[i]) > 1e-9)
                    {
                        changed = true;
                    }
                }
                program[i] = new Instruction((OpCode)opcodes[i], values[i]);
            }

            return changed;
        }
    }
}
