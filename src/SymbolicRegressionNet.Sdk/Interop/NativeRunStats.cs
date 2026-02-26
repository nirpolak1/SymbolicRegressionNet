using System;
using System.Runtime.InteropServices;

namespace SymbolicRegressionNet.Sdk.Interop
{
    /// <summary>
    /// Per-generation statistics returned by the engine.
    /// Memory layout strictly matches the C++ RunStats struct.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    internal struct NativeRunStats
    {
        public int Generation;

        public double BestMse;
        public double BestR2;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 512)]
        public string BestEquation;

        public int ParetoFrontSize;
    }
}
