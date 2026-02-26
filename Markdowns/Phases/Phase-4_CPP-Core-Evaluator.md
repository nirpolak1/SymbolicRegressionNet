# Phase 4 — C++ Core: Evaluator

> **Phase Goal:** Implement the performance-critical expression evaluation pipeline — a threaded postfix interpreter that evaluates every individual's linearized expression tree across the entire dataset, computes per-point errors for ε-Lexicase, calculates aggregate fitness metrics, and provides SIMD-ready stubs for future vectorized acceleration.

---

## Overview

Phase 4 builds the single most performance-sensitive component of SymbolicRegressionNet. Evaluation is the **dominant cost** of evolutionary symbolic regression: every generation, every individual in the population must be evaluated on every row in the training dataset. With a population of 1,000 individuals and a dataset of 10,000 rows, that's **10 million expression evaluations per generation**. Over 100 generations, that's 1 billion evaluations.

The evaluator transforms the linearized `Token[]` expression (defined in Phase 2) into a numerical result by executing a **stack-based postfix interpreter**. It reads tokens left-to-right, pushing operands and popping operators, producing a scalar output for each data row. This design was specifically chosen because:

1. **Postfix evaluation requires no explicit tree traversal** — no recursion, no stack frames from the call stack, just a tight loop with a data stack.
2. **The memory access pattern is sequential** — the evaluator reads tokens in order from a contiguous array, maximizing L1/L2 cache utilization.
3. **The algorithm maps naturally to SIMD** — instead of evaluating one row at a time, the evaluator can process 4 rows (AVX2) or 8 rows (AVX-512) simultaneously, performing the same operation on packed `double` vectors.

---

## Why This Phase Exists

### The Problem It Solves

Without evaluation, individuals have no fitness — and without fitness, selection cannot operate. The evaluator is the bridge between *syntactic* candidates (token arrays) and *semantic* quality (how well the expression fits the data).

But evaluation isn't just about correctness — it's about **speed**. If the evaluator is slow, the entire system bottlenecks:

- Each generation takes longer, reducing the total number of generations within a time budget.
- Fewer generations means less evolutionary search, which means worse final expressions.
- Users of the C# SDK (Phase 7) experience unacceptable wait times.

### Its Purpose in the Grand Scheme

The evaluator serves three roles in the system:

1. **Fitness computation** for the engine's selection (Phase 3): The scalar fitness (MSE, R²) determines which individuals survive.
2. **Per-case error vectors** for ε-Lexicase selection (Task 3.4): The error on each data point enables fine-grained, diversity-preserving parent selection.
3. **Gradient foundation** for constant optimization (Phase 5): The evaluator's stack-based execution can be augmented with forward-mode automatic differentiation (dual numbers) to produce exact gradients of the expression with respect to its constants — enabling gradient-based optimization without finite differences.

---

## Dependencies

| Depends On | Description |
|-----------|-------------|
| **Phase 1** | Build system with SIMD compile flags (AVX2/AVX-512) |
| **Phase 2** | `Token` struct (what to evaluate), `Individual` struct (where to store results), `DataView` struct (dataset to evaluate against) |
| **Phase 3** | The engine's evaluation step calls into this evaluator |

### What Depends on Phase 4

| Phase | Dependency Reason |
|-------|-------------------|
| **Phase 3** (Engine) | Bidirectional: the engine calls `EvaluatePopulation()`. Phase 3 can be developed with a stub evaluator, but full integration requires Phase 4. |
| **Phase 5** (Optimizer) | The Levenberg-Marquardt optimizer uses the evaluator + AutoDiff to compute Jacobians for constant optimization |
| **Phase 8** (Verification) | Performance benchmarks measure evaluation throughput |

---

## Tasks

### Task 4.1 — Implement the Threaded Postfix Interpreter

> **Assigned to:** `Perf-Engineer`

**Description:**
Implement the core expression evaluator: a stack-based postfix interpreter that evaluates a linearized `Token[]` expression against a single data row, and its batch wrapper that parallelizes evaluation across the population and dataset.

**Single-row evaluation function:**

```cpp
double EvaluateExpression(const Token* genome, int length, const double* row, int numVars);
```

Algorithm:
1. Maintain a small, fixed-size stack of `double` values (e.g., 64 entries — sufficient for trees of depth 64).
2. Iterate through the `Token` array left-to-right:
   - `Var(i)` → push `row[i]`
   - `Const(c)` → push `c`
   - `Add` → pop two values, push their sum
   - `Sub` → pop two values, push their difference
   - `Mul` → pop two values, push their product
   - `Div` → pop two values; if `|denominator| < 1e-10`, push `1.0` (protected division); else push `a/b`
   - `Sin` → pop one value, push `sin(v)`
   - `Cos` → pop one value, push `cos(v)`
   - `Exp` → pop one value; if `v > 709`, push `1e308` (overflow guard); else push `exp(v)`
   - `Log` → pop one value; if `v ≤ 0`, push `0.0` (protected log); else push `log(v)`
3. After all tokens, the top of the stack is the expression's output.

**Numerical guards (critical):**
The evaluator is the first line of defense against numerical instability. Expressions evolved by GP frequently produce:
- Division by near-zero (e.g., `x / (x - x)`).
- Exponential overflow (e.g., `exp(exp(x))`).
- Logarithm of non-positive numbers.
- Deep nesting producing NaN through accumulated floating-point errors.

Every operation must be protected. A NaN or Inf that leaks through the evaluator will corrupt the individual's fitness, poison selection, and silently degrade the entire search. The guards above (protected division, clamped exp, protected log) are standard in GP literature and must be implemented rigorously.

**Batch evaluation wrappers:**

```cpp
void EvaluateIndividual(Individual& ind, const DataView& data);
void EvaluatePopulation(Population& pop, const DataView& data);
```

- `EvaluateIndividual`: Loops over all rows in `data`, calls `EvaluateExpression` for each, stores per-row errors in `ind.error_vector`, and computes scalar `ind.fitness` (MSE).
- `EvaluatePopulation`: Loops over all individuals in the population, calls `EvaluateIndividual` for each, using OpenMP or `std::thread` for parallelism across individuals.

**Why this task matters:**
This is the innermost loop of the entire system. Evaluation is called `population_size × rows × generations` times. A 10% inefficiency here translates to hours of wasted compute over a full run. The fixed-size stack, sequential token access, and absence of recursion are all performance-critical design decisions.

**Relies on:** Phase 2 (`Token`, `Individual`, `DataView`), Phase 1 (compiler flags for optimization).

---

### Task 4.2 — Add SIMD Stubs / AVX2 Evaluation Skeleton

> **Assigned to:** `Perf-Engineer`

**Description:**
Create the SIMD-vectorized evaluation skeleton that processes **multiple data rows simultaneously** using AVX2 intrinsics. In this phase, the implementation consists of function signatures, data layout comments, and `TODO` markers — the full AVX2 intrinsic implementation is a future optimization pass.

**Concept:**
Instead of evaluating one row at a time (producing one `double`), the SIMD evaluator processes 4 rows at a time (producing a `__m256d` vector of 4 doubles). The evaluation stack becomes a stack of `__m256d` vectors.

**Skeleton:**

```cpp
// Evaluates the expression on 4 rows simultaneously using AVX2.
// row_base: pointer to the first of 4 consecutive rows in SoA layout.
// Returns: 4 output values packed in a __m256d.
__m256d EvaluateExpression_AVX2(
    const Token* genome, int length,
    const double* const* columns, int startRow
);
// TODO: AVX2 intrinsics — _mm256_add_pd, _mm256_mul_pd, etc.
// TODO: Protected division using _mm256_cmp_pd and _mm256_blendv_pd.
// TODO: Transcendentals (sin, cos, exp, log) via Intel SVML or polynomial approximation.
```

**AVX-512 variant (optional):**

```cpp
// Evaluates on 8 rows simultaneously (requires AVX-512).
__m512d EvaluateExpression_AVX512(
    const Token* genome, int length,
    const double* const* columns, int startRow
);
```

**Why SIMD stubs now, not full implementation?**
The SIMD skeleton establishes the function signatures and data flow contracts that the rest of the system (batch evaluator, engine) can code against. Full AVX2 implementation requires careful handling of transcendental functions (no native AVX2 `sin`/`cos` — must use SVML or polynomial approximation) and masked operations for remainder rows (when dataset size isn't a multiple of 4). These are engineering-heavy tasks that can be deferred without blocking other phases.

**Why this task matters:**
The SoA `DataView` layout from Phase 2 was **specifically designed** for this SIMD evaluator. Column-major storage means that `columns[i][row]` to `columns[i][row+3]` are contiguous in memory — a perfect fit for `_mm256_loadu_pd`. If the SIMD function signatures are not defined correctly now, refactoring the batch evaluation loop later will be disruptive.

**Relies on:** Task 4.1 (the scalar evaluator provides the reference implementation), Phase 2 (`DataView` SoA layout), Phase 1 (AVX2 compile flags in CMakeLists.txt).

---

### Task 4.3 — Implement Metric Computation (MSE, R², Complexity)

> **Assigned to:** `NumOpt-Engineer`

**Description:**
Implement the fitness metrics that quantify how well an expression fits the data and how complex it is. These metrics drive selection decisions in the engine (Phase 3) and are reported through the C-API (Phase 5) to the user.

**Metrics:**

**1. Mean Squared Error (MSE):**
```
MSE = (1/N) × Σ (predicted_i - observed_i)²
```
The primary fitness metric. Lower is better. Used as the fitness objective in AFP selection.

**2. Coefficient of Determination (R²):**
```
R² = 1 - (SS_res / SS_tot)
```
Where `SS_res = Σ(predicted_i - observed_i)²` and `SS_tot = Σ(observed_i - mean(observed))²`. R² of 1.0 indicates perfect fit; 0.0 indicates the model is no better than predicting the mean.

**Numerical guard for R²:** If `SS_tot < 1e-15` (constant target variable), return R² = 0.0 to avoid division by zero.

**3. Complexity Score:**
```
complexity = number of tokens in the genome
```
A simple count of nodes in the expression. Used as the second objective in (fitness, complexity) Pareto ranking to maintain a Hall of Fame of simple-yet-accurate expressions.

Future extensions may use weighted complexity (e.g., transcendentals cost 3, arithmetic costs 1), but the initial implementation uses a flat count.

**4. Per-point Error Vector:**
```
error_vector[i] = |predicted_i - observed_i|
```
Absolute error on each data point, stored on the `Individual`. Used directly by ε-Lexicase selection (Task 3.4).

**Implementation notes:**
- MSE and R² should be computed in a **numerically stable** manner. For MSE, use compensated summation (Kahan or pairwise) to avoid catastrophic cancellation when summing many small squared differences.
- R² should handle edge cases: constant predictions (SS_res = SS_tot → R² = 0), perfect fit (SS_res = 0 → R² = 1), and degenerate cases where predictions are all NaN (return R² = -∞ or a penalty value).
- Complexity scoring should be computed **once** after crossover/mutation (not recomputed every evaluation), since it depends only on the genome structure, not the data.

**Why this task matters:**
Metrics are the **feedback signal** of the evolutionary search. If MSE is computed incorrectly (e.g., with catastrophic cancellation on large datasets), the engine will select the wrong individuals, and the search will fail silently. If the complexity score is wrong, the Pareto front will include bloated expressions that should have been pruned. These metrics must be exact and robust.

**Relies on:** Phase 2 (`Individual.error_vector`, `Individual.fitness`, `Individual.complexity`), Task 4.1 (the evaluator produces predicted values).

---

## Phase Completion Criteria

| Criterion | Verification |
|-----------|-------------|
| Scalar evaluator produces correct results on simple expressions | Unit tests: `x + 1`, `x * x`, `sin(x)` |
| Protected operations prevent NaN/Inf leakage | Test with div-by-zero, exp(1000), log(-1) |
| Batch evaluator scores match individual evaluator results | Cross-check test |
| MSE and R² are numerically stable on large datasets | Test with 100K+ rows |
| Per-point error vector is populated correctly | Verify `error_vector.size() == rows` |
| SIMD function signatures compile with AVX2 flags | Compilation test |
| Parallel population evaluation produces deterministic results | Fixed seed + thread count test |

---

## Summary

Phase 4 implements the computational workhorse of SymbolicRegressionNet: the expression evaluator. The threaded postfix interpreter, SIMD stubs, and metric computation together form the pipeline that converts syntactic expressions (token arrays) into semantic quality scores (MSE, R², error vectors). This phase is uniquely performance-sensitive — it sits in the innermost loop of the evolutionary search — and its numerical correctness directly determines the quality of the discovered expressions. The SoA data layout from Phase 2 and the SIMD compile flags from Phase 1 were designed specifically to enable this phase's optimizations.
