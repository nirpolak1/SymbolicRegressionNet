#nullable disable
using System;
using Xunit;
using SymbolicRegressionNet.Sdk.Api;
using SymbolicRegressionNet.Sdk.Interop;

namespace SymbolicRegressionNet.Sdk.Tests
{
    public class LoggingTests
    {
        [Fact]
        public void NativeLogCallback_InterceptsAndRaisesGlobalEvent()
        {
            bool eventFired = false;
            EngineLogEventArgs capturedArgs = null;

            EventHandler<EngineLogEventArgs> handler = (sender, args) =>
            {
                eventFired = true;
                capturedArgs = args;
            };

            SymbolicRegressor.GlobalEngineLog += handler;

            try
            {
                // We use Reflection to simulate the C++ engine calling back into the static delegate.
                var delegateField = typeof(SymbolicRegressor).GetField("_logCallbackDelegate", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                var callback = (Delegate)delegateField.GetValue(null);

                // Simulate Native engine logging an warning
                callback?.DynamicInvoke((int)EngineLogLevel.Warning, "Numerical instability detected in expression.");

                Assert.True(eventFired);
                Assert.NotNull(capturedArgs);
                Assert.Equal(EngineLogLevel.Warning, capturedArgs.Level);
                Assert.Equal("Numerical instability detected in expression.", capturedArgs.Message);
            }
            finally
            {
                SymbolicRegressor.GlobalEngineLog -= handler;
            }
        }
    }
}
