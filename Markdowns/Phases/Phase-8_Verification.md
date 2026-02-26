# Phase 8 — Verification

> **Phase Goal:** Validate that the entire SymbolicRegressionNet scaffold — C++ core, CUDA stub, C# SDK, and cross-language interop — compiles, links, and produces correct outputs, establishing the build-system baseline that all future development relies upon.

---

## Overview

Phase 8 is the **quality gate** of the scaffold implementation. Phases 1-7 produced code; Phase 8 proves that code *works*. This phase does not add new functionality — it validates that the existing code compiles without errors, that the C++ and C# projects can build independently and together, that struct layouts match across languages, and that the interop pipeline (C++ shared library → P/Invoke → C# SDK) is wired correctly.

Verification at this stage is intentionally **build-focused rather than behavior-focused**. Since Phases 2-7 produce skeleton implementations (function signatures, struct definitions, architectural scaffolding), there is limited runtime behavior to test. The primary verification is that the toolchain is configured correctly and all components integrate.

---

## Why This Phase Exists

### The Problem It Solves

Without verification, the team moves forward on a foundation that *might* be broken:
- The CMakeLists.txt might have a wrong flag that only manifests when a specific header is included.
- The C# project might reference a NuGet package version that conflicts with .NET 8.
- The native library copy target might place the DLL in the wrong directory.
- The `NativeOptions` struct in C# might have a different `sizeof` than the C++ version.
- The CUDA stub might fail to compile because the CMake rules don't correctly invoke `nvcc`.

Each of these failures, if discovered later, forces a context switch: the developer working on Phase 4 must stop, debug a Phase 1 build issue, fix it, and then re-verify their own work. Phase 8 catches these issues early, when they are cheap to fix.

### Its Purpose in the Grand Scheme

Phase 8 establishes the **green baseline** — the state where `cmake --build` and `dotnet build` both succeed. From this point forward, every subsequent task can be validated incrementally: "does my change still compile?" This is the foundation used for continuous integration (GitHub Actions workflow, which the QA-Engineer will configure later). A broken baseline means no CI, which means no quality gates, which means bugs compound.

---

## Dependencies

| Depends On | Description |
|-----------|-------------|
| **Phase 1** | Build system (CMakeLists.txt, .csproj, .sln) |
| **Phase 2** | C++ header files (tree.h, types.h) |
| **Phase 3** | C++ engine implementation (engine.h/cpp) |
| **Phase 4** | C++ evaluator implementation (evaluator.h/cpp) |
| **Phase 5** | C++ optimizer, C-API, CUDA stub |
| **Phase 6** | C# data layer classes |
| **Phase 7** | C# API, interop, and result types |

### What Depends on Phase 8

| Phase | Dependency Reason |
|-------|-------------------|
| **All future development** | The verified baseline ensures new code can be incrementally validated |
| **CI/CD setup** | GitHub Actions workflows will replicate the verification steps defined here |

> Phase 8 is a terminal phase in the scaffold plan. It has no Phase 9. But it produces the baseline that all *future* development iterations depend on.

---

## Tasks

### Task 8.1 — Verify C++ Core Compiles with CMake

> **Assigned to:** `QA-Engineer`

**Description:**
Execute the CMake build pipeline for the C++ core library and verify it produces a valid shared library without errors or warnings.

**Verification steps:**

```bash
cd src/SymbolicRegressionNet.Core
cmake -B build -G "Visual Studio 17 2022" -DCMAKE_CXX_STANDARD=20
cmake --build build --config Release
```

**Expected outcomes:**
1. **CMake configure succeeds:** All headers resolve, all source files are found, C++20 standard is accepted by the compiler.
2. **Compilation succeeds:** All `.cpp` files compile without errors. Warnings should be treated as actionable — fix them or explicitly suppress with documented rationale.
3. **Linking succeeds:** The shared library (`SymbolicRegressionNetCore.dll` on Windows, `libSymbolicRegressionNetCore.so` on Linux) is produced in the build output directory.
4. **CUDA conditional compilation:** If CUDA toolkit is NOT installed, the build succeeds with `SRNET_NO_CUDA` defined. If CUDA IS installed, the `cuda_stub.cu` compiles cleanly.
5. **SIMD flags applied:** Verify (via build logs) that AVX2 flags are passed to the compiler.

**Failure triage:**
| Failure Type | Likely Root Cause | Fixing Owner |
|-------------|-------------------|--------------|
| Missing header include | Wrong header path in source | GP-Specialist or Perf-Engineer (whoever wrote the source) |
| Undefined symbol at link time | Missing source file in CMakeLists.txt | Perf-Engineer (CMake owner) |
| C++20 feature not supported | Compiler version too old | Perf-Engineer |
| CUDA compilation error | Wrong CUDA toolkit path or nvcc version | Perf-Engineer |

**Why this task matters:**
A C++ build that fails blocks Phase 3 (engine), Phase 4 (evaluator), and Phase 5 (optimizer/API) from being developed and tested. It also blocks Phase 7 (C# interop), because there's no native library to link against. This is the single highest-priority verification.

**Relies on:** All of Phases 1-5 (C++ components must be authored before they can be compiled).

---

### Task 8.2 — Verify C# SDK Builds with `dotnet build`

> **Assigned to:** `QA-Engineer`

**Description:**
Execute the .NET build pipeline for the C# SDK and verify it compiles without errors, produces a valid assembly, and resolves all NuGet dependencies.

**Verification steps:**

```bash
cd <repository-root>
dotnet restore SymbolicRegressionNet.sln
dotnet build SymbolicRegressionNet.sln --configuration Release --no-restore
```

**Expected outcomes:**
1. **NuGet restore succeeds:** `System.Text.Json` and `Microsoft.Extensions.Logging.Abstractions` are downloaded and cached.
2. **Compilation succeeds:** All `.cs` files compile without errors. The `AllowUnsafeBlocks` flag enables `GCHandle` and pointer operations without compiler errors.
3. **XML documentation generated:** `SymbolicRegressionNet.Sdk.xml` is produced alongside the DLL (validates that XML doc comments compile correctly).
4. **Nullable reference type analysis passes:** No `CS8600`/`CS8602` warnings from nullable analysis (or they are explicitly suppressed with `#nullable disable` in specific, documented locations).
5. **The output DLL is produced:** `bin/Release/net8.0/SymbolicRegressionNet.Sdk.dll` exists.

**Failure triage:**
| Failure Type | Likely Root Cause | Fixing Owner |
|-------------|-------------------|--------------|
| Missing type/namespace | Wrong `using` directive or missing project reference | API-Engineer |
| NuGet package not found | Wrong package name or version in `.csproj` | API-Engineer |
| Unsafe code error | `AllowUnsafeBlocks` not set in `.csproj` | API-Engineer |
| Nullable warning | Missing null check or wrong nullability annotation | API-Engineer or Architect |

**Why this task matters:**
If the C# SDK doesn't compile, Phase 7's API is unusable and no end-to-end testing is possible. NuGet resolution issues can be particularly tricky when running in CI environments with restricted network access — catching them now prevents CI failures later.

**Relies on:** Phases 1, 6, 7 (C# components).

---

### Task 8.3 — Verify Solution-Level Build and Integration

> **Assigned to:** `QA-Engineer`

**Description:**
Verify that the entire solution builds as a unit and that the inter-project dependencies (C++ → C#) are correctly wired — particularly that the native shared library ends up in the C# project's output directory.

**Verification steps:**

1. **Solution-level build:**
   ```bash
   dotnet build SymbolicRegressionNet.sln
   ```
   This should build the C# SDK project (and any test projects added later).

2. **Native library presence check:**
   After building, verify that the C++ native library (`SymbolicRegressionNetCore.dll` / `.so`) is present in the C# project's output folder:
   ```
   src/SymbolicRegressionNet.Sdk/bin/Release/net8.0/SymbolicRegressionNetCore.dll
   ```
   If missing, the `None` / copy target in the `.csproj` is misconfigured.

3. **Struct size cross-validation:**
   Write a small program (or test) that:
   - On the C++ side: `printf("sizeof(Options) = %zu\n", sizeof(Options));`
   - On the C# side: `Console.WriteLine($"sizeof(NativeOptions) = {Marshal.SizeOf<NativeOptions>()}");`
   - Compare the two values. They MUST be equal.
   - Repeat for `RunStats` / `NativeRunStats`.

4. **Minimal P/Invoke smoke test (optional but recommended):**
   If the engine is functional enough:
   ```csharp
   var opts = new NativeOptions { PopulationSize = 10, MaxGenerations = 1, ... };
   var handle = NativeMethods.SRNet_CreateEngine(ref opts);
   NativeMethods.SRNet_DestroyEngine(handle);
   // Should not crash (no segfault, no AccessViolationException)
   ```

**Why this task matters:**
The solution-level build is what CI will run. If it fails, no PR can be merged. The native library copy check is critical — a missing DLL causes `DllNotFoundException` at runtime, which is confusing because the C# project compiles fine. The struct size cross-validation catches the most insidious interop bugs: misaligned fields that silently produce wrong results.

**Relies on:** Tasks 8.1 and 8.2 (both projects must build independently first).

---

## Phase Completion Criteria

| Criterion | Verification |
|-----------|-------------|
| `cmake --build` succeeds for C++ core | Terminal output: 0 errors |
| `dotnet build` succeeds for solution | Terminal output: 0 errors |
| Native DLL present in C# output folder | File existence check |
| `sizeof(Options)` matches between C++ and C# | Cross-language comparison |
| `sizeof(RunStats)` matches between C++ and C# | Cross-language comparison |
| CUDA stub compiles (when toolkit available) | Conditional build log |
| No compiler warnings (or all are explained) | Warning count check |
| P/Invoke smoke test passes (optional) | No crash on create+destroy |

---

## Summary

Phase 8 is the capstone of the scaffold implementation. It does not create new code — it validates that all existing code, from CMake scripts to P/Invoke declarations, works together as a cohesive system. The three verification tasks (C++ build, C# build, solution integration) catch different categories of errors: compilation errors, linking errors, NuGet resolution, native library deployment, and cross-language struct alignment. Passing Phase 8 means the team has a **green baseline** from which all future development proceeds incrementally.
