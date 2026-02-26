# SymbolicRegressionNet

SymbolicRegressionNet is a high-performance hybrid C++/C# library for **Symbolic Regression**.

Unlike traditional regression that fits coefficients to a pre-defined model structure (like a line or a polynomial) or neural networks that behave as "black boxes," symbolic regression discovers the underlying mathematical equation itself.

By combining the raw execution speed and auto-vectorization of C++ with the developer experience and memory safety of a modern .NET C# API, SymbolicRegressionNet achieves the best of both worlds.

## Features
- **C++ Core Engine**: High-performance evolutionary search using Age-Fitness Pareto (AFP) and ε-Lexicase Selection.
- **Constant Optimization**: Uses Levenberg-Marquardt optimizer with Forward-Mode Automatic Differentiation to tune constants in the discovered equations.
- **Zero-Copy Interop**: Data ingested in C# is pinned in memory, allowing the C++ engine to compute directly on C# arrays without expensive data copies.
- **SIMD/AVX2 Vectorization**: Calculates outputs and errors across entire datasets efficiently utilizing CPU intrinsics.

## Installation
*(Instructions to be added based on NuGet or pre-compiled binary distribution method)*

## Quick Start

The easiest way to get started is to use the engine to solve for an unknown dataset.

```csharp
using SymbolicRegressionNet.Sdk;
using SymbolicRegressionNet.Sdk.Data;
using SymbolicRegressionNet.Sdk.Interop;

// 1. Prepare data
var dataset = Dataset.FromCsv("data.csv", targetColumn: "y");

// 2. Configure engine options
var options = new NativeOptions
{
    PopulationSize = 1000,
    MaxGenerations = 100
};

// 3. Initialize the regressor and fit
using var regressor = new SymbolicRegressor(dataset, valDataset: null, options, timeLimit: null);
var result = await regressor.FitAsync();

// The engine returns the absolute best equation it found:
Console.WriteLine($"Discovered Equation: {result.BestEquation}");

// It also returns a "Hall of Fame" representing the best trade-offs between simplicity and accuracy:
foreach (var model in result.HallOfFame)
{
    Console.WriteLine($"Complexity: {model.Complexity} | R2: {model.R2:F4} | Equation: {model.Equation}");
}
```

> **Tip:** You can check out the `tests/SymbolicRegressionNet.Benchmarks/Program.cs` file in the repository for a complete runnable evaluation setup.

---

## Understanding the Results
Because SymbolicRegressionNet explores infinite possible equations, it needs a way to score and rank them. Here is a quick guide to understanding the engine's output:

### 1. The Hall of Fame (Pareto Front)
Usually, you don't just want one single equation. You want a choice between a very simple equation that is "good enough" vs. a massive, highly complex equation that is perfectly accurate but hard to understand. 

The **Hall of Fame** (also known as the Pareto Front) contains a list of models where *no other model is both simpler and more accurate*. This allows you to inspect the results and choose the equation that best balances math simplicity with data accuracy for your specific use case.

### 2. $R^2$ (R-Squared)
The engine scores how accurate an equation is using the **$R^2$ Metric** (Coefficient of Determination), combined with Mean Squared Error (MSE).
*   **$R^2 = 1.0$**: A perfect fit. The mathematical equation perfectly predicts the target variable.
*   **$R^2 = 0.0$**: A terrible fit. The equation is no better than just guessing the average value of the data. 
*   **$R^2 < 0.0$**: The equation is completely incorrect.

*For scientific data, you typically want an $R^2$ as close to `1.0` as possible.*

### 3. Complexity
An equation's complexity is essentially how "long" or complicated the math formula is. 
*   `x * x` has a very low complexity.
*   `sin(exp(x / 4.2)) * cos(x)` has a high complexity. 

The engine actively tries to simplify equations and punish complex formulas unless that complexity provides a massive boost to the $R^2$ score.

---

## Benchmarks
SymbolicRegressionNet has been heavily benchmarked against 100 benchmark datasets (both physical/Feynman datasets and synthetic math pathological sets). 

Please refer to the [Benchmark.md](Benchmark.md) whitepaper for extensive results, evaluation criteria, and dataset methodology.
