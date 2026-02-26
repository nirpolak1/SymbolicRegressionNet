# SymbolicRegressionNet — Development Team

## Project Overview

**SymbolicRegressionNet** is a next-generation symbolic regression engine built in C# / .NET. It discovers closed-form mathematical expressions from data using evolutionary computation, neural-guided search, and multi-objective optimization. The system must handle expression tree manipulation, GPU-accelerated fitness evaluation, robust numerical methods, and a clean API layer.

---

## Team Composition

| # | Role | Codename | Primary Focus |
|---|------|----------|---------------|
| 1 | **Project Architect** | `Architect` | System design, module boundaries, design patterns, and cross-cutting concerns |
| 2 | **Genetic Programming Specialist** | `GP-Specialist` | Expression trees, genetic operators (crossover, mutation, selection), and population management |
| 3 | **Machine Learning / Neural Network Engineer** | `ML-Engineer` | Neural-guided search, surrogate models, transformer-based priors, and learned heuristics |
| 4 | **Numerical Methods & Optimization Engineer** | `NumOpt-Engineer` | Constant optimization, gradient-free solvers, numerical stability, and fitness landscape analysis |
| 5 | **Performance & Systems Engineer** | `Perf-Engineer` | SIMD, GPU compute (CUDA/OpenCL via ILGPU), parallelism, memory layout, and profiling |
| 6 | **Data Pipeline & API Engineer** | `API-Engineer` | Data ingestion, preprocessing, public API design, serialization, and configuration |
| 7 | **Testing & Quality Assurance Engineer** | `QA-Engineer` | Unit/integration/property-based testing, benchmarking, regression suites, and CI/CD |

---

## Workflow & Collaboration Model

```
┌────────────────────────────────────────────────────────────┐
│                     Architect                              │
│   Defines interfaces, module contracts, reviews designs    │
└──────────┬───────────────┬──────────────────┬──────────────┘
           │               │                  │
     ┌─────▼─────┐  ┌─────▼──────┐   ┌──────▼───────┐
     │ GP-Spec.  │  │ ML-Eng.    │   │ NumOpt-Eng.  │
     │ Core algo │  │ Neural     │   │ Const. opt.  │
     │ evolution │  │ guidance   │   │ Numerics     │
     └─────┬─────┘  └─────┬──────┘   └──────┬───────┘
           │               │                  │
     ┌─────▼───────────────▼──────────────────▼──────┐
     │              Perf-Engineer                     │
     │   GPU kernels, SIMD, parallel evaluation       │
     └─────────────────────┬─────────────────────────┘
                           │
     ┌─────────────────────▼─────────────────────────┐
     │              API-Engineer                      │
     │   Public surface, data pipeline, config        │
     └─────────────────────┬─────────────────────────┘
                           │
     ┌─────────────────────▼─────────────────────────┐
     │              QA-Engineer                       │
     │   Tests, benchmarks, CI/CD, quality gates      │
     └───────────────────────────────────────────────┘
```

## Decision-Making Rules

1. **Architecture decisions** (module boundaries, dependency direction, public interfaces) are owned by the **Architect**. All members propose; the Architect approves.
2. **Algorithm choices** within a module are owned by the specialist for that module (GP-Specialist, ML-Engineer, or NumOpt-Engineer).
3. **Performance constraints** are enforced by the Perf-Engineer. Any code touching hot loops or GPU kernels requires their sign-off.
4. **API surface changes** must be approved by both the Architect and the API-Engineer.
5. **No code merges** without passing the QA-Engineer's test suite and review.

## Communication Protocol

- Each team member operates within their defined scope and **must not** make changes outside their area without consulting the relevant owner.
- When a task spans two or more roles, the Architect coordinates a joint design session.
- All design decisions are documented as Architecture Decision Records (ADRs) by the Architect.

## Technology Stack

| Concern | Technology |
|---------|-----------|
| Language | C# 12 / .NET 8+ |
| Numerics | MathNet.Numerics |
| GPU Compute | ILGPU or ComputeSharp |
| Serialization | System.Text.Json |
| Testing | xUnit, FluentAssertions, BenchmarkDotNet |
| CI/CD | GitHub Actions |
