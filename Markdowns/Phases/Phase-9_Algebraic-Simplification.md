# Phase 9: Algebraic Simplification Pass

> **Phase Owner:** `@[Markdowns/Members/NumOpt-Engineer.md]`
> **Contributors:** `@[Markdowns/Members/GP-Specialist.md]`, `@[Markdowns/Members/Perf-Engineer.md]`

## Objective

To clean and reduce messy, unreduced expression trees discovered by the Symbolic Regression engine. Without this pass, trees bloat with mathematically redundant operations (e.g., `x - x`, `log(exp(x))`, or un-collapsed nested constants). This pass will produce compact, mathematically equivalent, and highly readable expressions without altering their inherent fitness, while optionally reducing the evaluation cost during the genetic loop by eliminating dead code.

---

## Technical Strategy

The simplification pass operates over the expression tree, applying bottom-up structural rewrites based on known algebraic identities. 

### 1. Constant Folding
Recursively evaluate subtrees where all inputs are constants.
- **Rules:** `c1 <op> c2 -> c3`, `Func(c1) -> c2`.
- **NumOpt-Engineer Requirements:** Ensure that floating-point arithmetic during folding matches the precision of the evaluator exactly. We must trap invalid continuous operations (e.g. `log(-1)`) and collapse them safely to a penalty/NaN constant rather than allowing exceptions to bleed into the engine.

### 2. Algebraic Identity Reduction
Recognize and eliminate zero-effect operations.
- **Rules:**
  - *Add/Subtract:* `x + 0 -> x`, `x - 0 -> x`, `x - x -> 0`, `0 - x -> -x`
  - *Multiply/Divide:* `x * 1 -> x`, `x * 0 -> 0`, `x / 1 -> x`, `x / x -> 1` (with numeric domain safety notes)
  - *Transcendental Inverses:* `exp(log(x)) -> x`, `log(exp(x)) -> x`

---

## Tasks & Responsibilities

### Task 9.1: Define Rewrite Rules and Domain Bounds
- **Owner:** `@[Markdowns/Members/NumOpt-Engineer.md]` (Me)
- **Description:** Specify the exact mapping of algebraic transformations we want to support safely. Define numeric tolerances for checking operations like `x / x == 1` around zero. Determine the bounds required to ensure `exp(log(x))` doesn't introduce a wider domain of valid floating point values than the original tree possessed. 

### Task 9.2: Implement Tree Pruning and Traversal Logic (C++)
- **Owner:** `@[Markdowns/Members/GP-Specialist.md]`
- **Description:** Implement `SimplifySubtree(std::vector<Token>& genome, int subtree_root)` within `genetic_ops.cpp`. Since the trees are stored as flat postfix arrays, the implementation will need to perform an efficient pattern-matching backward walk to identify reducible subtrees and safely splice tokens out of the vector without corrupting the postfix integrity.

### Task 9.3: Performance & Memory Allocation Profiling
- **Owner:** `@[Markdowns/Members/Perf-Engineer.md]`
- **Description:** Depending on the execution strategy, the simplification logic may run on every offspring generated, placing it on the hot-path. Ensure the C++ `SimplifySubtree` method works in-place to avoid heap allocations, or pre-sizes memory efficiently. Profile the pass to guarantee it remains $O(N)$ with respect to tree depth/size.

### Task 9.4: Integrate Simplification into the Evolutionary Loop (engine.cpp)
- **Owner:** `@[Markdowns/Members/GP-Specialist.md]`
- **Description:** Add the simplification call to the step sequence in `engine.cpp`.
  - *Option A (Structural):* Run periodically after mutation/crossover to reduce evaluation bloat, balancing bloat control with genetic diversity.
  - *Option B (Cosmetic):* Run only on the Pareto Front before returning the Hall of Fame strings to C#.
  - We will make this configurable using a bitmask/enum in the `Options` structure.
