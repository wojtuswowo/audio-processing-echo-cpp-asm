# Audio Echo Effect – C/C++ vs x64 Assembly (SIMD) Performance Comparison

## Project Overview

This project implements a digital **audio echo effect** for 16-bit WAV files and compares performance between:

- C/C++ implementation
- x86-64 Assembly implementation with SIMD instructions
- Multi-threaded processing

The goal of the project was to analyze the impact of:
- Low-level optimization
- SIMD vectorization
- Multi-threading
- Memory management strategies

The application includes a Windows Forms GUI written in C# and a high-performance processing DLL implemented in C/C++ and Assembly.

---

## Features

- 16-bit WAV (44.1 kHz) file processing
- Configurable echo parameters:
  - Delay (ms)
  - Bounce (number of repetitions)
  - Feedback (%)
  - Processing mode (C/C++ or ASM)
  - Number of threads
- SIMD vectorized assembly implementation (XMM registers)
- Saturating arithmetic for audio safety
- Performance measurement using `Stopwatch`
- CSV export for benchmark analysis
- Automatic CPU core detection
- Safe buffer handling without boundary checks in ASM

---

## Architecture

### GUI Layer (C# – Windows Forms)

- File selection
- Parameter configuration
- Thread management
- Benchmark timing
- Output file generation

### Processing Layer (C/C++ DLL)

Two implementations:

1. **C/C++ version**
   - Clean high-level implementation
   - Multi-threaded processing

2. **x64 Assembly version**
   - SIMD (128-bit XMM registers)
   - Processes 8 audio samples at once
   - Uses `pmulhw` and `vpaddsw`
   - Q15 fixed-point feedback scaling
   - Saturating arithmetic
   - Optimized delay handling (no boundary checks)

---

## Echo Algorithm

For each audio sample:
output[n] = input[n] +
feedback¹ * input[n - delay] +
feedback² * input[n - 2*delay] +
…


Key optimizations:

- Zero-padding at buffer start to avoid bounds checking
- Delay multiplication replaced with incremental addition
- SIMD processing of 8 samples per iteration
- Feedback power accumulation inside register

---

##  Performance Results

### Observations:

- Assembly implementation outperforms C/C++ version
- Performance gap increases with larger input files
- SIMD vectorization significantly reduces processing time
- Multi-threading improves performance up to CPU core saturation
- For small files, thread overhead reduces relative gains

Time complexity grows approximately linearly with input size.

---

## Testing

The application was tested for:

- Functional correctness (audible verification)
- Parameter sensitivity:
  - Increasing delay → longer echo spacing
  - Increasing bounce → more repetitions
  - Increasing feedback → slower echo decay
- Stability under extreme parameter values
- Repeated execution without restart
- Consistency between C/C++ and ASM output

---

## How to Run

1. Open the solution in Visual Studio (x64 configuration).
2. Build the DLL (C/C++ and ASM).
3. Run the Windows Forms application.
4. Select a 16-bit WAV file (44.1 kHz).
5. Configure parameters.
6. Click **Start**.
7. Output file is saved in the same directory:
   - `-delay-cpp.wav`
   - `-delay-asm.wav`

---

## Technical Highlights

- x86-64 Assembly
- SIMD (XMM registers)
- Fixed-point arithmetic (Q15)
- Multi-threading
- Memory-safe buffer extension strategy
- Performance benchmarking
- Hybrid architecture (C# + Native DLL)

---

## Technologies Used

- C#
- C/C++
- x86-64 Assembly
- SIMD
- Windows Forms
- Visual Studio
- CSV + Excel (performance analysis)

---
