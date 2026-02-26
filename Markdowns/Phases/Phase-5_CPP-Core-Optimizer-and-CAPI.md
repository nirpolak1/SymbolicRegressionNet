# Phase 5 — C++ Core: Optimizer & C-API

> **Phase Goal:** Implement the Levenberg-Marquardt constant optimization engine (with forward-mode automatic differentiation), define the `extern "C"` API that exposes the engine to foreign callers, and create the CUDA kernel stub for future GPU-accelerated evaluation.

---

## Overview

Phase 5 is a **triple-purpose phase** that bridges internal algorithms to external consumers:

1. **Constant Optimization (Levenberg-Marquardt):** Symbolic regression finds the *structure* of an expression (e.g., `a * sin(b * x + c)`), but the constants `a`, `b`, `c` are initially random. The optimizer fine-tunes these constants using nonlinear least-squares fitting, transforming a structurally promising but numerically mediocre expression into a highly accurate one. This is often the difference between R²=0.7 and R²=0.99.

2. **C-API:** The C++ core is a standalone native library. To be consumed by the C# SDK (Phase 7) — or any other language — it needs a stable, C-linkage API with `extern "C"` functions. This API is the contract between the two halves of the system.

3. **CUDA Stub:** A placeholder for GPU-accelerated batch evaluation. The stub defines the kernel launch function signatures and thread mapping strategy without implementing the kernel body — establishing the interface that a future GPU implementation will fill.

---

## Why This Phase Exists

### The Problem It Solves

**Constant Optimization:**
Genetic operators (Phase 3) manipulate expression *structure* — they swap subtrees, change operators, introduce new random constants. But they don't have the precision to fine-tune numerical constants. Consider `2.1 * x + 4.7` versus `2.0 * x + 5.0`: the first has MSE = 0.3; the second has MSE = 0.0 (on a particular dataset). GP might discover the second structure through mutation, but it's far more efficient to discover `a * x + b` and let a numerical optimizer find `a = 2.0, b = 5.0`.

Without constant optimization, GP must discover exact (or near-exact) constant values through random variation alone — essentially searching a continuous space with discrete tools. This is catastrophically inefficient and is the #1 quality bottleneck in naive symbolic regression.

**C-API:**
Without a stable C-API, the C# SDK (Phase 7) has no way to create engines, set data, run generations, or retrieve results. The C++ core would be a standalone executable rather than a library component in a larger system.

**CUDA Stub:**
Defining the GPU interface now — even as a stub — ensures that the engine's evaluation pipeline can dispatch to either CPU or GPU evaluation without architectural changes later.

### Its Purpose in the Grand Scheme

Constant optimization takes an expression from "structurally interesting" to "numerically optimal," dramatically amplifying the quality of GP's output. The C-API is the hinge that connects the high-performance C++ engine to the user-friendly C# SDK. The CUDA stub is a forward investment that preserves the option for 10-100× evaluation speedups without requiring Phase 3 or 4 to be redesigned.

---

## Dependencies

| Depends On | Description |
|-----------|-------------|
| **Phase 1** | Build system, CUDA cmake rules |
| **Phase 2** | `Token`, `Individual`, `Options`, `RunStats`, `DataView` types |
| **Phase 3** | `Engine` class (the C-API wraps it) |
| **Phase 4** | Evaluator (the optimizer uses expression evaluation + AutoDiff for Jacobian computation) |

### What Depends on Phase 5

| Phase | Dependency Reason |
|-------|-------------------|
| **Phase 7** (API & Interop) | `NativeMethods.cs` P/Invoke declarations mirror the C-API signatures defined here |
| **Phase 8** (Verification) | End-to-end tests call through the C-API; build verification includes CUDA stub compilation |

---

## Tasks

### Task 5.1 — Implement Levenberg-Marquardt Constant Optimization Skeleton

> **Assigned to:** `NumOpt-Engineer`

**Description:**
Implement the Levenberg-Marquardt (LM) algorithm for optimizing the real-valued constants embedded in an expression tree. LM is a hybrid method that interpolates between gradient descent (when far from the optimum) and Gauss-Newton's method (when near the optimum), making it robust and fast for nonlinear least-squares problems.

**Algorithm outline:**

Given an individual with `N` constants `{c₁, c₂, ..., cₙ}` and a dataset of `M` data points, minimize:

```
Objective(c) = Σᵢ₌₁ᴹ (f(xᵢ; c) - yᵢ)²
```

Where `f(x; c)` is the expression evaluated with features `xᵢ` and constants `c`.

**LM iteration:**

1. **Evaluate** the expression on all data points to get the residual vector `r(c)` (predicted - observed).
2. **Compute the Jacobian** `J[i][j] = ∂rᵢ/∂cⱼ` using forward-mode automatic differentiation (Task 5.2).
3. **Compute the approximate Hessian** `H = JᵀJ + λI` (where `λ` is the damping parameter).
4. **Solve** the linear system `H · δ = -Jᵀr` for the update step `δ`.
5. **Update** constants: `c_new = c + δ`.
6. **Evaluate** the objective with `c_new`:
   - If objective decreased: accept the step, decrease `λ` (more Gauss-Newton behavior).
   - If objective increased: reject the step, increase `λ` (more gradient descent behavior).
7. **Repeat** until convergence (|δ| < tolerance) or budget exhausted.

**Budget management:**
- Maximum iterations: configurable (default 10).
- Maximum function evaluations: `max_iters × M` (one Jacobian computation = N forward passes).
- Early termination if relative improvement < 1e-8.

**Heuristic trigger:**
Constant optimization is expensive relative to evaluation. It should only be applied to the **top-k% of individuals** (e.g., top 10% by fitness) to avoid wasting compute on poor structures. The engine (Phase 3) decides *which* individuals to optimize; this task implements *how* they are optimized.

**Numerical safeguards:**
- If `JᵀJ` is singular (degenerate expression or redundant constants), add regularization or bail out.
- If any constant becomes NaN/Inf during optimization, revert to the pre-optimization values.
- If the expression itself produces NaN (due to the new constant values), treat it as an objective increase and revert.

**Why this task matters:**
Constant optimization is the single largest contributor to final solution quality in symbolic regression. Empirical studies show that GP with constant optimization discovers expressions with R² > 0.95 on benchmarks where GP without it achieves R² < 0.7. The LM algorithm is specifically chosen because it converges in few iterations (typically 5-10) and handles the nonlinear, non-convex optimization landscapes that arise in symbolic regression.

**Relies on:** Phase 2 (`Individual.genome` to locate constants via `op == Const`), Phase 4 (evaluator for computing residuals), Task 5.2 (AutoDiff for Jacobian).

---

### Task 5.2 — Implement Forward-Mode Automatic Differentiation on Linearized Trees

> **Assigned to:** `NumOpt-Engineer`

**Description:**
Implement forward-mode automatic differentiation (AD) over the postfix token evaluator. This provides exact gradients of the expression output with respect to each constant — required by the LM optimizer (Task 5.1) for Jacobian computation.

**Concept — Dual Numbers:**
Replace each `double` value in the evaluator's stack with a **dual number** `(value, derivative)`. The derivative tracks `∂output/∂cⱼ` for a specific constant `cⱼ`. Arithmetic on dual numbers follows the standard differentiation rules:

| Operation | Value | Derivative |
|-----------|-------|------------|
| `a + b` | `a.val + b.val` | `a.deriv + b.deriv` |
| `a - b` | `a.val - b.val` | `a.deriv - b.deriv` |
| `a × b` | `a.val × b.val` | `a.val × b.deriv + a.deriv × b.val` |
| `a / b` | `a.val / b.val` | `(a.deriv × b.val - a.val × b.deriv) / (b.val²)` |
| `sin(a)` | `sin(a.val)` | `a.deriv × cos(a.val)` |
| `cos(a)` | `cos(a.val)` | `-a.deriv × sin(a.val)` |
| `exp(a)` | `exp(a.val)` | `a.deriv × exp(a.val)` |
| `log(a)` | `log(a.val)` | `a.deriv / a.val` |

**Seed initialization:**
When evaluating `∂f/∂cⱼ`:
- For `Const` tokens: if the token is the `j-th` constant, push `(value, 1.0)`. Otherwise, push `(value, 0.0)`.
- For `Var` tokens: push `(row[i], 0.0)` (variables are not differentiated with respect to constants).

**Computing the full Jacobian:**
To fill Jacobian column `j`, run the AD evaluator with the `j-th` constant seeded as 1.0. This requires `N` forward passes (one per constant). For expressions with many constants (N > 20), this may become expensive — in such cases, consider bundling multiple derivative seeds into a vector (vectorized forward-mode AD).

**Why this task matters:**
Exact gradients are far superior to finite differences (which are noisy, require 2N evaluations, and suffer from step-size sensitivity). Forward-mode AD produces machine-precision derivatives at the cost of one forward pass per constant — a perfect match for the LM optimizer. Without exact gradients, the optimizer would converge slowly (or diverge) on expressions with tightly coupled constants.

**Relies on:** Task 4.1 (the postfix evaluator — AD mirrors its structure with dual numbers instead of doubles), Phase 2 (`Token` struct, ability to identify which tokens are `Const`).

---

### Task 5.3 — Define the `extern "C"` API (api.h / api.cpp)

> **Assigned to:** `API-Engineer`

**Description:**
Define and implement the C-linkage API that exports the C++ engine's functionality to external consumers. This API must use only C-compatible types (no `std::vector`, `std::string`, classes by reference, or templates) because it will be consumed by C# P/Invoke in Phase 7.

**Exported functions:**

```cpp
extern "C" {
    // Engine lifecycle
    void* SRNet_CreateEngine(const Options* opts);
    void  SRNet_DestroyEngine(void* engine);

    // Data binding
    void SRNet_SetData(void* engine, const double* x_flat,
                       const double* y, int rows, int cols);

    // Evolutionary loop
    void SRNet_Step(void* engine, int generations, RunStats* out);

    // Results retrieval
    void SRNet_GetBestEquation(void* engine, char* buf, int bufLen);
    void SRNet_GetPredictions(void* engine, double* outPreds, int nRows);
    void SRNet_GetHallOfFame(void* engine, RunStats* outModels,
                             int maxModels, int* outCount);
}
```

**Implementation details:**

- **`SRNet_CreateEngine`:** Allocates a new `Engine` object on the heap, initializes it with the given `Options`, and returns an opaque `void*` handle.
- **`SRNet_DestroyEngine`:** Casts the handle back to `Engine*` and `delete`s it.
- **`SRNet_SetData`:** Copies or views the flat row-major `x_flat` array into the engine's columnar `DataView` (converting row-major to column-major for SoA layout). The `y` array is the target variable.
- **`SRNet_Step`:** Runs N generations of the evolutionary loop and fills the `RunStats` struct with current best metrics.
- **`SRNet_GetBestEquation`:** Writes the human-readable infix string of the best expression into the provided char buffer.
- **`SRNet_GetPredictions`:** Evaluates the best expression on the training data and writes predictions to the output array.
- **`SRNet_GetHallOfFame`:** Returns multiple Pareto-optimal models sorted by complexity, each as a `RunStats` entry.

**Error handling:**
Since exceptions cannot cross C-linkage boundaries, use return codes or out-parameters for error reporting. Consider adding:
```cpp
int SRNet_GetLastError(char* buf, int bufLen);
```

**Why this task matters:**
The C-API is the **only entry point** from C# to C++. If a function signature is wrong (wrong parameter order, wrong type, wrong calling convention), P/Invoke will either crash with an access violation or silently produce garbage results. These signatures must match *exactly* between `api.h` (C++) and `NativeMethods.cs` (C#, Phase 7).

**Relies on:** Phase 3 (`Engine` class), Phase 2 (`Options`, `RunStats` types), Task 5.1 (optimizer, which `SRNet_Step` triggers).

---

### Task 5.4 — Create the CUDA Stub

> **Assigned to:** `Perf-Engineer`

**Description:**
Create a CUDA kernel stub that defines the GPU evaluation entry point without implementing the kernel body. This establishes the interface for GPU-accelerated batch expression evaluation and ensures the build system correctly handles `.cu` files.

**Stub file (`cuda_stub.cu`):**

```cuda
#include "tree.h"
#include "types.h"

// GPU kernel: evaluates one expression on one data row per thread.
// blockIdx.x  = individual index in the population
// threadIdx.x = data row index (within the block)
__global__ void EvaluateKernel(
    const Token* genomes,      // Flattened genomes of all individuals
    const int*   genomeLengths,// Length of each genome
    const int*   genomeOffsets,// Offset into the flattened array for each individual
    const double* columns,     // Flattened SoA dataset
    int numRows,
    int numCols,
    double* results            // Output: one value per (individual, row)
) {
    // TODO: Implement stack-based postfix evaluation on GPU
    // Each thread processes one data row for one individual
    // Shared memory for the evaluation stack
}

// Host-side launch wrapper
void LaunchInterpreterKernel(
    const Population& pop,
    const DataView& data,
    double* d_results,
    cudaStream_t stream
) {
    // TODO: Flatten population genomes into contiguous device buffer
    // TODO: Launch kernel with (pop.Size()) blocks × (data.rows) threads
    // TODO: Copy results back to host
}
```

**Thread mapping rationale:**
- **One block per individual:** All threads in a block evaluate the same expression on different data rows. This maximizes instruction coherence (all threads execute the same token sequence — perfect for GPU warp execution).
- **One thread per row (within a block):** Each thread independently evaluates the expression on its row. The stack is per-thread (using local memory or shared memory partitions).
- **Alternative:** For very large datasets, use a grid-stride loop where each thread processes multiple rows.

**Why this task matters:**
GPU evaluation can provide 10-100× speedup over CPU for large populations and datasets. The stub establishes the kernel launch interface so that the engine (Phase 3) can conditionally dispatch to GPU evaluation without architectural changes. The build system integration (CMakeLists.txt must compile `.cu` files when CUDA is available) validates that the full GPU pipeline will work end-to-end.

**Relies on:** Phase 1 (CUDA support in CMakeLists.txt), Phase 2 (`Token`, `DataView`, `Population` types), Task 4.1 (understanding the stack-based evaluation algorithm that the kernel will implement).

---

## Phase Completion Criteria

| Criterion | Verification |
|-----------|-------------|
| LM optimizer reduces MSE on a known `a*x + b` problem | Unit test with known optimal constants |
| AutoDiff produces correct gradients vs. finite differences | Numerical comparison test |
| All C-API functions compile and link | Build test |
| C-API round-trip: create engine → set data → step → get equation → destroy | Integration test |
| CUDA stub compiles (when CUDA toolkit is available) | Conditional build test |
| No memory leaks through C-API lifecycle | Valgrind / ASAN |
| `RunStats` layout matches expected `sizeof()` | Static assert |

---

## Summary

Phase 5 completes the C++ core by adding the three components that interface with the outside world: the constant optimizer that refines GP's raw output into production-quality expressions, the C-API that makes the engine consumable by any foreign language, and the CUDA stub that reserves the path to GPU acceleration. Together with Phases 2-4, this phase completes the entire native computing engine — Phases 6-7 build the C# wrapper around it.
