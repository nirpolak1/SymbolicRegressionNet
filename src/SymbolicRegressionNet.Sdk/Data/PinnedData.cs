using System;
using System.Collections.Generic;
using SymbolicRegressionNet.Sdk.Interop;

namespace SymbolicRegressionNet.Sdk.Data
{
    /// <summary>
    /// Represents a collection of pinned memory allocations that hold the dataset data
    /// ready for zero-copy consumption by the C++ native engine.
    /// </summary>
    public sealed class PinnedData : IDisposable
    {
        private readonly PinnedBuffer<double>[] _featureBuffers;
        private readonly PinnedBuffer<double> _targetBuffer;
        
        /// <summary>
        /// Gets an array of stable memory pointers to the feature columns.
        /// </summary>
        public IntPtr[] FeaturePointers { get; }
        
        /// <summary>
        /// Gets a stable memory pointer to the target column, or <see cref="IntPtr.Zero"/> if none.
        /// </summary>
        public IntPtr TargetPointer { get; }

        /// <summary>
        /// Gets the number of rows in the pinned dataset.
        /// </summary>
        public int Rows { get; }
        
        /// <summary>
        /// Gets the number of feature columns.
        /// </summary>
        public int Columns { get; }

        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="PinnedData"/> class.
        /// </summary>
        /// <param name="featureBuffers">Pinned buffers for feature columns.</param>
        /// <param name="targetBuffer">An optional pinned buffer for the target column.</param>
        /// <param name="rows">Number of rows.</param>
        /// <param name="columns">Number of columns.</param>
        internal PinnedData(PinnedBuffer<double>[] featureBuffers, PinnedBuffer<double> targetBuffer, int rows, int columns)
        {
            _featureBuffers = featureBuffers;
            _targetBuffer = targetBuffer;
            Rows = rows;
            Columns = columns;

            FeaturePointers = new IntPtr[featureBuffers.Length];
            for (int i = 0; i < featureBuffers.Length; i++)
            {
                FeaturePointers[i] = featureBuffers[i].Pointer;
            }

            TargetPointer = targetBuffer?.Pointer ?? IntPtr.Zero;
        }

        /// <summary>
        /// Frees the underlying garbage collector handles, unpinning the arrays.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                foreach (var buffer in _featureBuffers)
                {
                    buffer?.Dispose();
                }
                _targetBuffer?.Dispose();

                _disposed = true;
            }
        }
    }
}
