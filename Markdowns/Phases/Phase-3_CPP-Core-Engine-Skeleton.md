# Phase 3 — C++ Core: Engine Skeleton

> **Phase Goal:** Implement the evolutionary search engine — the main generational loop, genetic operators (crossover and mutation on linearized trees), Age-Fitness Pareto (AFP) selection, and ε-Lexicase parent selection — that drives the symbolic regression discovery process.

---

## Overview

Phase 3 is where SymbolicRegressionNet comes alive. The data structures from Phase 2 defined *what* the system manipulates; this phase defines *how* it manipulates them. The `Engine` class is the heart of the system — an iterative loop that evolves a population of candidate mathematical expressions toward better data fit while managing complexity, diversity, and bloat.

The engine orchestrates several sophisticated evolutionary mechanisms:

1. **Age-Fitness Pareto Optimization (AFP)** — A generational survival strategy that balances fitness with "novelty." Rather than purely replacing the worst individuals, AFP maintains a Pareto front of (age, fitness) pairs, ensuring that both *fit* expressions and *young* expressions survive. This prevents premature convergence by continually injecting fresh genetic material.

2. **ε-Lexicase Selection** — A parent selection method that evaluates individuals on *individual data points* rather than aggregate fitness. It shuffles the training cases, then filters candidates that are within ε of the best on each successive case. This promotes specialists — expressions that are uniquely good on subsets of the data — which dramatically improves diversity and final solution quality.

3. **Genetic Operators on Linearized Trees** — Crossover and mutation must operate on the flat `Token[]` arrays defined in Phase 2, not pointer-based trees. This requires careful bookkeeping of subtree boundaries in postfix notation but enables `memcpy`-based splicing — far faster than recursive tree copying.

---

## Why This Phase Exists

### The Problem It Solves

Without the engine, there is no search. Phases 2, 4, and 5 provide the *substrate* (data structures, evaluation, optimization), but the engine provides the *dynamics*. It decides:

- Which expressions survive and which are discarded (selection pressure).
- How new expressions are created from existing ones (variation operators).
- When to stop searching (termination criteria).
- How to balance quality vs. complexity (multi-objective optimization).

### Its Purpose in the Grand Scheme

The engine is the **orchestrator** of the entire computational pipeline. In a single generation:

1. The engine calls the **evaluator** (Phase 4) to score every individual.
2. It calls the **optimizer** (Phase 5) to fine-tune constants of promising individuals.
3. It applies **selection** to choose survivors and parents.
4. It applies **genetic operators** to produce offspring.
5. It reports **statistics** through the C-API (Phase 5) to the C# SDK (Phase 7).

If the engine's selection or variation operators are implemented incorrectly — even subtly — the search either stagnates (too much selection pressure), produces bloated expressions (insufficient complexity control), or converges prematurely (not enough diversity). This phase must get the balance right.

---

## Dependencies

| Depends On | Description |
|-----------|-------------|
| **Phase 1** | Build system and project structure |
| **Phase 2** | `Token`, `Individual`, `Population`, `Options`, `DataView` types |

### What Depends on Phase 3

| Phase | Dependency Reason |
|-------|-------------------|
| **Phase 4** (Evaluator) | The engine calls `EvaluatePopulation()` each generation — the evaluator needs the population structure Phase 3 defines |
| **Phase 5** (Optimizer & C-API) | The engine calls `OptimizeConstants()` for top individuals; C-API's `SRNet_Step()` wraps the engine's `Step()` method |
| **Phase 8** (Verification) | End-to-end tests verify the engine converges on known benchmarks |

---

## Tasks

### Task 3.1 — Implement the `Engine` Class and Evolutionary Loop Skeleton

> **Assigned to:** `GP-Specialist`

**Description:**
Implement the core `Engine` class that owns the population, dataset reference, and configuration, and drives the generational evolutionary loop.

**Class structure:**

```cpp
class Engine {
public:
    Engine(const Options& opts);

    void SetData(const DataView& data);
    void Initialize();                    // Create random initial population
    void Step(int generations);           // Run N generations
    RunStats GetStats() const;            // Current best metrics
    const Individual& GetBest() const;    // Best individual by fitness

private:
    Population population_;
    DataView data_;
    Options options_;
    std::mt19937_64 rng_;                 // Mersenne Twister PRNG

    void RunOneGeneration();
    void EvaluateAll();                   // Calls the Evaluator (Phase 4)
    void SelectSurvivors();              // AFP selection
    std::pair<const Individual*, const Individual*> SelectParents();  // ε-Lexicase
    Individual CreateOffspring(const Individual& p1, const Individual& p2);
};
```

**The generational loop (`RunOneGeneration()`):**

1. **Evaluate** all individuals that have not been evaluated (new offspring).
2. **Optionally optimize constants** for top-k% individuals (calls Phase 5's optimizer).
3. **Select survivors** using AFP Pareto ranking.
4. **Select parents** using ε-Lexicase.
5. **Apply crossover** with probability `options_.crossover_rate`.
6. **Apply mutation** with probability `options_.mutation_rate`.
7. **Inject fresh random individuals** to replace the oldest/worst (AFP rejuvenation).
8. **Increment age** of all surviving individuals.
9. **Record statistics** (best fitness, Pareto front size, generation count).

**`Initialize()` — Ramped Half-and-Half:**
Generate the initial population using the Ramped Half-and-Half method:
- Divide the population into depth tiers from 2 to `max_tree_depth`.
- For each tier, generate half the individuals using the "grow" method (random depth up to the tier limit) and half using the "full" method (all leaves at the tier depth).
- This ensures diversity in initial expression sizes and structures.

**Why this task matters:**
The engine loop defines the cadence and ordering of all computational work. The sequence (evaluate → optimize → select → breed → inject) is carefully designed: evaluation must precede selection (you can't select without fitness), constant optimization must follow evaluation of raw fitness (to refine promising individuals), and fresh injection must follow selection (to replace discarded individuals). Reordering these steps would break the search dynamics.

**Relies on:** Phase 2 (`Individual`, `Population`, `Options` types must be defined).

---

### Task 3.2 — Implement Genetic Operators on Linearized Trees

> **Assigned to:** `GP-Specialist`

**Description:**
Implement crossover and mutation operators that work directly on the linearized `Token[]` genome representation. This is a non-trivial algorithmic challenge because subtree boundaries in postfix notation are not explicit — they must be inferred by traversing the token array.

**Subtree Crossover:**

Given two parent genomes (flat `Token` vectors), produce one offspring by replacing a subtree in parent 1 with a subtree from parent 2.

Algorithm:
1. **Find subtree boundaries** in both parents. A subtree in postfix notation starts at some index `i` and extends leftward. To find the start of a subtree rooted at index `i`, walk backward, tracking the stack depth: each binary op (`Add`, `Sub`, `Mul`, `Div`) increases the needed operand count by 1, each unary op (`Sin`, `Cos`, `Exp`, `Log`) keeps it the same, and each terminal (`Const`, `Var`) decreases it by 1. The subtree starts where the count reaches zero.
2. **Select random crossover points** in both parents (biased toward operators, not terminals, for meaningful exchange).
3. **Construct the offspring** by concatenating: `parent1[0..start1]` + `parent2[start2..end2]` + `parent1[end1..]`.
4. **Enforce depth limits** by checking the resulting tree depth and rejecting offspring that exceed `max_tree_depth`.

**Mutation operators:**

- **Subtree Mutation:** Replace a random subtree with a newly generated random subtree (using the "grow" method).
- **Point Mutation:** Replace a single token's opcode with a different opcode of the same arity (e.g., `Add` → `Mul`, `Sin` → `Cos`), or perturb a constant value by a small random amount.
- **Hoist Mutation:** Replace the entire tree with one of its subtrees — a powerful bloat reduction operator.
- **Shrink Mutation:** Replace a random subtree with a single terminal (constant or variable) — simplifies expressions.

**Why this task matters:**
Genetic operators are the *variation engine* — the only way the system explores new expressions. Without effective crossover, the search degenerates to random restart. Without mutation, the search has no mechanism to introduce novel sub-expressions. The quality and efficiency of these operators directly determine the convergence speed and final solution quality.

Working on linearized trees is harder to implement correctly than pointer-based trees, but it enables `memcpy`-based splicing (no dynamic allocation during crossover), which Phase 4's performance-sensitive evaluation loop depends on.

**Relies on:** Phase 2 (`Token` struct, `Individual.genome` as a `vector<Token>`), and Task 3.1 (the engine must exist to call these operators).

---

### Task 3.3 — Implement Age-Fitness Pareto (AFP) Selection

> **Assigned to:** `GP-Specialist`

**Description:**
Implement the survivor selection mechanism: Age-Fitness Pareto Optimization. AFP treats the evolutionary search as a bi-objective optimization problem with two objectives:

1. **Fitness** (minimize MSE) — how well the expression fits the data.
2. **Age** (minimize) — how many generations the individual has survived.

**Algorithm:**

1. **Combine** the current population (survivors from the previous generation, with age incremented) and the newly created offspring (with age = 0).
2. **Perform non-dominated sorting** on the (age, fitness) objective space.
   - An individual A **dominates** B if A is at least as good as B on both objectives and strictly better on at least one.
   - **Rank 0** = non-dominated individuals (nothing dominates them).
   - **Rank 1** = dominated only by Rank 0, etc.
3. **Fill the survivor pool** by iterating through ranks: add all of Rank 0, then Rank 1, etc., until the target population size is reached. If a rank partially fits, use **crowding distance** to select the individuals that contribute most to diversity within that rank.
4. **Complexity-based filtering** (optional but recommended): Add a second Pareto filter on (fitness, complexity) to maintain a front of simple-and-accurate expressions (the Hall of Fame).

**Why AFP is chosen over simpler selection:**
Traditional age-independent selection (e.g., tournament selection for survival) causes premature convergence: once a locally optimal expression dominates the population, the search stalls. AFP counteracts this by giving *young* individuals an advantage — even if their fitness is mediocre, their low age makes them non-dominated. This continually injects diversity and prevents the population from collapsing around a single basin in expression space.

**Why this task matters:**
Survivor selection determines the *search trajectory*. Too aggressive, and the population converges prematurely. Too lenient, and the population drifts without progress. AFP's balance of novelty (age) and quality (fitness) is specifically designed for symbolic regression, where the expression landscape is highly multimodal and deceptive.

**Relies on:** Phase 2 (`Individual` with `fitness` and `age` fields), Task 3.1 (the engine calls this during `SelectSurvivors()`).

---

### Task 3.4 — Implement ε-Lexicase Selection for Parent Selection

> **Assigned to:** `GP-Specialist`

**Description:**
Implement ε-Lexicase selection as the parent selection mechanism. Unlike tournament selection (which picks parents based on aggregate fitness), Lexicase selection evaluates candidates on *individual training cases* in a random order, progressively filtering out individuals that are not within ε of the best on each case.

**Algorithm:**

1. **Start** with the full population as the candidate pool.
2. **Shuffle** the training case indices into a random order.
3. **For each case** (in the shuffled order):
   a. Find the best (lowest) error on this case among all candidates.
   b. Set the threshold = best_error + ε (where ε is typically the Median Absolute Deviation of errors on this case across the population).
   c. **Filter** the candidate pool: remove any individual whose error on this case exceeds the threshold.
   d. If only one candidate remains, return it as the selected parent.
   e. If the candidate pool is empty (shouldn't happen with ε), fall back to random selection from the previous non-empty pool.
4. If all cases are exhausted and multiple candidates remain, return one at random.

**Calculating ε (epsilon):**
For each training case `j`, compute the Median Absolute Deviation (MAD) of `error_vector[j]` across the population. Use this as ε_j. This makes ε adaptive: on cases where the population has high variance, ε is large (tolerant); on cases where the population is uniform, ε is small (selective).

**Why ε-Lexicase is chosen:**
Standard Lexicase selection (ε = 0) is extremely selective — it often selects individuals that are *uniquely best* on at least one case, which promotes diversity but can be too aggressive. ε-Lexicase relaxes this by tolerating near-best individuals, which:
- Increases the chance that well-rounded individuals are selected (not just narrow specialists).
- Works better with continuous-valued errors (as in regression) compared to binary pass/fail (as in program synthesis).
- Empirically outperforms tournament selection on symbolic regression benchmarks (SRBench).

**Why this task matters:**
Parent selection determines *which genetic material enters the next generation*. If parents are selected poorly (too uniform, too random), genetic operators in Task 3.2 cannot produce meaningful improvements. ε-Lexicase is specifically designed for symbolic regression and is considered state-of-the-art for maintaining population diversity while selecting high-quality parents.

**Relies on:** Phase 2 (`Individual.error_vector` must be populated by the evaluator in Phase 4), Task 3.1 (the engine calls this during parent selection).

> **Note:** The `error_vector` will be filled by Phase 4's evaluator. During initial development of Phase 3, the engine can use a placeholder evaluator that assigns random errors. Full integration happens when Phase 4 is complete.

---

## Phase Completion Criteria

| Criterion | Verification |
|-----------|-------------|
| `Engine` class compiles and the generational loop runs | Compilation + simple test |
| Crossover produces valid postfix trees | Unit test: evaluate parent and offspring |
| Mutation produces valid postfix trees | Unit test: all mutation types |
| AFP selection maintains population size | Assert population size after selection |
| ε-Lexicase returns a valid individual | Unit test with known error vectors |
| No memory leaks in the crossover/mutation cycle | Valgrind or ASAN |
| Ramped half-and-half produces diverse initial population | Distribution check |

---

## Summary

Phase 3 transforms a static collection of data structures into a living evolutionary system. The engine loop, genetic operators, AFP survival selection, and ε-Lexicase parent selection together form the algorithmic core of SymbolicRegressionNet. This phase depends on Phase 2's types and provides the driver that Phase 4 (evaluation) and Phase 5 (optimization) plug into. Without this engine, the system has data structures but no search.
