# Phase 1 — Project Structure & Build System

> **Phase Goal:** Establish the foundational directory structure, build configurations, and project scaffolding that every subsequent phase depends upon.

---

## Overview

Phase 1 is the **bedrock of the entire SymbolicRegressionNet project**. Before any algorithm can be implemented, any kernel written, or any API designed, the project needs a well-defined skeleton — folders, build scripts, project files, and dependency configurations that allow all seven team members to work in parallel without stepping on each other.

This phase produces **zero runtime functionality**. Its output is purely structural: a compilable (but empty) solution that proves the toolchain works end-to-end. Yet it is arguably the most critical phase, because every architectural mistake made here propagates through all future phases. A misplaced module boundary, a wrong CMake flag, or an incorrect interop convention will cost days of rework later.

---

## Why This Phase Exists

### The Problem It Solves

Without a shared, compiling scaffold:

- Team members cannot begin parallel work — they have no agreed-upon file locations, namespace conventions, or build targets.
- Integration surprises appear late when independently-developed components fail to link, reference, or marshal correctly.
- Rework compounds exponentially — a structural change in Phase 5 that should have been caught in Phase 1 can invalidate work across Phases 2–7.

### Its Purpose in the Grand Scheme

SymbolicRegressionNet is a **hybrid C++/C# system**. The C++20 core performs computationally intensive evolutionary search, while the C# SDK provides the user-facing API. This split demands:

1. **Two independent build systems** (CMake for C++, MSBuild/.NET CLI for C#) that produce compatible artifacts.
2. **A well-defined interop boundary** — the C++ shared library must export C-linkage symbols that the C# P/Invoke layer can discover at runtime.
3. **Consistent directory conventions** so the native DLL ends up in the right `bin/` folder at build time.

Phase 1 establishes all of this infrastructure before any line of algorithm code is written.

---

## Dependencies

| Depends On | Description |
|-----------|-------------|
| **Nothing** | This is the first phase — it has no predecessors. |

### What Depends on Phase 1

| Phase | Dependency Reason |
|-------|-------------------|
| **Phase 2** (Data Structures) | Needs the C++ project and header locations to define types |
| **Phase 3** (Engine) | Needs the C++ project to compile engine code |
| **Phase 4** (Evaluator) | Needs the C++ project and SIMD compile flags |
| **Phase 5** (Optimizer & C-API) | Needs the C++ project, CUDA cmake rules, and C-API export conventions |
| **Phase 6** (Data Layer) | Needs the C# project file and folder structure |
| **Phase 7** (API & Interop) | Needs both the C# project and the native library copy targets |
| **Phase 8** (Verification) | Needs the build system to actually compile both projects |

> Every downstream phase inherits the conventions set here. This phase is a hard prerequisite for the entire project.

---

## Tasks

### Task 1.1 — Create the Directory Structure

> **Assigned to:** `Architect`

**Description:**
Create the complete directory tree that will house all source code, headers, build configuration, and documentation for the project. The structure must reflect the architectural layering: C++ Core → C-API → C# Interop → C# SDK → Tests.

**Detailed layout:**

```
SymbolicRegressionNet/
├── src/
│   ├── SymbolicRegressionNet.Core/        ← C++20 core library
│   │   ├── include/                       ← Public C++ headers
│   │   ├── src/                           ← C++ implementation files
│   │   ├── cuda/                          ← CUDA kernel source files
│   │   └── CMakeLists.txt                 ← CMake build file
│   └── SymbolicRegressionNet.Sdk/         ← C# .NET 8 SDK project
│       ├── Api/                           ← Public API surface
│       ├── Data/                          ← Dataset and splitting logic
│       ├── Interop/                       ← P/Invoke and pinned buffers
│       ├── Reporting/                     ← Result types and export
│       └── SymbolicRegressionNet.Sdk.csproj
├── tests/                                 ← Test projects (future)
├── Markdowns/                             ← Documentation
├── SymbolicRegressionNet.sln              ← Solution file
└── .gitignore
```

**Why this task matters:**
The directory layout encodes the architectural module boundaries. `Core` and `Sdk` are separate projects because they target different runtimes (native C++ vs. managed .NET). Within the SDK, subdirectories (`Api/`, `Data/`, `Interop/`, `Reporting/`) map to logical layers — this prevents spaghetti references and enforces separation of concerns.

**Relies on:** Nothing — this is the first task.

---

### Task 1.2 — Create CMakeLists.txt for C++20 Core

> **Assigned to:** `Perf-Engineer`

**Description:**
Author the CMake build file for the native C++ core library. This file must:

- Require CMake 3.20+ and target the C++20 standard.
- Define a **shared library** target named `SymbolicRegressionNetCore`.
- Glob or explicitly list header files from `include/` and source files from `src/`.
- Apply SIMD compile flags conditionally:
  - `-mavx2` / `/arch:AVX2` on platforms that support it.
  - Optional AVX-512 flag gated behind a CMake option.
- Integrate **optional CUDA support** via `find_package(CUDAToolkit)`:
  - If CUDA is found, add `cuda/` source files to the target.
  - If not, define a preprocessor macro `SRNET_NO_CUDA` so C++ code can conditionally compile without CUDA.
- Set the output library name and install rules so the `.dll`/`.so` can be copied to the C# project's output directory.

**Why this task matters:**
The CMake file governs how the performance-critical C++ core is compiled. Getting the SIMD flags right enables Phase 4's AVX2 evaluation; getting the CUDA integration right unblocks Phase 5's GPU stub. If the shared library doesn't export symbols correctly, Phase 7's P/Invoke layer will fail to bind at runtime.

**Relies on:** Task 1.1 (directory structure must exist first).

---

### Task 1.3 — Create the .NET 8 Class Library Project

> **Assigned to:** `API-Engineer`

**Description:**
Create the `SymbolicRegressionNet.Sdk.csproj` project file for the C# SDK. This file must:

- Target `.NET 8.0`.
- Enable `AllowUnsafeBlocks` (required for `GCHandle`, `Span<T>`, and pointer-based interop with the native library).
- Enable nullable reference types (`<Nullable>enable</Nullable>`).
- Configure XML documentation generation (`<GenerateDocumentationFile>true</GenerateDocumentationFile>`).
- Include a post-build or `None` item that copies the compiled C++ native library (`SymbolicRegressionNetCore.dll` / `.so`) to the output directory so it is discoverable by `DllImport` at runtime.
- Reference `System.Text.Json` and `Microsoft.Extensions.Logging.Abstractions` as NuGet dependencies.

**Why this task matters:**
This project file defines the .NET compilation contract — which language features are available, which dependencies are pulled in, and critically, how the native library gets bundled. Without the native library copy target, the SDK will compile but crash at runtime with `DllNotFoundException`.

**Relies on:** Task 1.1 (directory structure) and indirectly Task 1.2 (to know the native library output name/path).

---

### Task 1.4 — Create the Solution File

> **Assigned to:** `Architect`

**Description:**
Create `SymbolicRegressionNet.sln` at the repository root, referencing the C# SDK project. The solution file serves as the single entry point for `dotnet build` and Visual Studio. At this stage it contains only the SDK project; test projects will be added in Phase 8.

The solution must:

- Use the standard Visual Studio solution format.
- Include a `src` solution folder grouping the SDK project.
- Optionally include a `docs` solution folder linking to key markdown files for documentation discoverability.

**Why this task matters:**
The solution file is the entry point for the IDE and for CI/CD build commands. Without it, `dotnet build` doesn't know which projects to compile, and developers can't open the full project in Visual Studio or Rider. A malformed `.sln` can cause silent project omissions during builds.

**Relies on:** Task 1.3 (the `.csproj` must exist to be referenced by the `.sln`).

---

## Phase Completion Criteria

| Criterion | Verification |
|-----------|-------------|
| Directory tree matches the blueprint | Manual inspection |
| `cmake -B build` succeeds in the Core directory | Terminal command |
| `dotnet build` succeeds on the solution | Terminal command |
| Native library copy target works | Build output contains the native DLL/SO |
| No circular project references | Solution structure review |

---

## Summary

Phase 1 produces an empty-but-compilable scaffold. It sets the architectural conventions that every subsequent phase inherits: file locations, namespace structure, build flags, and native interop rules. It is entirely structural — no algorithms, no data structures, no business logic — but it is the single most important phase for preventing late-stage integration failures.
