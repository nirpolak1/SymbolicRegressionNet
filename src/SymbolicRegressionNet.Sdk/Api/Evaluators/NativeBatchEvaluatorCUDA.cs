using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SymbolicRegressionNet.Sdk.Data;

namespace SymbolicRegressionNet.Sdk.Api.Evaluators
{
    public sealed class NativeBatchEvaluatorCUDA : IBatchEvaluator
    {
        [DllImport("SymbolicRegressionNet.Core", CallingConvention = CallingConvention.Cdecl, EntryPoint = "EvaluateBatch_CUDA")]
        private static extern void EvaluateBatch_CUDA(
            [In] int[] opcodeArray,
            [In] double[] valueArray,
            int genomeLength,
            [In] double[] featuresFlattened,
            int numRows,
            int numVars,
            [Out] double[] outResults
        );

        public void EvaluateBatch(IReadOnlyList<Instruction[]> programs, Dataset data, double[,] resultsBuffer)
        {
            int numRows = data.Rows;
            int numVars = data.FeatureCount;
            
            // Reusable GPU staging buffer per engine run
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

                EvaluateBatch_CUDA(
                    opcodes,
                    values,
                    len,
                    data.FlattenedFeatures,
                    numRows,
                    numVars,
                    batchResults
                );

                // Copy results back for RMSE score calculation
                for (int r = 0; r < numRows; r++)
                {
                    resultsBuffer[p, r] = batchResults[r];
                }
            }
        }
    }
}
