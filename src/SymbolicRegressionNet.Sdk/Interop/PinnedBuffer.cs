using System;
using System.Runtime.InteropServices;

namespace SymbolicRegressionNet.Sdk.Interop
{
    /// <summary>
    /// A generic, disposable wrapper around a pinned GC handle, providing safe and
    /// zero-copy access to a managed array's raw memory address for unmanaged interop.
    /// </summary>
    /// <typeparam name="T">An unmanaged (blittable) type.</typeparam>
    public sealed class PinnedBuffer<T> : IDisposable where T : unmanaged
    {
        private readonly T[] _array;
        private GCHandle _handle;
        private bool _disposed;

        /// <summary>
        /// Pins the provided array in memory.
        /// </summary>
        /// <param name="array">The array to pin.</param>
        public PinnedBuffer(T[] array)
        {
            _array = array ?? throw new ArgumentNullException(nameof(array));
            _handle = GCHandle.Alloc(_array, GCHandleType.Pinned);
        }

        /// <summary>The stable pointer to the pinned array's first element.</summary>
        public IntPtr Pointer => _handle.AddrOfPinnedObject();

        /// <summary>A span over the underlying array.</summary>
        public Span<T> Span => _array.AsSpan();

        /// <summary>Length of the array.</summary>
        public int Length => _array.Length;

        /// <summary>
        /// Frees the underlying GC handle.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _handle.Free();
                _disposed = true;
            }
        }
    }
}
