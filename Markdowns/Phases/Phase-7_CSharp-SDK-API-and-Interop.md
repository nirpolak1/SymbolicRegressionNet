# Phase 7 — C# SDK: API & Interop

> **Phase Goal:** Build the public-facing C# API — the fluent builder, the orchestrating `SymbolicRegressor` class, P/Invoke declarations, and result types — that lets users run symbolic regression in three lines of code while the C++ core does the heavy lifting underneath.

---

## Overview

Phase 7 is where SymbolicRegressionNet becomes a **product**. Everything built in Phases 1-6 is infrastructure — powerful, but invisible to the end user. Phase 7 wraps it all in a clean, documented, IntelliSense-friendly C# API.

This phase implements five interconnected components:

1. **`NativeMethods`** — P/Invoke declarations matching Phase 5's C-API.
2. **`RegressionBuilder`** — Fluent configuration builder.
3. **`SymbolicRegressor`** — Engine lifecycle orchestrator with async support.
4. **`GenerationReport`** — Per-generation progress telemetry.
5. **`HallOfFame`** — Pareto front of discovered expressions.

---

## Why This Phase Exists

The C++ core communicates through `void*` handles and raw `double*` pointers. Without Phase 7, using the engine from C# would require 50+ lines of boilerplate: marshaling structs, pinning arrays, checking return codes, and freeing handles. This phase transforms the engine from a C++ library into a **.NET library** with sensible defaults, XML docs, async/await, and `IDisposable`.

---

## Dependencies

| Depends On | Description |
|-----------|-------------|
| **Phase 1** | C# project structure, NuGet dependencies |
| **Phase 2** | `Options` and `RunStats` struct definitions (must match C#/C++) |
| **Phase 5** | C-API function signatures |
| **Phase 6** | `Dataset`, `PinnedBuffer<T>`, and `Splitter` |

### What Depends on Phase 7

| Phase | Dependency Reason |
|-------|-------------------|
| **Phase 8** (Verification) | End-to-end tests exercise the SDK API |

---

## Tasks

### Task 7.1 — Define P/Invoke Signatures in `NativeMethods.cs`

> **Assigned to:** `API-Engineer`

**Description:**
Declare managed P/Invoke signatures matching Phase 5's `extern "C"` API. These declarations must have **identical** memory layouts for all struct parameters.

**P/Invoke declarations include:**
- `SRNet_CreateEngine`, `SRNet_DestroyEngine` — engine lifecycle
- `SRNet_SetData` — binding pinned dataset pointers
- `SRNet_Step` — running generations with `RunStats` output
- `SRNet_GetBestEquation` — retrieving the best expression string
- `SRNet_GetPredictions` — evaluating the best model
- `SRNet_GetHallOfFame` — retrieving Pareto-optimal models

**Marshaled structs (`NativeOptions`, `NativeRunStats`):**
Must use `[StructLayout(LayoutKind.Sequential)]` with field order exactly matching C++. Key mappings:
- `int` (C#) = `int` (C++, 4 bytes)
- `double` (C#) = `double` (C++, 8 bytes)  
- `uint` / `ulong` (C#) = `uint32_t` / `uint64_t` (C++)
- `[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 512)]` = `char[512]` (C++)

**Why critical:** A single mismatched field causes silent memory corruption. No type checker catches cross-language struct mismatches.

**Relies on:** Phase 5 (`api.h`), Phase 2 (`Options`, `RunStats`).

---

### Task 7.2 — Implement `RegressionBuilder` (Fluent Builder Pattern)

> **Assigned to:** `API-Engineer`

**Description:**
Implement the fluent builder collecting user configuration and producing a `SymbolicRegressor`.

**Fluent methods:** `WithData()`, `SplitData()`, `WithPopulationSize()`, `WithMaxGenerations()`, `WithMaxTreeDepth()`, `WithTournamentSize()`, `WithCrossoverRate()`, `WithMutationRate()`, `WithRandomSeed()`, `WithFunctions()`, `WithTimeLimit()`, `Build()`.

**Sensible defaults:**

| Parameter | Default | Rationale |
|-----------|---------|-----------|
| PopulationSize | 200 | Balance diversity/speed |
| MaxGenerations | 50 | Sufficient for simple problems |
| MaxTreeDepth | 6 | Prevents bloat |
| TournamentSize | 7 | Standard GP |
| CrossoverRate | 0.9 | Standard GP |
| MutationRate | 0.1 | Complementary operator |
| FunctionsMask | All | Full operator set |

**Validation in `Build()`:** Dataset set, sizes > 0, rates ∈ [0,1], depth ∈ [2,20]. Throws `ArgumentException` with specific messages.

**Why this matters:** The builder is the primary configuration interface. Poor defaults or missing validation cause confusion, poor results, or crashes.

**Relies on:** Task 7.1 (builds `NativeOptions`), Phase 6 (`Dataset`, `SplitStrategy`).

---

### Task 7.3 — Implement `SymbolicRegressor` (Orchestrator)

> **Assigned to:** `Architect`

**Description:**
Implement the orchestrator managing the native engine lifecycle: create → set data → step → results → destroy.

**Key design decisions:**
- **`async Task<RegressionResult> FitAsync(CancellationToken, IProgress<GenerationReport>)`** — runs the search on a background thread, supports cancellation and per-generation progress.
- **Single-generation stepping from C#:** Steps one generation at a time to enable progress reporting and cancellation without modifying the C++ core.
- **`IDisposable`:** Owns native engine handle and pinned data handles. Users use `using var regressor = builder.Build();`.
- Constructor pins data, creates native engine, sets data via P/Invoke. `Dispose()` destroys engine and releases pins.

**Why this matters:** This class ties everything together — data loading (Phase 6), configuration (Task 7.2), native execution (Phase 5), and result reporting (Task 7.4). A leaked handle or premature unpin affects the entire user experience.

**Relies on:** Task 7.1, Task 7.2, Phase 6 (`Dataset.Pin()`).

---

### Task 7.4 — Implement Result Types (`GenerationReport`, `HallOfFame`)

> **Assigned to:** `API-Engineer`

**Description:**
Implement immutable, well-documented result types exposing the engine's output.

**`GenerationReport`:** A `record` with `Generation`, `BestMse`, `BestR2`, `BestEquation`, `ParetoFrontSize`. Reported via `IProgress<T>` during `FitAsync`.

**`DiscoveredModel`:** A `record` with `Expression`, `Mse`, `R2`, `Complexity`.

**`HallOfFame`:** Implements `IReadOnlyList<DiscoveredModel>` with helpers:
- `GetBestByComplexity(int maxNodes)` — best model under complexity budget
- `ExportCsv()` — export Pareto front as CSV
- `Best` — highest R² model

**`RegressionResult`:** Contains `BestExpression`, `HallOfFame`, `GenerationsRun`, `ElapsedTime`.

All types are immutable (`record`, `init` setters), fully XML-documented, and IntelliSense-friendly.

**Relies on:** Task 7.1 (`NativeRunStats` → `DiscoveredModel` mapping), Phase 5 (`SRNet_GetHallOfFame`).

---

### Task 7.5 — Implement Telemetry Pipeline

> **Assigned to:** `API-Engineer`

**Description:**
Extend beyond basic `IProgress<T>` to support three consumer patterns:

1. **`IProgress<GenerationReport>`** — standard .NET progress (UI progress bars, logging).
2. **`IObservable<GenerationReport>`** — reactive stream for Rx consumers (LINQ-style generation queries).
3. **`Action<GenerationReport>`** callback — lightweight consumers.

The regressor maintains a subscriber list and invokes all after each generation. This maximizes SDK usability across console apps, WPF/Avalonia, and logging frameworks.

**Relies on:** Task 7.3, Task 7.4.

---

## Phase Completion Criteria

| Criterion | Verification |
|-----------|-------------|
| Struct layouts match C++ `sizeof()` | Cross-language sizeof comparison |
| Builder validates all parameters | Unit tests with invalid configs |
| Builder defaults produce a working regressor | Integration test |
| `FitAsync` reports per-generation progress | Integration test with `IProgress` |
| `FitAsync` respects `CancellationToken` | Cancellation test |
| `Dispose()` frees native engine | Memory check |
| `HallOfFame.ExportCsv()` produces valid CSV | Content validation |
| Full round-trip: CSV → Dataset → Builder → Fit → Result | End-to-end test |

---

## Summary

Phase 7 completes the SDK by wrapping the C++ core in idiomatic C#. The P/Invoke layer bridges languages, the builder provides ergonomic config, the regressor orchestrates execution, and result types present discoveries in a developer-friendly format. This is what transforms a research engine into an adoptable library.
