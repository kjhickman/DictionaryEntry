# DictionaryEntry Benchmarks

Full BenchmarkDotNet results for source revision `af9e3b9`, measured on November 17, 2026, comparing idiomatic hand-written `Dictionary<TKey, TValue>` code against `DictionaryEntry`.

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
BenchmarkDotNet v0.15.8, Linux NixOS 26.11 (Zokor)
Intel Core i9-14900KF 3.19GHz, 1 CPU, 32 logical and 16 physical cores
.NET SDK 11.0.100
  [Host] : .NET 11.0.0 (11.0.0, 11.0.26.53018), X64 RyuJIT x86-64-v3
  GC     : Concurrent Server

MaxRelativeError=0.01  Toolchain=InProcessNoEmitToolchain  InvocationCount=128000 (384000 for int)
OperationsPerInvoke=256  MaxIterationCount=15  MinIterationCount=8
MaxWarmupIterationCount=8  MinWarmupIterationCount=5  RunStrategy=Throughput
```

All 86 benchmarks across 12 classes completed. Process-priority elevation was denied; several fast cases triggered minimum-iteration-time warnings (67–100 ms versus the recommended 100 ms), and `Upsert_Entry_Exists` had a bimodal distribution. Treat small timing differences cautiously.

## Results

Means are per dictionary operation, measured in batches of 256. Insertion-miss batches include clearing a preallocated dictionary; hit-removal batches include repopulating it. Dictionary resizing is not measured.

`Ratio` is BenchmarkDotNet's mean of the ratio distribution relative to the hand-written baseline within the same category (lower is better). `-` in the Allocated column means no managed allocations. Capturing-lambda variants intentionally demonstrate closure allocations.

### Basic Operations

#### `GetOrAddBenchmarks` (`Dictionary<int, int>`)

| Method | Mean | Ratio | Allocated |
|---|---:|---:|---:|
| `GetOrAdd_Traditional_Exists` | `1.244 ns` | `1.00` | `-` |
| `GetOrAdd_Entry_Exists` | `1.360 ns` | `1.09` | `-` |
| | | | |
| `GetOrAdd_Traditional_NotExists` | `3.473 ns` | `1.00` | `-` |
| `GetOrAdd_Entry_NotExists` | `2.700 ns` | `0.78` | `-` |

#### `DefaultValueBenchmarks` (`Dictionary<string, int>` / `Dictionary<string, string?>`)

| Method | Mean | Ratio | Allocated |
|---|---:|---:|---:|
| `DefaultValue_Traditional_Exists` | `2.968 ns` | `1.00` | `-` |
| `DefaultValue_Entry_Exists` | `3.254 ns` | `1.10` | `-` |
| | | | |
| `DefaultValue_Traditional_NotExists` | `7.754 ns` | `1.00` | `-` |
| `DefaultValue_Entry_NotExists` | `5.488 ns` | `0.71` | `-` |
| | | | |
| `DefaultValue_TraditionalString_NotExists` | `7.203 ns` | `1.00` | `-` |
| `DefaultValue_EntryString_NotExists` | `4.875 ns` | `0.68` | `-` |

#### `IncrementCounterBenchmarks` (`Dictionary<string, int>`)

| Method | Mean | Ratio | Allocated |
|---|---:|---:|---:|
| `IncrementCounter_Traditional_Exists` | `5.904 ns` | `1.00` | `-` |
| `IncrementCounter_EntryAndModify_Exists` | `3.674 ns` | `0.62` | `-` |
| | | | |
| `IncrementCounter_Traditional_NotExists` | `7.694 ns` | `1.00` | `-` |
| `IncrementCounter_EntryAndModify_NotExists` | `5.834 ns` | `0.76` | `-` |
| | | | |
| `IncrementByAmount_Traditional_Exists` | `5.926 ns` | `1.00` | `-` |
| `IncrementByAmount_EntryAndModify_Exists` | `17.213 ns` | `2.91` | `80 B` |
| `IncrementByAmount_EntryAndModifyState_Exists` | `3.615 ns` | `0.61` | `-` |
| `IncrementByAmount_EntrySwitch_Exists` | `3.736 ns` | `0.63` | `-` |
| | | | |
| `IncrementByAmount_Traditional_NotExists` | `7.868 ns` | `1.00` | `-` |
| `IncrementByAmount_EntryAndModify_NotExists` | `20.733 ns` | `2.64` | `80 B` |
| `IncrementByAmount_EntryAndModifyState_NotExists` | `5.798 ns` | `0.74` | `-` |
| `IncrementByAmount_EntrySwitch_NotExists` | `6.001 ns` | `0.76` | `-` |

#### `DictionaryContainsBenchmarks` (`Dictionary<int, int>`)

| Method | Mean | Ratio | Allocated |
|---|---:|---:|---:|
| `TryGetValue_Found` | `1.1514 ns` | `1.00` | `-` |
| `Entry_IsOccupied_Found` | `1.1661 ns` | `1.01` | `-` |
| | | | |
| `TryGetValue_NotFound` | `0.8599 ns` | `1.00` | `-` |
| `Entry_IsOccupied_NotFound` | `0.8421 ns` | `0.98` | `-` |

#### `TryGetEntryBenchmarks` (`Dictionary<string, int>`)

| Method | Mean | Ratio | Allocated |
|---|---:|---:|---:|
| `TryLookup_Traditional_Exists` | `2.950 ns` | `1.00` | `-` |
| `TryLookup_TryGetOccupied_Exists` | `2.957 ns` | `1.00` | `-` |
| `TryLookup_IsPattern_Exists` | `2.981 ns` | `1.01` | `-` |
| | | | |
| `TryLookup_Traditional_NotExists` | `2.180 ns` | `1.00` | `-` |
| `TryLookup_TryGetOccupied_NotExists` | `2.311 ns` | `1.06` | `-` |
| `TryLookup_IsPattern_NotExists` | `2.295 ns` | `1.05` | `-` |

#### `InitializeAbsentBenchmarks` (`Dictionary<string, int>`)

| Method | Mean | Ratio | Allocated |
|---|---:|---:|---:|
| `InitializeAbsent_Traditional_Exists` | `2.873 ns` | `1.00` | `-` |
| `InitializeAbsent_Entry_Exists` | `3.054 ns` | `1.06` | `-` |
| | | | |
| `InitializeAbsent_Traditional_NotExists` | `5.168 ns` | `1.00` | `-` |
| `InitializeAbsent_Entry_NotExists` | `5.565 ns` | `1.08` | `-` |

#### `RetrieveAndRemoveBenchmarks` (`Dictionary<string, int>`)

| Method | Mean | Ratio | Allocated |
|---|---:|---:|---:|
| `GetAndRemove_Traditional_Exists` | `8.008 ns` | `1.00` | `-` |
| `GetAndRemove_Entry_Exists` | `8.747 ns` | `1.09` | `-` |
| | | | |
| `GetAndRemove_Traditional_NotExists` | `2.317 ns` | `1.00` | `-` |
| `GetAndRemove_Entry_NotExists` | `2.191 ns` | `0.95` | `-` |

### Conditional Operations

#### `UpdateExistingBenchmarks` (`Dictionary<string, int>`)

| Method | Mean | Ratio | Allocated |
|---|---:|---:|---:|
| `Update_Traditional_Exists` | `5.899 ns` | `1.00` | `-` |
| `Update_Entry_Exists` | `3.458 ns` | `0.59` | `-` |
| `Update_EntryValueRef_Exists` | `2.966 ns` | `0.50` | `-` |
| | | | |
| `Update_Traditional_NotExists` | `2.266 ns` | `1.00` | `-` |
| `Update_Entry_NotExists` | `2.263 ns` | `1.00` | `-` |
| `Update_EntryValueRef_NotExists` | `2.100 ns` | `0.93` | `-` |

#### `UpsertBenchmarks` (`Dictionary<string, int>`)

| Method | Mean | Ratio | Allocated |
|---|---:|---:|---:|
| `Upsert_Traditional_Exists` | `2.850 ns` | `1.00` | `-` |
| `Upsert_Entry_Exists` | `3.222 ns` | `1.13` | `-` |
| | | | |
| `Upsert_Traditional_NotExists` | `4.640 ns` | `1.00` | `-` |
| `Upsert_Entry_NotExists` | `5.493 ns` | `1.18` | `-` |

### Factory Operations

#### `FactoryMethodBenchmarks` (`Dictionary<string, int>`)

| Method | Mean | Ratio | Allocated |
|---|---:|---:|---:|
| `FactoryMethod_Traditional_Exists` | `2.979 ns` | `1.00` | `-` |
| `FactoryMethod_Entry_Exists` | `3.579 ns` | `1.20` | `-` |
| | | | |
| `FactoryMethod_Traditional_NotExists` | `42.806 ns` | `1.00` | `-` |
| `FactoryMethod_Entry_NotExists` | `40.453 ns` | `0.95` | `-` |
| | | | |
| `KeyBasedFactory_Traditional_Exists` | `3.053 ns` | `1.00` | `-` |
| `KeyBasedFactory_Entry_Exists` | `3.563 ns` | `1.17` | `-` |
| | | | |
| `KeyBasedFactory_Traditional_NotExists` | `7.755 ns` | `1.00` | `-` |
| `KeyBasedFactory_Entry_NotExists` | `7.164 ns` | `0.92` | `-` |
| | | | |
| `StatefulFactory_Traditional_Exists` | `3.102 ns` | `1.00` | `-` |
| `StatefulFactory_EntryCapturing_Exists` | `16.729 ns` | `5.40` | `80 B` |
| `StatefulFactory_EntryState_Exists` | `3.388 ns` | `1.09` | `-` |
| | | | |
| `StatefulFactory_Traditional_NotExists` | `8.095 ns` | `1.00` | `-` |
| `StatefulFactory_EntryCapturing_NotExists` | `21.652 ns` | `2.68` | `80 B` |
| `StatefulFactory_EntryState_NotExists` | `6.664 ns` | `0.82` | `-` |

### Advanced Operations

#### `PatternMatchingBenchmarks` (`Dictionary<string, int>`)

| Method | Mean | Ratio | Allocated |
|---|---:|---:|---:|
| `PatternMatching_Traditional_Exists` | `3.063 ns` | `1.00` | `-` |
| `PatternMatching_EntryMatch_Exists` | `3.799 ns` | `1.24` | `-` |
| `PatternMatching_EntrySwitch_Exists` | `3.045 ns` | `0.99` | `-` |
| | | | |
| `PatternMatching_Traditional_NotExists` | `2.201 ns` | `1.00` | `-` |
| `PatternMatching_EntryMatch_NotExists` | `3.227 ns` | `1.47` | `-` |
| `PatternMatching_EntrySwitch_NotExists` | `2.224 ns` | `1.01` | `-` |
| | | | |
| `DifferentActions_Traditional_Exists` | `6.654 ns` | `1.00` | `-` |
| `DifferentActions_EntryMatch_Exists` | `4.617 ns` | `0.69` | `-` |
| `DifferentActions_EntrySwitch_Exists` | `3.558 ns` | `0.53` | `-` |
| `StatefulActions_EntryMatchCapturing_Exists` | `28.394 ns` | `4.27` | `136 B` |
| `StatefulActions_EntryMatchState_Exists` | `3.849 ns` | `0.58` | `-` |
| | | | |
| `DifferentActions_Traditional_NotExists` | `7.556 ns` | `1.00` | `-` |
| `DifferentActions_EntryMatch_NotExists` | `7.340 ns` | `0.97` | `-` |
| `DifferentActions_EntrySwitch_NotExists` | `6.217 ns` | `0.82` | `-` |
| `StatefulActions_EntryMatchCapturing_NotExists` | `31.064 ns` | `4.11` | `136 B` |
| `StatefulActions_EntryMatchState_NotExists` | `6.903 ns` | `0.91` | `-` |

#### `SpanLookupBenchmarks` (`Dictionary<string, int>` / `ReadOnlySpan<char>`)

| Method | Mean | Ratio | Allocated |
|---|---:|---:|---:|
| `SpanLookup_TraditionalToString_Exists` | `19.914 ns` | `1.00` | `53 B` |
| `SpanLookup_DirectEntry_Exists` | `6.567 ns` | `0.33` | `-` |
| `SpanLookup_AlternateLookupEntry_Exists` | `6.547 ns` | `0.33` | `-` |
| | | | |
| `SpanLookup_TraditionalToString_NotExists` | `21.392 ns` | `1.00` | `53 B` |
| `SpanLookup_DirectEntry_NotExists` | `21.363 ns` | `1.00` | `53 B` |
| `SpanLookup_AlternateLookupEntry_NotExists` | `21.889 ns` | `1.02` | `53 B` |
