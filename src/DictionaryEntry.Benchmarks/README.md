# DictionaryEntry Benchmarks

Full BenchmarkDotNet results for the current version of `DictionaryEntry` (`v0.3.0`), comparing idiomatic hand-written `Dictionary<TKey, TValue>` code (`TryGetValue` + indexer) against `DictionaryEntry`.

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

MaxRelativeError=0.01  Toolchain=InProcessNoEmitToolchain  InvocationCount=64000
OperationsPerInvoke=256  MaxIterationCount=15  MinIterationCount=8
MaxWarmupIterationCount=8  MinWarmupIterationCount=5  RunStrategy=Throughput
```

## Results

### Basic Operations

#### `GetOrAddBenchmarks` (`Dictionary<int, int>`)

| Method | Mean | Allocated |
|---|---:|---:|
| `GetOrAdd_Traditional_Exists` | `1.812 ns` | `-` |
| `GetOrAdd_Traditional_NotExists` | `5.172 ns` | `-` |
| `GetOrAdd_Entry_Exists` | `1.849 ns` | `-` |
| `GetOrAdd_Entry_NotExists` | `3.724 ns` | `-` |

#### `DefaultValueBenchmarks` (`Dictionary<string, int>` / `Dictionary<string, string?>`)

| Method | Mean | Allocated |
|---|---:|---:|
| `DefaultValue_Traditional_Exists` | `4.101 ns` | `-` |
| `DefaultValue_Traditional_NotExists` | `11.617 ns` | `-` |
| `DefaultValue_Entry_Exists` | `4.159 ns` | `-` |
| `DefaultValue_Entry_NotExists` | `8.558 ns` | `-` |
| `DefaultValue_TraditionalString_NotExists` | `11.796 ns` | `-` |
| `DefaultValue_EntryString_NotExists` | `7.192 ns` | `-` |

#### `IncrementCounterBenchmarks` (`Dictionary<string, int>`)

| Method | Mean | Allocated |
|---|---:|---:|
| `IncrementCounter_Traditional_Exists` | `8.937 ns` | `-` |
| `IncrementCounter_Traditional_NotExists` | `11.571 ns` | `-` |
| `IncrementCounter_EntryAndModify_Exists` | `4.568 ns` | `-` |
| `IncrementCounter_EntryAndModify_NotExists` | `8.631 ns` | `-` |
| `IncrementByAmount_Traditional_Exists` | `9.004 ns` | `-` |
| `IncrementByAmount_Traditional_NotExists` | `11.810 ns` | `-` |
| `IncrementByAmount_EntryAndModify_Exists` | `20.668 ns` | `80 B` |
| `IncrementByAmount_EntryAndModify_NotExists` | `19.532 ns` | `80 B` |
| `IncrementByAmount_EntryAndModifyState_Exists` | `4.755 ns` | `-` |
| `IncrementByAmount_EntryAndModifyState_NotExists` | `8.593 ns` | `-` |
| `IncrementByAmount_EntrySwitch_Exists` | `5.129 ns` | `-` |
| `IncrementByAmount_EntrySwitch_NotExists` | `8.621 ns` | `-` |

#### `DictionaryContainsBenchmarks` (`Dictionary<int, int>`)

| Method | Mean | Allocated |
|---|---:|---:|
| `TryGetValue_Found` | `1.615 ns` | `-` |
| `Entry_IsOccupied_Found` | `1.626 ns` | `-` |
| `TryGetValue_NotFound` | `1.201 ns` | `-` |
| `Entry_IsOccupied_NotFound` | `1.221 ns` | `-` |

#### `TryGetEntryBenchmarks` (`Dictionary<string, int>`)

| Method | Mean | Allocated |
|---|---:|---:|
| `TryLookup_Traditional_Exists` | `4.097 ns` | `-` |
| `TryLookup_Traditional_NotExists` | `3.260 ns` | `-` |
| `TryLookup_TryGetOccupied_Exists` | `4.166 ns` | `-` |
| `TryLookup_TryGetOccupied_NotExists` | `3.249 ns` | `-` |
| `TryLookup_IsPattern_Exists` | `4.163 ns` | `-` |
| `TryLookup_IsPattern_NotExists` | `3.260 ns` | `-` |

#### `InitializeAbsentBenchmarks` (`Dictionary<string, int>`)

| Method | Mean | Allocated |
|---|---:|---:|
| `InitializeAbsent_Traditional_Exists` | `4.101 ns` | `-` |
| `InitializeAbsent_Traditional_NotExists` | `7.239 ns` | `-` |
| `InitializeAbsent_Entry_Exists` | `4.122 ns` | `-` |
| `InitializeAbsent_Entry_NotExists` | `8.525 ns` | `-` |

#### `RetrieveAndRemoveBenchmarks` (`Dictionary<string, int>`)

| Method | Mean | Allocated |
|---|---:|---:|
| `GetAndRemove_Traditional_Exists` | `12.048 ns` | `-` |
| `GetAndRemove_Traditional_NotExists` | `3.330 ns` | `-` |
| `GetAndRemove_Entry_Exists` | `14.850 ns` | `-` |
| `GetAndRemove_Entry_NotExists` | `3.245 ns` | `-` |

### Conditional Operations

#### `UpdateExistingBenchmarks` (`Dictionary<string, int>`)

| Method | Mean | Allocated |
|---|---:|---:|
| `Update_Traditional_Exists` | `8.127 ns` | `-` |
| `Update_Traditional_NotExists` | `3.160 ns` | `-` |
| `Update_Entry_Exists` | `5.610 ns` | `-` |
| `Update_Entry_NotExists` | `3.479 ns` | `-` |
| `Update_EntryValueRef_Exists` | `4.143 ns` | `-` |
| `Update_EntryValueRef_NotExists` | `3.155 ns` | `-` |

#### `InsertOrUpdateBenchmarks` (`Dictionary<string, int>`)

| Method | Mean | Allocated |
|---|---:|---:|
| `InsertOrUpdate_Traditional_Exists` | `8.109 ns` | `-` |
| `InsertOrUpdate_Traditional_NotExists` | `11.747 ns` | `-` |
| `InsertOrUpdate_Entry_Exists` | `5.669 ns` | `-` |
| `InsertOrUpdate_Entry_NotExists` | `8.623 ns` | `-` |

#### `UpsertBenchmarks` (`Dictionary<string, int>`)

| Method | Mean | Allocated |
|---|---:|---:|
| `Upsert_Traditional_Exists` | `4.353 ns` | `-` |
| `Upsert_Traditional_NotExists` | `6.836 ns` | `-` |
| `Upsert_Entry_Exists` | `4.278 ns` | `-` |
| `Upsert_Entry_NotExists` | `8.598 ns` | `-` |

#### `ConditionalModificationBenchmarks` (`Dictionary<string, int>`)

| Method | Mean | Allocated |
|---|---:|---:|
| `ConditionalModify_Traditional_Exists` | `8.007 ns` | `-` |
| `ConditionalModify_Traditional_NotExists` | `11.690 ns` | `-` |
| `ConditionalModify_Entry_Exists` | `8.109 ns` | `-` |
| `ConditionalModify_Entry_NotExists` | `10.163 ns` | `-` |

#### `ConditionalGetOrComputeBenchmarks` (`Dictionary<string, int>`)

| Method | Mean | Allocated |
|---|---:|---:|
| `GetOrCompute_Traditional_Exists` | `4.104 ns` | `-` |
| `GetOrCompute_Traditional_NotExists` | `33.131 ns` | `-` |
| `GetOrCompute_Entry_Exists` | `4.375 ns` | `-` |
| `GetOrCompute_Entry_NotExists` | `30.141 ns` | `-` |
| `GetOrCompute_Extra_Traditional_Exists` | `4.195 ns` | `-` |
| `GetOrCompute_Extra_Traditional_NotExists` | `11.808 ns` | `-` |
| `GetOrCompute_Extra_Entry_Exists` | `4.358 ns` | `-` |
| `GetOrCompute_Extra_Entry_NotExists` | `10.218 ns` | `-` |

### Factory Operations

#### `FactoryMethodBenchmarks` (`Dictionary<string, int>`)

| Method | Mean | Allocated |
|---|---:|---:|
| `FactoryMethod_Traditional_Exists` | `4.154 ns` | `-` |
| `FactoryMethod_Traditional_NotExists` | `52.016 ns` | `-` |
| `FactoryMethod_Entry_Exists` | `4.354 ns` | `-` |
| `FactoryMethod_Entry_NotExists` | `48.640 ns` | `-` |
| `KeyBasedFactory_Traditional_Exists` | `4.120 ns` | `-` |
| `KeyBasedFactory_Traditional_NotExists` | `11.832 ns` | `-` |
| `KeyBasedFactory_Entry_Exists` | `4.427 ns` | `-` |
| `KeyBasedFactory_Entry_NotExists` | `10.312 ns` | `-` |
| `StatefulFactory_Traditional_Exists` | `4.138 ns` | `-` |
| `StatefulFactory_Traditional_NotExists` | `11.952 ns` | `-` |
| `StatefulFactory_EntryCapturing_Exists` | `17.345 ns` | `80 B` |
| `StatefulFactory_EntryCapturing_NotExists` | `26.885 ns` | `80 B` |
| `StatefulFactory_EntryState_Exists` | `4.343 ns` | `-` |
| `StatefulFactory_EntryState_NotExists` | `9.664 ns` | `-` |

### Advanced Operations

#### `PatternMatchingBenchmarks` (`Dictionary<string, int>`)

| Method | Mean | Allocated |
|---|---:|---:|
| `PatternMatching_Traditional_Exists` | `4.563 ns` | `-` |
| `PatternMatching_Traditional_NotExists` | `3.289 ns` | `-` |
| `PatternMatching_EntryMatch_Exists` | `5.773 ns` | `-` |
| `PatternMatching_EntryMatch_NotExists` | `4.657 ns` | `-` |
| `PatternMatching_EntrySwitch_Exists` | `4.664 ns` | `-` |
| `PatternMatching_EntrySwitch_NotExists` | `3.521 ns` | `-` |
| `DifferentActions_Traditional_Exists` | `8.968 ns` | `-` |
| `DifferentActions_Traditional_NotExists` | `11.725 ns` | `-` |
| `DifferentActions_EntryMatch_Exists` | `6.784 ns` | `-` |
| `DifferentActions_EntryMatch_NotExists` | `11.235 ns` | `-` |
| `DifferentActions_EntrySwitch_Exists` | `5.123 ns` | `-` |
| `DifferentActions_EntrySwitch_NotExists` | `8.598 ns` | `-` |
| `StatefulActions_EntryMatchCapturing_Exists` | `28.179 ns` | `136 B` |
| `StatefulActions_EntryMatchCapturing_NotExists` | `29.411 ns` | `136 B` |
| `StatefulActions_EntryMatchState_Exists` | `5.374 ns` | `-` |
| `StatefulActions_EntryMatchState_NotExists` | `10.251 ns` | `-` |
| `StatefulActions_EntrySwitch_Exists` | `5.166 ns` | `-` |
| `StatefulActions_EntrySwitch_NotExists` | `8.550 ns` | `-` |

#### `SpanLookupBenchmarks` (`Dictionary<string, int>` / `ReadOnlySpan<char>`)

| Method | Mean | Allocated |
|---|---:|---:|
| `SpanLookup_TraditionalToString_Exists` | `18.822 ns` | `53 B` |
| `SpanLookup_DirectEntry_Exists` | `11.391 ns` | `-` |
| `SpanLookup_AlternateLookupEntry_Exists` | `8.915 ns` | `-` |
| `SpanLookup_TraditionalToString_NotExists` | `22.141 ns` | `53 B` |
| `SpanLookup_DirectEntry_NotExists` | `22.140 ns` | `53 B` |
| `SpanLookup_AlternateLookupEntry_NotExists` | `20.386 ns` | `53 B` |

#### `BatchOperationsBenchmarks` (`Dictionary<string, int>`)

| Method | Mean | Allocated |
|---|---:|---:|
| `BatchOperations_Traditional_Exists` | `10.145 ns` | `-` |
| `BatchOperations_Traditional_NotExists` | `11.791 ns` | `-` |
| `BatchOperations_Entry_Exists` | `5.590 ns` | `-` |
| `BatchOperations_Entry_NotExists` | `16.883 ns` | `-` |
