# DictionaryEntry Benchmarks

Full BenchmarkDotNet results for the current version of `DictionaryEntry` (`v0.4.0`), comparing idiomatic hand-written `Dictionary<TKey, TValue>` code (`TryGetValue` + indexer) against `DictionaryEntry`.

## Running the Benchmarks

Run the interactive benchmark switcher or execute all benchmarks in `Release` mode:

```bash
dotnet run -c Release --project src/DictionaryEntry.Benchmarks -- --filter "*"
```

Run a specific benchmark class or method filter:

```bash
dotnet run -c Release --project src/DictionaryEntry.Benchmarks -- --filter "*GetOrAdd*"
dotnet run -c Release --project src/DictionaryEntry.Benchmarks -- --filter "*PatternMatching*"
```

## Environment

```text
BenchmarkDotNet v0.15.8, macOS Tahoe 26.6.2 (25G83) [Darwin 25.6.0]
Apple M1 Pro, 1 CPU, 8 logical and 8 physical cores
.NET SDK 11.0.100
  [Host] : .NET 11.0.0 (11.0.0, 11.0.26.53018), Arm64 RyuJIT armv8.0-a

MaxRelativeError=0.01  Toolchain=InProcessNoEmitToolchain  InvocationCount=128000 (384000 for int)
OperationsPerInvoke=256  MaxIterationCount=15  MinIterationCount=8
MaxWarmupIterationCount=8  MinWarmupIterationCount=5  RunStrategy=Throughput
```

## Results

### Basic Operations

#### `GetOrAddBenchmarks` (`Dictionary<int, int>`)

| Method | Mean | Ratio | Allocated |
|---|---:|---:|---:|
| `GetOrAdd_Traditional_Exists` | `1.854 ns` | `1.00` | `-` |
| `GetOrAdd_Entry_Exists` | `2.152 ns` | `1.16` | `-` |
| | | | |
| `GetOrAdd_Traditional_NotExists` | `5.440 ns` | `1.00` | `-` |
| `GetOrAdd_Entry_NotExists` | `3.994 ns` | `0.73` | `-` |

#### `DefaultValueBenchmarks` (`Dictionary<string, int>` / `Dictionary<string, string?>`)

| Method | Mean | Ratio | Allocated |
|---|---:|---:|---:|
| `DefaultValue_Traditional_Exists` | `4.215 ns` | `1.00` | `-` |
| `DefaultValue_Entry_Exists` | `4.662 ns` | `1.11` | `-` |
| | | | |
| `DefaultValue_Traditional_NotExists` | `11.997 ns` | `1.00` | `-` |
| `DefaultValue_Entry_NotExists` | `8.697 ns` | `0.73` | `-` |
| | | | |
| `DefaultValue_TraditionalString_NotExists` | `11.026 ns` | `1.00` | `-` |
| `DefaultValue_EntryString_NotExists` | `7.323 ns` | `0.66` | `-` |

#### `IncrementCounterBenchmarks` (`Dictionary<string, int>`)

| Method | Mean | Ratio | Allocated |
|---|---:|---:|---:|
| `IncrementCounter_Traditional_Exists` | `9.187 ns` | `1.00` | `-` |
| `IncrementCounter_EntryAndModify_Exists` | `6.174 ns` | `0.67` | `-` |
| | | | |
| `IncrementCounter_Traditional_NotExists` | `11.803 ns` | `1.00` | `-` |
| `IncrementCounter_EntryAndModify_NotExists` | `8.657 ns` | `0.73` | `-` |
| | | | |
| `IncrementByAmount_Traditional_Exists` | `9.188 ns` | `1.00` | `-` |
| `IncrementByAmount_EntryAndModify_Exists` | `21.612 ns` | `2.35` | `80 B` |
| `IncrementByAmount_EntryAndModifyState_Exists` | `5.113 ns` | `0.56` | `-` |
| `IncrementByAmount_EntrySwitch_Exists` | `5.732 ns` | `0.62` | `-` |
| | | | |
| `IncrementByAmount_Traditional_NotExists` | `12.013 ns` | `1.00` | `-` |
| `IncrementByAmount_EntryAndModify_NotExists` | `22.259 ns` | `1.85` | `80 B` |
| `IncrementByAmount_EntryAndModifyState_NotExists` | `8.690 ns` | `0.72` | `-` |
| `IncrementByAmount_EntrySwitch_NotExists` | `8.575 ns` | `0.71` | `-` |

#### `DictionaryContainsBenchmarks` (`Dictionary<int, int>`)

| Method | Mean | Ratio | Allocated |
|---|---:|---:|---:|
| `TryGetValue_Found` | `1.646 ns` | `1.00` | `-` |
| `Entry_IsOccupied_Found` | `1.655 ns` | `1.01` | `-` |
| | | | |
| `TryGetValue_NotFound` | `1.251 ns` | `1.00` | `-` |
| `Entry_IsOccupied_NotFound` | `1.236 ns` | `0.99` | `-` |

#### `TryGetEntryBenchmarks` (`Dictionary<string, int>`)

| Method | Mean | Ratio | Allocated |
|---|---:|---:|---:|
| `TryLookup_Traditional_Exists` | `4.200 ns` | `1.00` | `-` |
| `TryLookup_TryGetOccupied_Exists` | `4.244 ns` | `1.01` | `-` |
| `TryLookup_IsPattern_Exists` | `4.232 ns` | `1.01` | `-` |
| | | | |
| `TryLookup_Traditional_NotExists` | `3.325 ns` | `1.00` | `-` |
| `TryLookup_TryGetOccupied_NotExists` | `3.325 ns` | `1.00` | `-` |
| `TryLookup_IsPattern_NotExists` | `3.344 ns` | `1.01` | `-` |

#### `InitializeAbsentBenchmarks` (`Dictionary<string, int>`)

| Method | Mean | Ratio | Allocated |
|---|---:|---:|---:|
| `InitializeAbsent_Traditional_Exists` | `4.229 ns` | `1.00` | `-` |
| `InitializeAbsent_Entry_Exists` | `4.476 ns` | `1.06` | `-` |
| | | | |
| `InitializeAbsent_Traditional_NotExists` | `7.348 ns` | `1.00` | `-` |
| `InitializeAbsent_Entry_NotExists` | `8.525 ns` | `1.16` | `-` |

#### `RetrieveAndRemoveBenchmarks` (`Dictionary<string, int>`)

| Method | Mean | Ratio | Allocated |
|---|---:|---:|---:|
| `GetAndRemove_Traditional_Exists` | `12.283 ns` | `1.00` | `-` |
| `GetAndRemove_Entry_Exists` | `13.002 ns` | `1.06` | `-` |
| | | | |
| `GetAndRemove_Traditional_NotExists` | `3.411 ns` | `1.00` | `-` |
| `GetAndRemove_Entry_NotExists` | `4.317 ns` | `1.27` | `-` |

### Conditional Operations

#### `UpdateExistingBenchmarks` (`Dictionary<string, int>`)

| Method | Mean | Ratio | Allocated |
|---|---:|---:|---:|
| `Update_Traditional_Exists` | `8.444 ns` | `1.00` | `-` |
| `Update_Entry_Exists` | `6.005 ns` | `0.71` | `-` |
| `Update_EntryValueRef_Exists` | `4.369 ns` | `0.52` | `-` |
| | | | |
| `Update_Traditional_NotExists` | `3.192 ns` | `1.00` | `-` |
| `Update_Entry_NotExists` | `3.508 ns` | `1.10` | `-` |
| `Update_EntryValueRef_NotExists` | `3.271 ns` | `1.02` | `-` |

#### `UpsertBenchmarks` (`Dictionary<string, int>`)

| Method | Mean | Ratio | Allocated |
|---|---:|---:|---:|
| `Upsert_Traditional_Exists` | `4.508 ns` | `1.00` | `-` |
| `Upsert_Entry_Exists` | `4.701 ns` | `1.04` | `-` |
| | | | |
| `Upsert_Traditional_NotExists` | `7.114 ns` | `1.00` | `-` |
| `Upsert_Entry_NotExists` | `8.396 ns` | `1.18` | `-` |

### Factory Operations

#### `FactoryMethodBenchmarks` (`Dictionary<string, int>`)

| Method | Mean | Ratio | Allocated |
|---|---:|---:|---:|
| `FactoryMethod_Traditional_Exists` | `4.312 ns` | `1.00` | `-` |
| `FactoryMethod_Entry_Exists` | `4.875 ns` | `1.13` | `-` |
| | | | |
| `FactoryMethod_Traditional_NotExists` | `54.207 ns` | `1.00` | `-` |
| `FactoryMethod_Entry_NotExists` | `49.320 ns` | `0.91` | `-` |
| | | | |
| `KeyBasedFactory_Traditional_Exists` | `4.262 ns` | `1.00` | `-` |
| `KeyBasedFactory_Entry_Exists` | `4.970 ns` | `1.17` | `-` |
| | | | |
| `KeyBasedFactory_Traditional_NotExists` | `12.143 ns` | `1.00` | `-` |
| `KeyBasedFactory_Entry_NotExists` | `9.924 ns` | `0.82` | `-` |
| | | | |
| `StatefulFactory_Traditional_Exists` | `4.376 ns` | `1.00` | `-` |
| `StatefulFactory_EntryCapturing_Exists` | `20.420 ns` | `4.67` | `80 B` |
| `StatefulFactory_EntryState_Exists` | `4.778 ns` | `1.09` | `-` |
| | | | |
| `StatefulFactory_Traditional_NotExists` | `12.385 ns` | `1.00` | `-` |
| `StatefulFactory_EntryCapturing_NotExists` | `27.457 ns` | `2.22` | `80 B` |
| `StatefulFactory_EntryState_NotExists` | `10.198 ns` | `0.82` | `-` |

### Advanced Operations

#### `PatternMatchingBenchmarks` (`Dictionary<string, int>`)

| Method | Mean | Ratio | Allocated |
|---|---:|---:|---:|
| `PatternMatching_Traditional_Exists` | `4.838 ns` | `1.00` | `-` |
| `PatternMatching_EntryMatch_Exists` | `6.238 ns` | `1.29` | `-` |
| `PatternMatching_EntrySwitch_Exists` | `4.883 ns` | `1.01` | `-` |
| | | | |
| `PatternMatching_Traditional_NotExists` | `3.579 ns` | `1.00` | `-` |
| `PatternMatching_EntryMatch_NotExists` | `4.761 ns` | `1.33` | `-` |
| `PatternMatching_EntrySwitch_NotExists` | `3.733 ns` | `1.04` | `-` |
| | | | |
| `DifferentActions_Traditional_Exists` | `10.201 ns` | `1.00` | `-` |
| `DifferentActions_EntryMatch_Exists` | `7.371 ns` | `0.72` | `-` |
| `DifferentActions_EntrySwitch_Exists` | `5.598 ns` | `0.55` | `-` |
| `StatefulActions_EntryMatchCapturing_Exists` | `35.058 ns` | `3.44` | `136 B` |
| `StatefulActions_EntryMatchState_Exists` | `5.919 ns` | `0.58` | `-` |
| | | | |
| `DifferentActions_Traditional_NotExists` | `11.944 ns` | `1.00` | `-` |
| `DifferentActions_EntryMatch_NotExists` | `12.135 ns` | `1.02` | `-` |
| `DifferentActions_EntrySwitch_NotExists` | `8.892 ns` | `0.74` | `-` |
| `StatefulActions_EntryMatchCapturing_NotExists` | `36.118 ns` | `3.02` | `136 B` |
| `StatefulActions_EntryMatchState_NotExists` | `11.666 ns` | `0.98` | `-` |

#### `SpanLookupBenchmarks` (`Dictionary<string, int>` / `ReadOnlySpan<char>`)

| Method | Mean | Ratio | Allocated |
|---|---:|---:|---:|
| `SpanLookup_TraditionalToString_Exists` | `21.107 ns` | `1.00` | `53 B` |
| `SpanLookup_DirectEntry_Exists` | `11.527 ns` | `0.55` | `-` |
| `SpanLookup_AlternateLookupEntry_Exists` | `9.624 ns` | `0.46` | `-` |
| | | | |
| `SpanLookup_TraditionalToString_NotExists` | `25.131 ns` | `1.00` | `53 B` |
| `SpanLookup_DirectEntry_NotExists` | `25.056 ns` | `1.00` | `53 B` |
| `SpanLookup_AlternateLookupEntry_NotExists` | `22.203 ns` | `0.89` | `53 B` |
