using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SymbolicRegressionNet.Sdk.Data;
using SymbolicRegressionNet.Sdk.Interop;

namespace SymbolicRegressionNet.Sdk
{
    /// <summary>
    /// Orchestrator for the symbolic regression engine.
    /// Manages native engine lifetime, data pinning, and async execution.
    /// </summary>
    public sealed class SymbolicRegressor : IDisposable, IObservable<GenerationReport>
    {
        internal Dataset TrainDataset { get; }
        internal Dataset ValidationDataset { get; }
        internal NativeOptions Options { get; }
        internal TimeSpan? TimeLimit { get; }

        private IntPtr _engine = IntPtr.Zero;
        private readonly PinnedData _pinnedTrain;

        // Telemetry handlers
        private readonly List<IProgress<GenerationReport>> _progressHandlers = new();
        private readonly List<Action<GenerationReport>> _actionHandlers = new();
        private readonly List<IObserver<GenerationReport>> _observers = new();

        internal SymbolicRegressor(Dataset train, Dataset val, NativeOptions options, TimeSpan? timeLimit)
        {
            TrainDataset = train ?? throw new ArgumentNullException(nameof(train));
            ValidationDataset = val;
            Options = options;
            TimeLimit = timeLimit;

            // 1. Create native engine
            var opts = Options;
            _engine = NativeMethods.SRNet_CreateEngine(ref opts);
            if (_engine == IntPtr.Zero)
                throw new InvalidOperationException("Failed to create native engine.");

            // 2. Pin data (columns are individually pinned for zero-copy)
            _pinnedTrain = TrainDataset.Pin();

            // 3. Set data in C++ using zero-copy column pointers
            int rows = train.Rows;
            int cols = train.FeatureCount;
            NativeMethods.SRNet_SetDataColumns(_engine, _pinnedTrain.FeaturePointers, _pinnedTrain.TargetPointer, rows, cols);
        }

        /// <summary>
        /// Runs the symbolic regression search asynchronously.
        /// </summary>
        public async Task<RegressionResult> FitAsync(CancellationToken cancellationToken = default, IProgress<GenerationReport> progress = null)
        {
            if (_engine == IntPtr.Zero) throw new ObjectDisposedException(nameof(SymbolicRegressor));

            return await Task.Run(() =>
            {
                var startTime = DateTime.UtcNow;
                int generationsRun = 0;

                // Pin a cancel flag int and pass its address to C++ for mid-generation cancellation
                int[] cancelFlag = new int[] { 0 };
                using var pinnedCancel = new PinnedBuffer<int>(cancelFlag);
                NativeMethods.SRNet_SetCancelFlag(_engine, pinnedCancel.Pointer);

                // Register callback: when the token is cancelled, set the flag to 1
                using var registration = cancellationToken.Register(() => cancelFlag[0] = 1);

                for (int i = 0; i < Options.MaxGenerations; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    if (TimeLimit.HasValue && (DateTime.UtcNow - startTime) > TimeLimit.Value)
                    {
                        cancelFlag[0] = 1; // Signal C++ to stop mid-generation too
                        break;
                    }

                    // Step one generation
                    NativeMethods.SRNet_Step(_engine, 1, out NativeRunStats stats);
                    generationsRun++;

                    var report = new GenerationReport(
                        stats.Generation, 
                        stats.BestMse, 
                        stats.BestR2, 
                        stats.BestEquation, 
                        stats.ParetoFrontSize);

                    progress?.Report(report);
                    NotifyTelemetry(report);
                }

                // Clean up: unset the cancel flag pointer
                NativeMethods.SRNet_SetCancelFlag(_engine, IntPtr.Zero);

                foreach (var obs in _observers) obs.OnCompleted();
                return FinalizeResult(generationsRun, DateTime.UtcNow - startTime);

            }, cancellationToken);
        }

        /// <summary>
        /// Retrieves the current Hall of Fame (Pareto front) from the native engine.
        /// </summary>
        public HallOfFame GetHallOfFame()
        {
            if (_engine == IntPtr.Zero) return new HallOfFame();

            NativeMethods.SRNet_GetHallOfFame(_engine, null, 0, out int count);
            var hofStats = new NativeRunStats[count];
            if (count > 0)
            {
                NativeMethods.SRNet_GetHallOfFame(_engine, hofStats, count, out count);
            }

            var hof = new HallOfFame();
            for (int i = 0; i < count; i++)
            {
                hof.Add(new DiscoveredModel(
                    hofStats[i].BestEquation,
                    hofStats[i].BestMse,
                    hofStats[i].BestR2,
                    Complexity: i // Proxy for complexity, real implementation will parse or compute
                ));
            }
            return hof;
        }

        private RegressionResult FinalizeResult(int generationsRun, TimeSpan elapsed)
        {
            var hof = GetHallOfFame();

            var sb = new StringBuilder(512);
            NativeMethods.SRNet_GetBestEquation(_engine, sb, 512);

            return new RegressionResult(sb.ToString(), hof, generationsRun, elapsed);
        }

        // Telemetry registration
        public void AddProgressHandler(IProgress<GenerationReport> handler) => _progressHandlers.Add(handler);
        public void AddActionHandler(Action<GenerationReport> handler) => _actionHandlers.Add(handler);

        public IDisposable Subscribe(IObserver<GenerationReport> observer)
        {
            if (!_observers.Contains(observer))
                _observers.Add(observer);
            return new Unsubscriber(_observers, observer);
        }

        private void NotifyTelemetry(GenerationReport report)
        {
            foreach (var h in _progressHandlers) h.Report(report);
            foreach (var a in _actionHandlers) a(report);
            foreach (var o in _observers) o.OnNext(report);
        }

        public void Dispose()
        {
            if (_engine != IntPtr.Zero)
            {
                NativeMethods.SRNet_DestroyEngine(_engine);
                _engine = IntPtr.Zero;
            }
            
            _pinnedTrain?.Dispose();
        }

        private sealed class Unsubscriber : IDisposable
        {
            private readonly List<IObserver<GenerationReport>> _observers;
            private readonly IObserver<GenerationReport> _observer;

            public Unsubscriber(List<IObserver<GenerationReport>> observers, IObserver<GenerationReport> observer)
            {
                _observers = observers;
                _observer = observer;
            }

            public void Dispose()
            {
                if (_observer != null && _observers.Contains(_observer))
                    _observers.Remove(_observer);
            }
        }
    }
}
