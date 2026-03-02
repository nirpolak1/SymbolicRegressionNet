# Kanban Board

## Kanban Best Practices
This board is maintained by the AI team using the following rules:
1. **Visualize the Workflow**: All tasks flow through: *Tasks to do* -> *Tasks being done* -> *Tasks done* -> *Tasks reviewed*.
2. **Limit WIP (Work In Progress)**: A team member clocks in, takes exactly one task, completes it, and clocks out.
3. **Explicit Policies**: A developer cannot review their own code. QA must approve.
4. **Pull Mechanism**: Members "pull" work from the "Tasks to do" column when they clock in.

---

## Epics
- **[Epic 26] Native C++ AVX2 & CUDA Batch Evaluator Port**
  *Source:* Architectural Correction (Performance Engineer) | *Impact:* High | *Complexity:* High
  *Goal:* Port the C# `FastStackEvaluator` and `BatchEvaluator` interfaces directly into the `SymbolicRegressionNet.Core` C++ repository using intrinsic AVX2 and `.cu` CUDA kernels, ensuring maximum vectorization without C# Interop overhead.

- **[Epic 27] Native C++ Quasi-Newton L-BFGS Optimizer Port**
  *Source:* Architectural Correction (NumOpt Engineer) | *Impact:* High | *Complexity:* High
  *Goal:* Move the `LbfgsOptimizer.cs` constant snaper into the C++ native core to tightly couple gradient descent with the C++ AST representation.

---

## Tasks to do
## Tasks being done
- [ ] **[Architect]** Define the C++ headers for the `NativeLbfgsOptimizer`.
- [ ] **[NumOpt-Engineer]** Implement the L-BFGS gradient descent algorithm inside `lbfgs_optimizer.cpp`.
- [ ] **[API-Engineer]** Expose the C++ optimizer via P/Invoke and migrate the C# SDK to use it.
- [ ] **[QA-Engineer]** Run the SRBench suite to ensure the C++ ports maintain the R2 standards while bypassing C# GC.

---

## Tasks done
- [x] **[Architect]** Define the C++ headers and P/Invoke exports for the `NativeBatchEvaluator`.
- [x] **[Performance-Engineer]** Implement `avx2_evaluator.cpp` taking contiguous float arrays.
- [x] **[ML-Engineer]** Write the `cuda_evaluator.cu` kernel for high-throughput discrete evaluations.
- [x] **[API-Engineer]** Hook the .NET `RegressionBuilder` to load and pass arrays to the new C++ exports.

---

## Tasks reviewed
- [x] **[API-Engineer]** Hook tiered strategy into `SymbolicRegressor` options.
- [x] **[QA-Engineer]** Unit test tiered approach reduces dataset rows evaluated for bad candidates.
- [x] **[Architect]** Define `ITieredEvaluationStrategy` interface.
- [x] **[ML-Engineer]** Implement `SubsetTieredStrategy` doing 5% initial checks.
- [x] **[Architect]** Define token instruction set structs.
- [x] **[Performance-Engineer]** Build stack evaluator using `ArrayPool<double>`.
- [x] **[API-Engineer]** Expose zero-alloc evaluator as `FastStackEvaluator`.
- [x] **[QA-Engineer]** Unit test evaluating expressions with standard postfix notation.
- [x] **[Architect]** Define `ISelectionStrategy` interface.
- [x] **[ML-Engineer]** Implement `DoubleTournamentSelection` using expression complexity.
- [x] **[API-Engineer]** Update `SymbolicRegressor` to use custom selection logic.
- [x] **[QA-Engineer]** Write unit tests verifying parsimony selection behaviors.
- [x] **[API-Engineer]** Implement hook into `SymbolicRegressor` to use a custom evaluator.
- [x] **[Architect]** Define `IEvaluator.cs` and `EvaluationContext.cs`.
- [x] **[ML-Engineer]** Implement `FallbackCpuEvaluator.cs` as a structured reference.
- [x] **[Architect]** Update `GenerationReport` with timing metrics (`EstimatedTimeRemaining`, `EvaluationsPerSecond`).
- [x] **[Performance-Engineer]** Implement throughput calculation tracking logic.
- [x] **[API-Engineer]** Update `SymbolicRegressor` main loop to pass timestamp deltas into the telemetry handlers.
- [x] **[API-Engineer]** Expose `IReadOnlyList<FeatureImportance>` inside `RegressionResult` and print them nicely in a `ToString()` override.
## Tasks reviewed
- [x] **[QA-Engineer]** Write a mock test verifying ETA updates correctly over multiple generations.
- [x] **[API-Engineer]** Add `Scale()` method to `Dataset` tying the pipeline together seamlessly.
- [x] **[QA-Engineer]** Write unit tests to assert test distributions correctly use training scale factors.
- [x] **[Architect]** Define the `IDataScaler` contract for fitting and transforming `Dataset`s.
- [x] **[ML-Engineer]** Implement `StandardScaler` that learns and stores parameter vectors during `Fit()`.
- [x] **[API-Engineer]** Update `SymbolicRegressor` to populate metrics using `TrainDataset.Rows` and expose `BestModelByAic` / `BestModelByBic` on `HallOfFame`.
- [x] **[QA-Engineer]** Write unit tests to assert the mathematical correctness of AIC and BIC.
- [x] **[Architect]** Update `DiscoveredModel` to include `Aic` and `Bic` properties.
- [x] **[NumOpt-Engineer]** Implement AIC and BIC calculation logic based on MSE, model complexity, and dataset row count.
- [x] **[QA-Engineer]** Write unit tests to assert the correct variables receive the highest importance fraction.
- [x] **[Architect]** Define the `IFeatureImportanceCalculator` contract and `FeatureImportance` struct.
- [x] **[ML-Engineer]** Implement `FrequencyBasedImportanceCalculator` that extracts explicit Variable tokens `x0, x1...` from `HallOfFame` expressions and computes frequency weighted by R2 score.
- [x] **[Architect]** Define the `IExpressionExporter` contract and placement within `SymbolicRegressionNet.Sdk` **(Epic 1)**
- [x] **[API-Engineer]** Implement `LatexExporter`, `PythonExporter`, and `CCodeExporter` classes based on the contract **(Epic 1)**
- [x] **[QA-Engineer]** Scaffold the `tests/SymbolicRegressionNet.Sdk.Tests` project and write unit tests for the exporters **(Epic 1)**
