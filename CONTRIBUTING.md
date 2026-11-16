# Contributing to DictionaryEntry

Contributions, bug reports, and ideas are welcome! Please review the guidelines below before opening a pull request.

## Prerequisites

- **.NET 11.0 SDK** (`11.0.100` or newer, pinned in [`global.json`](global.json))
- **.NET 9.0 and .NET 10.0 runtimes** (required to execute the multi-targeted test suite across `net9.0`, `net10.0`, and `net11.0`)

## Building, Formatting, and Packaging

Build the entire solution in `Release` mode (warnings are treated as errors):

```bash
dotnet build --configuration Release
```

Verify formatting and code-style rules against [`.editorconfig`](.editorconfig):

```bash
dotnet format --verify-no-changes
```

Validate NuGet packaging and multi-target compatibility (`EnablePackageValidation`):

```bash
dotnet pack src/DictionaryEntry/DictionaryEntry.csproj --configuration Release
```

## Running Tests

Run the unit, edge-case, layout-parity, and 600-seed differential fuzz suites across all target frameworks:

```bash
dotnet test --configuration Release
```

## Running Benchmarks

Any change touching `src/DictionaryEntry` must maintain **zero performance and allocation regressions**. Run targeted or full BenchmarkDotNet suites in `Release` mode:

```bash
# Run a targeted suite
dotnet run -c Release --project src/DictionaryEntry.Benchmarks -- --filter "*GetOrAdd*"

# Run all benchmarks
dotnet run -c Release --project src/DictionaryEntry.Benchmarks -- --filter "*"
```

## Design & Performance Guardrails

- **Zero regressions on hot paths:** Core entry lookups and updates execute in ~1–5 ns. Avoid adding branches, widening `Entry` / `OccupiedEntry` / `VacantEntry` structs, or introducing abstractions that inhibit RyuJIT inlining.
- **Zero unexpected allocations:** All non-capturing operations (and `ReadOnlySpan<char>` cache hits / read-only misses) must allocate `0 B`.
- **`Dictionary<TKey, TValue>` layout safety:** `DictionaryAccess` relies on `[UnsafeAccessor]` and `RawEntry` layout parity with the BCL. Any change to internal dictionary access must pass both `LayoutParityTests` and `DifferentialFuzzTests` across `.NET 9`, `.NET 10`, and `.NET 11`.
