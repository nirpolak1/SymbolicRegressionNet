using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SymbolicRegressionNet.Sdk.Data;

namespace SymbolicRegressionNet.Sdk.Api.Evaluators
{
    public sealed class NativeBatchEvaluator : IBatchEvaluator
    {
        [DllImport("SymbolicRegressionNet.Core", CallingConvention = CallingConvention.Cdecl, EntryPoint = "EvaluateBatch_AVX2")]
        private static extern void EvaluateBatch_AVX2(
            [In] int[] opcodeArray,
            [In] double[] valueArray,
            int genomeLength,
            [In] double[] featuresFlattened,
            int numRows,
            int numVars,
            [Out] double[] outResults
        );

        private readonly bool _useAvx2;

        public NativeBatchEvaluator(bool useAvx2 = true)
        {
            _useAvx2 = useAvx2;
        }

        public void EvaluateBatch(IReadOnlyList<Instruction[]> programs, Dataset data, double[,] resultsBuffer)
        {
            int numRows = data.Rows;
            int numVars = data.FeatureCount;
            
            // Allocate a scratchpad for the results of a single program
            double[] batchResults = new double[numRows];

            for (int p = 0; p < programs.Count; p++)
            {
                var prog = programs[p];
                int len = prog.Length;
                
                int[] opcodes = new int[len];
                double[] values = new double[len];

                for (int i = 0; i < len; i++)
                {
                    opcodes[i] = (int)prog[i].Op;
                    values[i] = prog[i].Value;
                }

                if (_useAvx2)
                {
                    EvaluateBatch_AVX2(
                        opcodes,
                        values,
                        len,
                        data.FlattenedFeatures, // Assumes dataset caching support
                        numRows,
                        numVars,
                        batchResults
                    );
                }

                // Copy back
                for (int r = 0; r < numRows; r++)
                {
                    resultsBuffer[p, r] = batchResults[r];
                }
            }
        }
    }
}
