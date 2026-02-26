# SymbolicRegressionNet: Detailed API & Class Documentation

This document provides a detailed breakdown of the main classes, structures, and methods in both the C++ Core and the C# SDK.

---

## 1. C# SDK (`SymbolicRegressionNet.Sdk`)

The C# SDK provides the managed interface for users to ingest data, configure the regression, and execute the native engine asynchronously.

### 1.1 `Dataset` (`SymbolicRegressionNet.Sdk.Data.Dataset`)
A zero-copy, highly optimized structure for holding tabular data. Data is stored in a column-major format (Struct-of-Arrays).

**Key Methods & Properties:**
*   `FromCsv(string filePath, bool hasHeader, MissingValueStrategy...)`: Parses a CSV file into a `Dataset`. Handles missing values via strategies like `DropRow` or `ReplaceWithMean`.
*   `FromArray(double[,] features, double[] target)`: Instantiates a dataset straight from memory arrays.
*   `WithTarget(string columnName)`: Fluently sets the target variable column for regression.
*   `Drop(params string[] columnNames)`: Returns a new `Dataset` view that excludes the specified columns.
*   `Normalize(NormalizationMethod method)`: Normalizes feature columns in place and returns a new view. Supports `ZScore` and `MinMax`.
*   `Pin()`: **[Crucial for Performance]** Pins the managed arrays in memory using `GCHandle`. Returns a `PinnedData` object containing pointers. This allows C++ to read C# arrays directly without copying.

### 1.2 `RegressionBuilder`
A fluent builder pattern class used to configure the hyperparameters of the genetic programming engine.

**Key Methods:**
*   `WithData(Dataset dataset)`: Sets the primary training data.
*   `SplitData(float testRatio)`: Automatically splits the loaded data into a training and validation set.
*   `WithPopulationSize(int size)`, `WithMaxGenerations(int generations)`: Control the scale of the evolutionary search.
*   `WithMaxTreeDepth(int depth)`: Constrains the complexity of generated equations.
*   `WithTournamentSize(int size)`: Sets the pressure for parent selection.
*   `WithCrossoverRate(double rate)`, `WithMutationRate(double rate)`: Evolutionary probabilities.
*   `Build()`: Validates all inputs and instantiates the `SymbolicRegressor`.

### 1.3 `SymbolicRegressor`
The main orchestrator. It manages the lifetime of the native C++ engine via `IntPtr`.

**Key Methods:**
*   `FitAsync(CancellationToken token, IProgress<GenerationReport> progress)`: Runs the evolutionary loop on a background thread. It steps the unmanaged engine generation by generation, emitting telemetry and checking for cancellation.
*   `GetHallOfFame()`: Calls into the native engine to retrieve the Age-Fitness Pareto front (the best models discovered, balancing accuracy against complexity).

---

## 2. C++ Core (`SymbolicRegressionNet.Core`)

The C++ Core handles the performance-critical evolutionary loop and expression evaluation. 

### 2.1 Core Data Structures (`types.h` / `tree.h`)

*   `DataView`: A Struct-of-Arrays (SoA) layout containing raw `double*` pointers received from C#. It enables coalesced memory access and vectorization (e.g., AVX2).
*   `Options`: A 48-byte struct containing hyperparameter configurations. Its memory layout perfectly mirrors the C# `NativeOptions` struct for seamless `P/Invoke` marshaling.
*   `RunStats`: A fixed-size struct used to pass generational statistics (like best MSE, R², and the best equation string) back to C#.
*   `Token` / `Individual`: Mathematical syntax trees are completely flattened into postfix arrays of `Token`s. `Individual` encapsulates this flattened token array along with its evaluated fitness (MSE), age, and complexity.

### 2.2 `Engine`
The central state machine for the evolutionary process.

**Key Methods:**
*   `Initialize()`: Initializes the first generation randomly using the "Ramped Half-and-Half" method.
*   `Step(int generations)`: Executes the main generational loop: Evaluate -> Select Survivors -> Select Parents -> Crossover -> Mutate. Can be cleanly interrupted by a thread-safe atomic cancellation flag sent from C#.
*   `GetParetoFront()`: Extracts the current non-dominated individuals to return to the user.

### 2.3 `evaluator` Namespace
Responsible for blazingly fast calculation of a mathematical expression against thousands of data rows.

**Key Functions:**
*   `EvaluateExpression(const Token* genome, length, row, numVars)`: Postfix scalar interpreter used as a fallback or for single-row evaluations.
*   `EvaluateIndividual(Individual& ind, const DataView& data)`: Evaluates a single model against all data rows. Computes Kahan summation to prevent floating-point catastrophic cancellation when summing large squared errors.
*   **SIMD Evaluator** (`EvaluateExpression_AVX2`): When compiled with AVX2 support, evaluates expressions on 4 data rows *simultaneously* using 256-bit registers (`__m256d`). This multiplies evaluation throughput almost fourfold.

### 2.4 `optimizer` Namespace
Houses numerical optimization algorithms used to magically tune the numerical constants within generated expressions (e.g., turning $c_1 x + c_2$ into $3.14 x - 0.5$).

**Key Classes:**
*   `IConstantOptimizer`: Abstract interface for constant tuning.
*   `LevenbergMarquardtOptimizer`: Implements the Levenberg-Marquardt algorithm. It combines gradient descent and the Gauss-Newton method to perform non-linear least squares optimization of the constants embedded in an `Individual`'s expression tree. Requires gradients, which are supplied by an Automatic Differentiation evaluator.
