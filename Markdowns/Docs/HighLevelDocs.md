# SymbolicRegressionNet: High-Level Documentation

## What is SymbolicRegressionNet?
SymbolicRegressionNet is a hybrid C++/C# project designed to perform **Symbolic Regression**. Symbolic regression is a type of machine learning that searches the space of mathematical expressions to find the model that best fits a given dataset, both in terms of accuracy and simplicity.

Unlike traditional regression (which fits coefficients to a pre-defined model structure like a line or a polynomial) or neural networks (which are often "black boxes"), symbolic regression discovers the underlying mathematical equation itself.

## Architecture Overview
The project is built on a two-tier architecture designed for maximum performance during the heavy mathematical search, while providing an easy-to-use, modern API for the user.

1. **C++ Core Engine (`SymbolicRegressionNet.Core`)**: The heavy lifting is done here. C++ is used because evaluating millions of mathematical expressions per second requires low-level memory control, aggressive compiler optimizations, and SIMD (Single Instruction, Multiple Data) instructions like AVX2.
2. **C# SDK (`SymbolicRegressionNet.Sdk`)**: This provides the user-facing API. It handles data ingestion, memory pinning (to allow C++ access to C# memory without copying), configuration building, and async orchestration.

### The Problem it Solves
Finding an analytical equation for data is an NP-hard problem. The search space of all possible equations is infinite and discrete. SymbolicRegressionNet uses **Genetic Programming (GP)** to heuristically explore this space.

## How the Evolutionary Process Works
The C++ Core Engine uses evolutionary algorithms inspired by biological evolution:

1. **Initialization**: A `Population` of random math expressions (represented as abstract syntax trees) is generated.
2. **Evaluation**: Each expression is evaluated against the training dataset. We calculate its error (e.g., Mean Squared Error). Fast execution is critical here.
3. **Survivor Selection**: The engine determines which expressions get to survive to the next generation. It uses **Age-Fitness Pareto (AFP)**, which balances how well an equation fits the data (fitness) versus how recently it was generated (age), preventing premature convergence on local optima.
4. **Parent Selection**: To create new expressions, parents are selected using **ε-Lexicase Selection**, which promotes diversity by evaluating expressions across distinct test cases rather than aggregated fitness.
5. **Crossover & Mutation**: Two parent expressions are combined (crossover) to create an offspring. Random changes (mutation) are also applied to maintain genetic diversity.
6. **Constant Optimization**: Before an offspring is fully evaluated, its numeric constants might be tuned using gradients (Levenberg-Marquardt optimizer with Forward-Mode Automatic Differentiation).
7. **Iteration**: This process repeats for thousands of generations.

## High-Level Component Roles

### C# SDK
*   **`SymbolicRegressor`**: The main orchestrator. You give it data and run `FitAsync()`. It creates the C++ engine, passes pointers to the pinned data, and polls for generational statistics.
*   **`Dataset` and `DataView`**: C# loads the data. The data is "pinned" in memory, meanings the garbage collector won't move it. A `DataView` struct containing raw memory pointers is passed to C++.
*   **Zero-Copy Interop**: The key to performance. Data is never copied from C# to C++. C++ computes directly on C# arrays.

### C++ Core
*   **`Engine`**: Runs the evolutionary generational loop.
*   **`Evaluator` & `ADEvaluator`**: Takes a mathematical expression and the dataset, and computes the outputs. Uses SIMD/AVX2 for bulk evaluation across many data rows simultaneously.
*   **Tree (`Individual`, `Token`)**: Expressions are stored as flat arrays of `Token`s in reversed Polish notation (postfix). This avoids deep object trees and pointers, making it cache-friendly.
*   **`GeneticOps`**: Performs the Crossover and Mutation on the flat token arrays.
*   **`Optimizer`**: Tunes the constants ($c_1, c_2$, etc.) inside trees to perfectly fit the data.

## Why C++ and C#?
*   **C#** provides a fantastic ecosystem for data loading, async programming, dependency injection, and application creation.
*   **C++** provides raw, predictable execution speed, explicit memory layouts (Struct of Arrays `DataView`), and auto-vectorization.
By bridging the two via P/Invoke, SymbolicRegressionNet achieves the "best of both worlds": the performance of a native C++ application with the developer experience of a modern .NET library.
