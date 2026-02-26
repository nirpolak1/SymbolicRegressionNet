# Phase 6 — C# SDK: Data Layer

> **Phase Goal:** Implement the managed data layer that ingests user data (CSV files, in-memory arrays, columnar data), prepares it for zero-copy transfer to the native C++ core, and provides train/validation/test splitting strategies — bridging the gap between user-friendly .NET APIs and the raw pointers the C++ engine consumes.

---

## Overview

Phase 6 marks the transition from the unmanaged C++ world to the managed C# world. While Phases 2-5 built the high-performance engine, this phase builds the **data on-ramp** — the components that accept data in familiar .NET formats and prepare it for the native evaluator.

The central engineering challenge is **zero-copy interop**. The C++ core expects raw `double*` pointers (via the `DataView` struct), but C# manages memory through a garbage collector that moves objects in memory unpredictably. The solution is **pinning**: using `GCHandle.Alloc(..., GCHandleType.Pinned)` to lock managed arrays in place, obtaining stable `IntPtr` addresses that can be passed to C++ through P/Invoke.

This phase must also handle the ergonomic concerns of data loading: CSV parsing with automatic type detection, column selection, normalization, and splitting — features that save the end-user dozens of lines of boilerplate.

---

## Why This Phase Exists

### The Problem It Solves

Users of the SymbolicRegressionNet SDK work in C#/.NET. They have:
- CSV files from Excel or data engineering pipelines.
- `double[,]` arrays from numerical computing.
- DataFrame-like column collections from data science workflows.

None of these formats are directly consumable by the C++ engine, which expects contiguous column-major `double*` arrays. Without Phase 6, users would need to manually:
1. Parse their CSV into jagged double arrays.
2. Transpose from row-major to column-major.
3. Pin each array with `GCHandle`.
4. Extract `IntPtr` pointers and pass them through P/Invoke.
5. Hold GC handles for the entire engine lifecycle.
6. Dispose handles on cleanup.

This is complex, error-prone, and violates the SDK's design goal of simplicity (`new SymbolicRegressor().Fit(X, y)` with zero boilerplate).

### Its Purpose in the Grand Scheme

The data layer is the **first touch point** between users and the engine. It determines:
- **How easy** it is to get started (can users go from CSV to results in 3 lines of code?).
- **How fast** data transfer is (zero-copy pinning avoids a `Marshal.Copy` that would double memory usage).
- **How safe** interop is (pinned handles that outlive the engine cause memory leaks; handles freed too early cause use-after-free crashes).

Phase 7's `SymbolicRegressor` builds on this data layer — it calls `Dataset.Pin()` to prepare data before passing it to the C-API.

---

## Dependencies

| Depends On | Description |
|-----------|-------------|
| **Phase 1** | C# project file (`.csproj`) and project structure |
| **Phase 2** | `DataView` struct definition (to understand what the C++ side expects) |
| **Phase 5** | C-API `SRNet_SetData` function signature (to know how data is passed to the engine) |

### What Depends on Phase 6

| Phase | Dependency Reason |
|-------|-------------------|
| **Phase 7** (API & Interop) | `SymbolicRegressor` uses `Dataset` and `PinnedBuffer` to prepare data for the engine |
| **Phase 8** (Verification) | End-to-end tests load data via the Dataset class |

---

## Tasks

### Task 6.1 — Implement the `Dataset` Class

> **Assigned to:** `API-Engineer`

**Description:**
Implement the `Dataset` class — the primary data container for users of the SDK. It provides multiple factory methods for data ingestion, fluent methods for column manipulation, and a `Pin()` method that locks the data in memory for native access.

**Public API:**

```csharp
public sealed class Dataset : IDisposable
{
    // ─── Factory Methods ───
    public static Dataset FromCsv(string filePath, bool hasHeader = true);
    public static Dataset FromArray(double[,] features, double[] target);
    public static Dataset FromColumns(
        IReadOnlyDictionary<string, double[]> columns, string targetColumn);

    // ─── Fluent Configuration ───
    public Dataset WithTarget(string columnName);
    public Dataset Drop(params string[] columnNames);
    public Dataset Normalize(NormalizationMethod method = NormalizationMethod.ZScore);

    // ─── Properties ───
    public int Rows { get; }
    public int FeatureCount { get; }
    public IReadOnlyList<string> FeatureNames { get; }
    public string TargetName { get; }

    // ─── Interop ───
    public PinnedData Pin();  // Returns pinned pointers for C++ consumption
}
```

**`FromCsv` implementation details:**
- Parse the file line-by-line, detecting delimiters (comma, tab, semicolon).
- First row is treated as header if `hasHeader` is true; otherwise, columns are named `x0, x1, ..., xN`.
- All columns must be parseable as `double`. Non-numeric columns throw a descriptive `FormatException`.
- Missing values (empty cells, "NA", "NaN") are handled per a configurable strategy: `ThrowOnMissing` (default), `ReplaceWithMean`, `ReplaceWithMedian`, or `DropRow`.
- Internally store data in **column-major order** (`double[][] columns`) to match the `DataView` SoA layout expected by C++.

**`FromArray` implementation details:**
- Accept a row-major `double[,]` feature matrix and a `double[]` target vector.
- Validate that `features.GetLength(0) == target.Length`.
- Transpose to column-major storage internally.

**`Pin()` — the critical interop bridge:**
Returns a `PinnedData` record (or struct) containing:
- Array of `IntPtr` pointers, one per feature column (pinned `double[]` arrays).
- An `IntPtr` for the target column.
- Row and column counts.
- Implements `IDisposable` to release GC handles when the engine is done.

**Why this task matters:**
The `Dataset` class is the first thing users interact with. A poor CSV parser, a confusing column API, or a buggy `Pin()` implementation will frustrate users before they even reach the symbolic regression result. More critically, the `Pin()` mechanism must be **airtight** — holding GC handles longer than necessary wastes memory and degrades GC performance; releasing them too early causes segmentation faults in the C++ core.

**Relies on:** Phase 1 (C# project), Task 6.2 (`PinnedBuffer<T>` is used internally).

---

### Task 6.2 — Implement `PinnedBuffer<T>` for Zero-Copy Interop

> **Assigned to:** `Perf-Engineer`

**Description:**
Implement a generic, disposable wrapper around `GCHandle.Alloc(array, GCHandleType.Pinned)` that provides safe, controlled access to a managed array's raw memory address.

**Implementation:**

```csharp
public sealed class PinnedBuffer<T> : IDisposable where T : unmanaged
{
    private readonly T[] _array;
    private GCHandle _handle;
    private bool _disposed;

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

    public void Dispose()
    {
        if (!_disposed)
        {
            _handle.Free();
            _disposed = true;
        }
    }
}
```

**Design rationale:**

- **`where T : unmanaged` constraint:** Ensures only blittable types can be pinned. `double`, `int`, `float` work; `string`, `object` do not. This prevents runtime `ArgumentException` from `GCHandle.Alloc`.
- **`IDisposable` pattern:** GC handles are **unmanaged resources** — they prevent the GC from compacting the heap. If not freed, they cause heap fragmentation over time. The `Dispose` pattern ensures deterministic cleanup, ideally through a `using` statement.
- **No finalizer:** Unlike typical unmanaged resource wrappers, `PinnedBuffer` intentionally omits a finalizer. Finalizers add cost to the GC and can cause ordering issues. Instead, the design relies on `SymbolicRegressor.Dispose()` (Phase 7) to cascade disposal.
- **`Span<T>` access:** Allows C# code to read/write the underlying data without going through `IntPtr` — useful for populating the array before pinning or reading results after evaluation.

**Why this task matters:**
`PinnedBuffer<T>` is the foundational interop primitive for the entire C# SDK. Every piece of data that crosses the C++/C# boundary goes through a pinned buffer. If pinning is done wrong (e.g., pinning inside a `fixed` block that goes out of scope), the C++ engine will read corrupted memory. If handles are leaked, the GC's compaction is impaired across the entire application.

**Relies on:** Phase 1 (C# project with `AllowUnsafeBlocks`).

---

### Task 6.3 — Implement Data Splitting Strategies

> **Assigned to:** `API-Engineer`

**Description:**
Implement a set of data splitting strategies that partition a `Dataset` into training, validation, and optionally test subsets. Splits must be efficient (no data copying — use index-based views) and reproducible (deterministic given a seed).

**`SplitStrategy` hierarchy:**

```csharp
public abstract record SplitStrategy;

public sealed record RandomSplit(
    double TrainRatio = 0.8,
    int Seed = 42
) : SplitStrategy;

public sealed record TimeSeriesSplit(
    string TimeColumn
) : SplitStrategy;

public sealed record KFoldSplit(
    int K = 5,
    int Seed = 42
) : SplitStrategy;
```

**`Splitter` class:**

```csharp
public static class Splitter
{
    public static (Dataset Train, Dataset Validation) Split(
        Dataset dataset, SplitStrategy strategy);

    public static IEnumerable<(Dataset Train, Dataset Validation)> CrossValidate(
        Dataset dataset, KFoldSplit strategy);
}
```

**`RandomSplit` implementation:**
1. Generate a permutation of row indices `[0, 1, ..., N-1]` using a seeded `Random`.
2. Take the first `TrainRatio × N` indices as training; the rest as validation.
3. Create two `Dataset` views that reference the same underlying column arrays but only expose the selected row indices. This is **zero-copy** — no data is duplicated.

**`TimeSeriesSplit` implementation:**
1. Sort row indices by the values in the specified time column.
2. Use the first `TrainRatio` fraction for training, the remainder for validation.
3. No shuffling — temporal order is preserved to prevent data leakage.

**`KFoldSplit` implementation:**
1. Shuffle row indices with a seeded `Random`.
2. Partition into `K` roughly equal folds.
3. Yield `K` iterations, each using one fold as validation and the other `K-1` folds as training.

**Why this task matters:**
Proper data splitting is essential for evaluating model generalization. Training and validation on the same data produces overfitting — the model memorizes the data rather than discovering the underlying pattern. Time-series splitting prevents future-data leakage. K-fold cross-validation provides robust performance estimates. Without these utilities, users must implement splitting themselves, risking subtle data leakage bugs.

**Relies on:** Task 6.1 (`Dataset` class must exist and support index-based views).

---

## Phase Completion Criteria

| Criterion | Verification |
|-----------|-------------|
| `FromCsv` parses a standard CSV with header correctly | Unit test |
| `FromArray` transposes row-major to column-major | Unit test comparing values |
| `PinnedBuffer<double>` provides a stable `IntPtr` | Test: pin, pass to unmanaged, verify data matches |
| `PinnedBuffer.Dispose()` frees the GC handle | No leaked handles after `using` block |
| `RandomSplit` produces reproducible splits with the same seed | Equality test |
| `TimeSeriesSplit` preserves temporal order | Sorted order assertion |
| `KFoldSplit` covers all rows exactly once per fold | Coverage assertion |
| No data copying during splits | Memory usage verification |

---

## Summary

Phase 6 builds the data layer that makes SymbolicRegressionNet usable from C#. The `Dataset` class provides ergonomic data ingestion, `PinnedBuffer<T>` provides safe zero-copy interop with the native engine, and the splitting strategies provide rigorous experiment setup. This phase transforms raw user data into the exact memory layout the C++ core expects — bridging managed .NET conventions with the unmanaged pointers required for maximum performance.
