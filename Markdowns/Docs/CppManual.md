# A Manual to C++ (Through SymbolicRegressionNet)

Welcome! If you are new to C++ but want to understand how the **SymbolicRegressionNet** core engine works, this manual is for you. We will use actual examples from the project to explain core C++ concepts.

C++ is a compiled, highly-performant language that gives you direct control over computer memory. It powers game engines, operating systems, and heavy-duty mathematical software like our Symbolic Regression engine.

---

## 1. The Basics: Headers, Includes, and Namespaces

### `#include` and `#pragma once`
In C#, you use `using System;` to bring in functionality. In C++, code is split into **header files** (`.h`) and **source files** (`.cpp`). To use code from a header file, you `#include` it.

Look at the top of `engine.h`:
```cpp
#pragma once
#include "tree.h"
#include "types.h"
#include <vector>
```
*   `#include <vector>` brings in the standard library's resizable array (like C#'s `List<T>`).
*   `#include "types.h"` (with quotes) brings in our own local project file.
*   `#pragma once` tells the compiler to only include this file once per compilation, preventing infinite loops if files include each other.

### Namespaces
Like C#'s `namespace`, C++ groups related code together. In `optimizer.h`, you see:
```cpp
namespace optimizer {
    class LevenbergMarquardtOptimizer { ... };
}
```
To use this class elsewhere, you either write `optimizer::LevenbergMarquardtOptimizer` (the `::` is the scope resolution operator) or use `using namespace optimizer;`.

---

## 2. Structs, Classes, and Access Modifiers

In C++, `struct` and `class` are almost identical. The only difference is that in a `class`, things are `private` by default, whereas in a `struct`, things are `public` by default.

### A Simple Data Container (`struct`)
In `types.h`, we define `Options`:
```cpp
struct Options {
    int      population_size;
    int      max_generations;
    double   mutation_rate;
};
```
This is a plain data container. It holds variables and has no logic. We use it exactly like a C# `struct` to pass settings into the Core Engine.

### A Logic Container (`class`)
In `engine.h`, we define the `Engine` class:
```cpp
class Engine {
public:
    explicit Engine(const Options& opts); // Constructor
    void Step(int generations);           // Method anyone can call

private:
    Population population_;               // Data only the Engine can see
    void RunOneGeneration();              // Helper method
};
```
*   **Encapsulation**: We hide the internal workings (`population_`, `RunOneGeneration`) behind the `private:` keyword so external code cannot mess with it.
*   **The Constructor**: `Engine(...)` is the method called when you create an Engine object.

---

## 3. Pointers and Memory: The Heart of C++ Performance

This is where C++ differs most from languages like C# or Python. In C#, the Garbage Collector manages memory. In C++, **you** manage it, usually via **Pointers**.

A pointer is simply a variable that stores a **memory address**.

Look at our `DataView` struct in `types.h`:
```cpp
struct DataView {
    double** columns;   
    double*  target;    
    int      rows;      
    int      cols;      
};
```

*   `double* target`: This means `target` points to the memory address where the first `double` (a 64-bit decimal number) of our target dataset lives. It behaves like an array: `target[5]` gets the 6th number.
*   `double** columns`: A pointer to a pointer! `columns[0]` gives you a `double*` (the array for the first feature column). Then `columns[0][5]` gives you the 6th data point of the 1st feature column.

**Why use pointers here? (Zero-Copy Interop)**
We create the dataset in C#, but we want to process it in C++. Instead of copying millions of numbers, C# just tells C++: *"The data is located at memory address `0xABC123`"*. C++ uses a pointer to look directly at that C# memory. This is called **zero-copy interop**, and it makes the application incredibly fast.

---

## 4. `const`, References (`&`), and Pointers (`*`) in Methods

When passing variables to methods, C++ gives you choices to optimize performance.

Look at this method snippet:
```cpp
void EvaluateIndividual(Individual& ind, const DataView& data);
```

*   **Pass by Value**: If we wrote `DataView data`, C++ would copy the entire 48-byte object. Not terrible, but unnecessary.
*   **Pass by Reference (`&`)**: `Individual& ind` means we pass a direct reference to the original `Individual` object. If `EvaluateIndividual` changes `ind.fitness` inside the method, the original object is updated. (Like `ref` in C#).
*   **Const Reference (`const &`)**: `const DataView& data` means we pass the original object to avoid copying, but we place a `const` lock on it. The compiler will throw an error if this method tries to modify `data`. It is a "read-only reference."

---

## 5. C API Export (`extern "C"`)

Because C++ allows methods to be "overloaded" (having multiple methods with the same name but different parameters), the C++ compiler "mangles" the names of methods in the compiled `.dll` file. For example, `Engine::Step(int)` might become `_ZN6Engine4StepEi`.

C# cannot easily guess these mangled names. To allow C# to call our methods, we expose an API using the older, simpler C language standard.

In `api.h` and `api.cpp`:
```cpp
extern "C" {
    __declspec(dllexport) void* SRNet_CreateEngine(Options* opts) {
        return new Engine(*opts);
    }
}
```
*   `extern "C"`: Tells the C++ compiler "Do not mangle these function names! Treat them like basic C functions."
*   `__declspec(dllexport)`: A Windows-specific command that tells the compiler to make this function visible to the outside world (like our C# app).
*   `void*`: A "raw pointer to anything." C# holds onto this memory address as an `IntPtr` and passes it back to us later.

---

## 6. Extreme Optimization: SIMD (`__m256d`)

In `evaluator.h`, you might see something terrifying:
```cpp
__m256d EvaluateExpression_AVX2(...)
```

Computers process data using Central Processing Units (CPUs). Normally, a CPU adds two numbers at the same time: `A + B = C`. This is called **Scalar** processing.

Modern CPUs have **SIMD** (Single Instruction, Multiple Data) capabilities. **AVX2** is a specific SIMD instruction set. 
`__m256d` is a special 256-bit wide variable. Instead of holding one 64-bit `double`, it holds exactly **four**.

When the symbolic regression engine uses AVX2, evaluating an expression like `X + Y` doesn't just evaluate one row of data. It pulls in 4 rows of `X`, 4 rows of `Y`, adds them in a single clock cycle, and spits out 4 answers. This makes the math engine nearly 4x faster!
