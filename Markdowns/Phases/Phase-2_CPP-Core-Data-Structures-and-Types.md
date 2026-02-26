# Phase 2 — C++ Core: Data Structures & Types

> **Phase Goal:** Define the foundational C++ data structures — the linearized expression tree, individual and population representations, fitness data layouts, and configuration types — that form the memory-level contract for the entire compute engine.

---

## Overview

Phase 2 transitions from scaffolding to substance. While Phase 1 created empty folders and build files, Phase 2 populates the C++ core with the **data type definitions** that every algorithmic component will operate on. These are not algorithms themselves — they are the *language* in which algorithms will speak.

The central design decision in SymbolicRegressionNet is the use of **linearized (flat) expression trees**. Rather than the traditional pointer-based tree where each node has `left` and `right` children, expressions are encoded as a flat array of `Token` structs in **reverse-Polish (postfix) order**. This representation is critical for performance: it enables cache-friendly sequential evaluation, trivial SIMD vectorization, efficient GPU kernel dispatch, and `memcpy`-based genetic operators (crossover becomes a splice of contiguous memory).

Every structure defined in this phase must satisfy two audiences simultaneously:

1. **The algorithm implementers** (GP-Specialist, NumOpt-Engineer) who need semantically rich types with clear invariants.
2. **The performance engineers** (Perf-Engineer) who need memory layouts that map efficiently to hardware — struct-of-arrays for SIMD, contiguous allocations for cache locality, and alignment for GPU transfers.

---

## Why This Phase Exists

### The Problem It Solves

Without agreed-upon data structures:

- The GP-Specialist cannot implement crossover (what is being spliced?).
- The NumOpt-Engineer cannot implement constant optimization (where are the constants in the tree?).
- The Perf-Engineer cannot design GPU kernels (what is the memory layout they'll operate on?).
- The API-Engineer cannot design marshaling (what structs cross the C++/C# boundary?).

### Its Purpose in the Grand Scheme

These types are the **shared vocabulary** of the project. The `Token` struct appears in every phase from 2 through 7. The `Individual` struct flows through the engine (Phase 3), the evaluator (Phase 4), the optimizer (Phase 5), and across the C-API boundary into C# (Phases 6–7). Getting these types right — their size, alignment, field order, and semantics — is a prerequisite for all downstream work.

---

## Dependencies

| Depends On | Description |
|-----------|-------------|
| **Phase 1** | The C++ project (CMakeLists.txt, include/ and src/ directories) must exist to house these header files |

### What Depends on Phase 2

| Phase | Dependency Reason |
|-------|-------------------|
| **Phase 3** (Engine) | Uses `Individual`, `Population`, `Options` to drive the evolutionary loop |
| **Phase 4** (Evaluator) | Uses `Token` (linearized tree) and `DataView` (SoA data) for batch evaluation |
| **Phase 5** (Optimizer) | Uses `Individual` and `Token` to locate and tune constants; `Options`/`RunStats` for C-API |
| **Phase 7** (API & Interop) | Uses `Options` and `RunStats` as marshaled structs across the P/Invoke boundary |

---

## Tasks

### Task 2.1 — Define the `Token` Struct and `OpCode` Enum

> **Assigned to:** `GP-Specialist`

**Description:**
Define the fundamental building block of every expression in the system: the `Token` struct and its associated `OpCode` enumeration. Together, they encode a single node of an expression tree in a linearized (postfix) array.

**`OpCode` enumeration:**

```cpp
enum class OpCode : uint8_t {
    Add, Sub, Mul, Div,       // Binary arithmetic
    Sin, Cos, Exp, Log,       // Unary transcendentals
    Const,                    // Numeric constant
    Var                       // Input variable reference
};
```

**`Token` struct:**

```cpp
struct Token {
    OpCode op;                // Which operation this node represents
    uint16_t var_index;       // Index into the feature columns (used when op == Var)
    double constant;          // Numeric value (used when op == Const)
};
```

**Design rationale:**

- **Why `uint8_t` for OpCode?** Minimizes struct size. With fewer than 256 opcodes, a single byte suffices, and it packs efficiently alongside other fields.
- **Why `uint16_t` for var_index?** Supports up to 65,535 input variables — far beyond any realistic symbolic regression problem — while keeping the struct compact.
- **Why `double` for constant?** Full 64-bit precision is necessary for numerical optimization (Phase 5). Using `float` would limit the constant optimizer's convergence.
- **Why a single struct for all node types?** A discriminated union (all fields in one struct, semantics determined by `op`) avoids virtual dispatch, pointer indirection, and dynamic allocation. The evaluator (Phase 4) can process tokens sequentially without branching on type hierarchies.

**Linearization convention:** An expression like `(x₀ + 3.14) * sin(x₁)` is stored as:

```
[Var(0), Const(3.14), Add, Var(1), Sin, Mul]
```

This postfix ordering allows stack-based evaluation in a single left-to-right pass — a property that Phase 4's evaluator exploits heavily.

**Why this task matters:**
The `Token` struct is the **atomic unit of every expression** in the system. Its layout determines evaluation speed (cache line utilization), GPU kernel design (thread block mapping), and genetic operator efficiency (crossover = contiguous memory splice). A suboptimal token layout would cascade performance penalties across Phases 3, 4, and 5.

**Relies on:** Phase 1 (C++ project structure must exist).

---

### Task 2.2 — Define the `Individual` and `Population` Classes

> **Assigned to:** `GP-Specialist`

**Description:**
Define the types that represent a single candidate solution (`Individual`) and a collection of candidates (`Population`). These types carry the evolutionary state through every generation of the search.

**`Individual` struct:**

```cpp
struct Individual {
    std::vector<Token> genome;                // The linearized expression tree
    double fitness;                           // Scalar fitness (e.g., MSE)
    int age;                                  // Number of generations survived (for AFP)
    int complexity;                           // Expression complexity score (node count)
    std::vector<double> error_vector;         // Per-data-point errors (for ε-Lexicase)
};
```

**`Population` class:**

```cpp
class Population {
public:
    std::vector<Individual> individuals;      // The population members
    int generation;                           // Current generation counter

    void Initialize(const Options& opts);     // Random population generation
    void SortByFitness();                     // For ranking-based selection
    Individual& operator[](int index);        // Random access
    int Size() const;                         // Population size
};
```

**Design rationale:**

- **`error_vector` on each Individual:** The ε-Lexicase selection algorithm (Phase 3) requires per-case error information, not just a scalar fitness. Storing this on the individual avoids recomputation during selection.
- **`age` field:** Age-Fitness Pareto Optimization (AFP) uses age as a second objective. Newly created individuals start at age 0; survivors have their age incremented each generation.
- **`complexity` score:** Multi-objective Pareto ranking uses fitness vs. complexity to maintain a trade-off front of simple-and-good expressions.
- **Flat `std::vector` storage in `Population`:** Enables `Population` to be iterated cache-efficiently and parallelized trivially (Phase 4's batch evaluation, Phase 5's parallel constant optimization).

**Why this task matters:**
The `Individual` and `Population` types are the primary data flowing through the evolutionary loop (Phase 3), the evaluator (Phase 4), the optimizer (Phase 5), and the C-API (Phase 5). If the `Individual` is missing a field (e.g., `error_vector`), selection algorithms in Phase 3 cannot function. If `Population` doesn't support efficient random access and iteration, Phase 4's parallel evaluation is hobbled.

**Relies on:** Task 2.1 (the `Token` type must be defined first, since `Individual.genome` is a vector of tokens).

---

### Task 2.3 — Define SoA Data-View Structures

> **Assigned to:** `Perf-Engineer`

**Description:**
Define the Struct-of-Arrays (SoA) data layout that the evaluator will use for batch expression evaluation across the dataset. The dataset is the *other* input to evaluation (alongside the expression) — it provides the feature values and target variable.

**`DataView` struct:**

```cpp
struct DataView {
    double** columns;     // Array of pointers, one per feature column
    double* target;       // Target variable values
    int rows;             // Number of data points (samples)
    int cols;             // Number of feature columns (variables)
};
```

**Design rationale:**

- **Why SoA instead of AoS (Array-of-Structures)?** When the evaluator encounters a `Var(i)` token, it needs the *i-th column* across all rows. In SoA layout, this is a single contiguous `double*` array — perfect for sequential reads, SIMD vectorization (AVX2 loads 4 doubles), and GPU coalesced memory access. An AoS layout (rows of structs with all features) would cause strided access patterns that thrash caches.
- **Why raw pointers?** The `DataView` is a *view* — it does not own the data. The C# side (Phase 6) allocates and pins the managed arrays; the C++ side receives raw pointers. This avoids data copying across the interop boundary.
- **Why `double**` (pointer-to-pointer)?** Each column may be independently allocated (or a slice of a larger buffer). The indirection allows the C# `PinnedBuffer<T>` (Phase 6) to pin individual column arrays and pass their addresses.

**Why this task matters:**
The `DataView` is the performance-critical data structure in the evaluation hot path. Every expression evaluation (which happens `population_size × dataset_rows` times per generation) reads from this structure. The SoA layout is specifically chosen to enable the SIMD and GPU optimizations in Phase 4. Changing this layout later would require rewriting the evaluator, the CUDA kernel (Phase 5), and the C# pinning logic (Phase 6).

**Relies on:** Phase 1 (C++ project structure).

---

### Task 2.4 — Define `Options` and `RunStats` Structs for the C-API

> **Assigned to:** `Architect`

**Description:**
Define the configuration and reporting structures that will cross the C++/C# interop boundary. These are the types that the C# SDK will marshal via P/Invoke, so they must have **fixed memory layouts**, no C++ standard library types (no `std::vector`, `std::string`), and explicit field ordering.

**`Options` struct:**

```cpp
struct Options {
    int population_size;       // Number of individuals per generation
    int max_generations;       // Maximum evolutionary generations
    int max_tree_depth;        // Maximum expression tree depth
    int tournament_size;       // Tournament selection pressure
    double crossover_rate;     // Probability of crossover vs. mutation
    double mutation_rate;      // Probability of mutation per individual
    uint32_t functions_mask;   // Bitmask of enabled OpCodes
    uint64_t random_seed;      // Seed for reproducibility
};
```

**`RunStats` struct:**

```cpp
struct RunStats {
    int generation;            // Current generation number
    double best_mse;           // Best MSE in the population
    double best_r2;            // Best R² in the population
    char best_equation[512];   // Human-readable best expression (null-terminated)
    int pareto_front_size;     // Number of Pareto-optimal models
};
```

**Design rationale:**

- **Fixed-size char array for `best_equation`:** C# P/Invoke cannot marshal `std::string`. A fixed-size buffer with a known bound allows `[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 512)]` on the managed side.
- **`functions_mask` as a bitmask:** Each bit enables/disables one `OpCode` in the function set. This is compact, fast to check during tree generation, and trivially marshaled as a `uint32_t`.
- **No pointers or references:** These structs are **value-copied** across the interop boundary. They must be self-contained — no heap allocations, no pointers to external memory.
- **`SequentialLayout`-compatible:** The fields are ordered to minimize padding. The Architect coordinates with the API-Engineer (Phase 7) to ensure the C# `[StructLayout(LayoutKind.Sequential)]` attribute produces identical memory layout.

**Why this task matters:**
These structs are the **contract between C++ and C#**. If the field order, types, or sizes disagree between the two languages, P/Invoke calls will silently read garbage data — one of the hardest bugs to diagnose. Getting the layout right in Phase 2 prevents crashes and data corruption in Phases 5, 6, and 7.

**Relies on:** Phase 1 (C++ project structure), and conceptual coordination with Phase 7 (C# P/Invoke marshaling — the Architect must ensure both sides agree on the struct layout).

---

## Phase Completion Criteria

| Criterion | Verification |
|-----------|-------------|
| `tree.h` defines `OpCode` enum and `Token` struct | Code review |
| `tree.h` defines `Individual` struct and `Population` class | Code review |
| `types.h` defines `DataView` struct | Code review |
| `types.h` defines `Options` and `RunStats` structs | Code review |
| All headers compile cleanly with C++20 | `cmake --build` succeeds |
| `sizeof()` assertions match expected sizes | Static asserts in source |
| Struct layouts are documented for C# interop consistency | Comments or ADR |

---

## Summary

Phase 2 defines the data-level vocabulary of SymbolicRegressionNet. The linearized `Token` representation, the `Individual`/`Population` evolutionary containers, the SoA `DataView` for evaluation, and the interop-safe `Options`/`RunStats` types together form the foundation that Phases 3–7 build upon. Every algorithmic decision downstream — how crossover splices genomes, how the evaluator walks tokens, how the GPU kernel maps threads, how C# marshals results — depends on the types defined here.
