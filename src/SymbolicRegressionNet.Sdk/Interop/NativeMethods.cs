using System;
using System.Runtime.InteropServices;
using System.Text;

namespace SymbolicRegressionNet.Sdk.Interop
{
    /// <summary>
    /// Exposes P/Invoke signatures matching the C-API from srnet_core.
    /// </summary>
    internal static class NativeMethods
    {
        private const string DllName = "SymbolicRegressionNetCore";
        // Engine lifecycle
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr SRNet_CreateEngine(ref NativeOptions opts);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SRNet_DestroyEngine(IntPtr engine);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void NativeLogCallback(int level, [MarshalAs(UnmanagedType.LPStr)] string message);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SRNet_SetLogCallback(NativeLogCallback callback);


        // Data binding (legacy flat row-major)
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SRNet_SetData(IntPtr engine, IntPtr x_flat, IntPtr y, int rows, int cols);

        // Data binding (zero-copy column pointers)
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SRNet_SetDataColumns(IntPtr engine, IntPtr[] x_columns, IntPtr y, int rows, int cols);

        // Evolutionary loop
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SRNet_Step(IntPtr engine, int generations, out NativeRunStats stats);

        // Cancellation
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SRNet_SetCancelFlag(IntPtr engine, IntPtr cancelFlag);

        // Results retrieval
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SRNet_GetBestEquation(IntPtr engine, StringBuilder buf, int bufLen);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SRNet_GetPredictions(IntPtr engine, IntPtr outPreds, int nRows);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SRNet_GetHallOfFame(IntPtr engine, [Out] NativeRunStats[] outModels, int maxModels, out int outCount);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SRNet_GetLastError(StringBuilder buf, int bufLen);
    }
}
