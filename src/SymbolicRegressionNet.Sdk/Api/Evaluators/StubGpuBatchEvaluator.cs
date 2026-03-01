using System;
using System.Collections.Generic;
using SymbolicRegressionNet.Sdk.Data;

namespace SymbolicRegressionNet.Sdk.Api.Evaluators
{
    /// <summary>
    /// A stub reference implementation indicating where ILGPU or ComputeSharp integration 
    /// would intercept the batch evaluation request and dispatch to kernel space.
    /// Currently simulates hardware parallelism by executing sequential/TPL logic.
    /// </summary>
    public class StubGpuBatchEvaluator : IBatchEvaluator
    {
        public void EvaluateBatch(IReadOnlyList<Instruction[]> programs, Dataset data, double[,] resultsBuffer)
        {
            if (programs == null) throw new ArgumentNullException(nameof(programs));
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (resultsBuffer == null) throw new ArgumentNullException(nameof(resultsBuffer));

            if (resultsBuffer.GetLength(0) < programs.Count || resultsBuffer.GetLength(1) < data.Rows)
            {
                throw new ArgumentException("resultsBuffer is insufficiently sized to hold batch outputs.");
            }

            // SIMULATION: In a real ILGPU kernel, this would be:
            // GPU.Launch(kernel, new Index2D(programs.Count, data.Rows), devicePrograms, deviceData, deviceResults);
            
            // For the stub implementation, we just mock the batch processing using the FastStackEvaluator
            // so tests and orchestrators can consume the interface.
            System.Threading.Tasks.Parallel.For(0, programs.Count, programIdx =>
            {
                var evaluator = new FastStackEvaluator(programs[programIdx]);
                double[] rowOutput = new double[data.Rows];
                
                evaluator.EvaluateOnto(data, rowOutput);
                
                for (int row = 0; row < data.Rows; row++)
                {
                    resultsBuffer[programIdx, row] = rowOutput[row];
                }
            });
        }
    }
}
