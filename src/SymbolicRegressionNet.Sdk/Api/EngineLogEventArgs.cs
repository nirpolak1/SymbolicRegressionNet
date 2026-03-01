using System;

namespace SymbolicRegressionNet.Sdk.Api
{
    public enum EngineLogLevel
    {
        Trace = 0,
        Debug = 1,
        Information = 2,
        Warning = 3,
        Error = 4,
        Critical = 5,
        None = 6
    }

    public class EngineLogEventArgs : EventArgs
    {
        public EngineLogLevel Level { get; }
        public string Message { get; }

        public EngineLogEventArgs(EngineLogLevel level, string message)
        {
            Level = level;
            Message = message;
        }
    }
}
