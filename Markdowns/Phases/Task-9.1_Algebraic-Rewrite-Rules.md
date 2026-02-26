# Task 9.1: Algebraic Rewrite Rules & Domain Bounds

> **Author:** `@[Markdowns/Members/NumOpt-Engineer.md]`
> **Related Phase:** Phase 9 (Algebraic Simplification Pass)

This document categorizes the algebraic simplification rules into **Constant Folding** and **Identity Reduction**, explicitly defining the numerical domain bounds and floating-point tolerances required to maintain equivalence and stability within the Symbolic Regression engine.

---

## 1. Floating-Point Tolerances & Safety

In machine-learning-driven symbolic regression, exactly hitting `0.0` or `1.0` is rare due to floating point drift. Identity matching must utilize a tolerance `ϵ`.

### Constants
- **Zero Tolerance (`ϵ_zero`):** `1e-9`. Constants $c$ where $|c| < 10^{-9}$ should be treated as `0.0` for Identity mappings (e.g., $x * c \rightarrow 0$).
- **One Tolerance (`ϵ_one`):** `1e-9`. Constants $c$ where $|c - 1.0| < 10^{-9}$ should be treated as `1.0`.

### NaN & Inf Handling
- If any subtree evaluates to `NaN` or `±Infinity` during constant folding, the simplification pass should **abort folding for that branch**, leaving it as-is or folding the entire branch to a fallback large penalty constant (e.g., `1e100`), ensuring the evaluator correctly fails the genotype.

---

## 2. Constant Folding Rules

Recursive evaluation of operators where all children are `Const` tokens.

| Operation | Match | Folded Result | Domain Guard / Failure Condition |
| :--- | :--- | :--- | :--- |
| **Add** | `Const(a) + Const(b)` | `Const(a + b)` | None |
| **Sub** | `Const(a) - Const(b)` | `Const(a - b)` | None |
| **Mul** | `Const(a) * Const(b)` | `Const(a * b)` | None |
| **Div** | `Const(a) / Const(b)` | `Const(a / b)` | If $|b| < \epsilon_{zero}$, map to `PenaltyConst` (e.g., `1e100`) to avoid `Inf`/`NaN` injection. |
| **Sin** | `sin(Const(a))` | `Const(sin(a))` | None |
| **Cos** | `cos(Const(a))` | `Const(cos(a))` | None |
| **Exp** | `exp(Const(a))` | `Const(exp(a))` | If $a > 709.0$, folds to `PenaltyConst` (avoids `Inf`). |
| **Log** | `log(Const(a))` | `Const(log(|a|))` | *(Assuming protected log).* If $a == 0$, folds to `PenaltyConst`. |

---

## 3. Algebraic Identity Reductions

Matching subtrees to eliminate zero-effect operations. Let `X` denote any valid subtree (could be a variable, constant, or complex algebraic tree). 

*Note: For performance, structural equality of string representations or deep subtree token matching is required to assert `X == X`.*

### A. Additive Identities
| Rule | Pattern | Replacement | Safeties |
| :--- | :--- | :--- | :--- |
| **Zero Add** | `X + 0` <br> `0 + X` | `X` | Tolerance: constant matched as 0 if $|c| < \epsilon_{zero}$. |
| **Zero Sub** | `X - 0` | `X` | Same as above. |
| **Negation** | `0 - X` | `-1.0 * X` | Converts subtraction from zero to unified scalar multiplication (helps constant optimizer). |
| **Self Sub** | `X - X` | `0.0` | **Warning:** If `X` evaluates to `Inf`, `Inf - Inf = NaN`. Since SR punishes `Inf` anyway, collapsing to `0.0` is generally safe and prevents bloat. |
| **Self Add** | `X + X` | `2.0 * X` | Folds redundant variables into scalar multiplications. |

### B. Multiplicative Identities
| Rule | Pattern | Replacement | Safeties |
| :--- | :--- | :--- | :--- |
| **Identity** | `X * 1` <br> `1 * X` | `X` | Tolerance: $|c - 1.0| < \epsilon_{one}$. |
| **Annihilation**| `X * 0` <br> `0 * X` | `0.0` | **Warning:** If `X` is `NaN`, `NaN * 0 = NaN`. In our engine, collapsing an infected `NaN` tree to `0.0` might bypass a penalty, giving a bad equation a good MSE. **Rule:** Only apply if `X` contains no unsafe domain operations (like division by zero). Alternatively, just accept the slight risk for the massive simplification benefits. |
| **Div Identity**| `X / 1` | `X` | Same as Identity Mul. |
| **Self Div** | `X / X` | `1.0` | **Warning:** If `X` evaluates to $0$, it yields `NaN`. If protected division is active, $0/0$ is forced to $1.0$ anyway. Can be safely simplified if using protected division. |
| **Zero Div** | `0 / X` | `0.0` | Same `NaN` masking warning as Annihilation. |

### C. Transcendental Inverses (Domain-Restricted)
These are notoriously tricky in floating point.

| Rule | Pattern | Replacement | Domain Guard |
| :--- | :--- | :--- | :--- |
| **Exp-Log** | `exp(log(X))` | `X` | Or `|X|` if using protected logarithm over absolute values. |
| **Log-Exp** | `log(exp(X))` | `X` | Mathematically sound for all real $X$. Can safely reduce. |
| **Double Exp**| `exp(exp(X))` | *None* | Often causes immediate overflow `Inf`. Let the evaluator hit the ceiling and penalty. |
