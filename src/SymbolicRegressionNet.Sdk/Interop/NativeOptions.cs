using System;
using System.Runtime.InteropServices;

namespace SymbolicRegressionNet.Sdk.Interop
{
    /// <summary>
    /// Configuration options for the symbolic regression engine.
    /// Memory layout strictly matches the C++ Options struct (48 bytes).
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeOptions
    {
        public int PopulationSize;     // 4 bytes, offset 0
        public int MaxGenerations;     // 4 bytes, offset 4
        public int MaxTreeDepth;       // 4 bytes, offset 8
        public int TournamentSize;     // 4 bytes, offset 12

        public double CrossoverRate;   // 8 bytes, offset 16
        public double MutationRate;    // 8 bytes, offset 24

        public uint FunctionsMask;     // 4 bytes, offset 32
        public uint _pad0;             // 4 bytes, offset 36
        public ulong RandomSeed;       // 8 bytes, offset 40
    }
}
