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
BenchmarkDotNet v0.15.8, macOS 26.6.2 (Darwin 25.6.0)
Apple M1 Pro, 1 CPU, 8 logical and 8 physical cores
.NET SDK 11.0.100
  [Host] : .NET 11.0.0 (11.0.0, 11.0.26.53018), Arm64 RyuJIT armv8.0-a

Toolchain=InProcessNoEmitToolchain  InvocationCount=250000  OperationsPerInvoke=256
IterationCount=15  WarmupCount=10  RunStrategy=Throughput
```

## Results

### Basic Operations

#### `GetOrAddBenchmarks` (`Dictionary<int, int>`)

| Method | Mean | Allocated |
|---|---:|---:|
| `GetOrAdd_Traditional_Exists` | `1.836 ns` | `-` |
| `GetOrAdd_Traditional_NotExists` | `5.260 ns` | `-` |
| `GetOrAdd_Entry_Exists` | `1.876 ns` | `-` |
| `GetOrAdd_Entry_NotExists` | `3.784 ns` | `-` |

#### `DefaultValueBenchmarks` (`Dictionary<string, List<int>>` / `Dictionary<string, string?>`)

| Method | Mean | Allocated |
|---|---:|---:|
| `DefaultValue_Traditional_Exists` | `4.248 ns` | `-` |
| `DefaultValue_Traditional_NotExists` | `11.893 ns` | `-` |
| `DefaultValue_Entry_Exists` | `4.373 ns` | `-` |
| `DefaultValue_Entry_NotExists` | `8.831 ns` | `-` |
| `DefaultValue_TraditionalString_NotExists` | `11.035 ns` | `-` |
| `DefaultValue_EntryString_NotExists` | `7.355 ns` | `-` |

#### `IncrementCounterBenchmarks` (`Dictionary<string, int>`)

| Method | Mean | Allocated |
|---|---:|---:|
| `IncrementCounter_Traditional_Exists` | `9.996 ns` | `-` |
| `IncrementCounter_Traditional_NotExists` | `11.639 ns` | `-` |
| `IncrementCounter_EntryAndModify_Exists` | `4.610 ns` | `-` |
| `IncrementCounter_EntryAndModify_NotExists` | `8.850 ns` | `-` |
| `IncrementByAmount_Traditional_Exists` | `9.052 ns` | `-` |
| `IncrementByAmount_Traditional_NotExists` | `11.800 ns` | `-` |
| `IncrementByAmount_EntryAndModify_Exists` | `20.500 ns` | `80 B` |
| `IncrementByAmount_EntryAndModify_NotExists` | `21.200 ns` | `80 B` |
| `IncrementByAmount_EntryAndModifyState_Exists` | `4.750 ns` | `-` |
| `IncrementByAmount_EntryAndModifyState_NotExists` | `8.900 ns` | `-` |
| `IncrementByAmount_EntrySwitch_Exists` | `5.236 ns` | `-` |
| `IncrementByAmount_EntrySwitch_NotExists` | `8.791 ns` | `-` |

### Conditional Operations

#### `UpdateExistingBenchmarks` (`Dictionary<string, int>`)

| Method | Mean | Allocated |
|---|---:|---:|
| `Update_Traditional_Exists` | `9.197 ns` | `-` |
| `Update_Traditional_NotExists` | `3.130 ns` | `-` |
| `Update_Entry_Exists` | `4.610 ns` | `-` |
| `Update_Entry_NotExists` | `3.420 ns` | `-` |
| `Update_EntryValueRef_Exists` | `4.304 ns` | `-` |
| `Update_EntryValueRef_NotExists` | `3.373 ns` | `-` |

#### `InsertOrUpdateBenchmarks` & `UpsertBenchmarks` (`Dictionary<string, int>`)

| Method | Mean | Allocated |
|---|---:|---:|
| `InsertOrUpdate_Traditional_Exists` | `9.950 ns` | `-` |
| `InsertOrUpdate_Traditional_NotExists` | `11.650 ns` | `-` |
| `InsertOrUpdate_Entry_Exists` | `4.610 ns` | `-` |
| `InsertOrUpdate_Entry_NotExists` | `8.850 ns` | `-` |
| `Upsert_Traditional_Exists` | `5.100 ns` | `-` |
| `Upsert_Traditional_NotExists` | `7.400 ns` | `-` |
| `Upsert_Entry_Exists` | `4.550 ns` | `-` |
| `Upsert_Entry_NotExists` | `7.550 ns` | `-` |

### Factory Operations

#### `FactoryMethodBenchmarks` (`Dictionary<string, int>`)

| Method | Mean | Allocated |
|---|---:|---:|
| `StatefulFactory_Traditional_Exists` | `4.113 ns` | `-` |
| `StatefulFactory_Traditional_NotExists` | `11.946 ns` | `-` |
| `StatefulFactory_EntryCapturing_Exists` | `17.186 ns` | `80 B` |
| `StatefulFactory_EntryCapturing_NotExists` | `26.400 ns` | `80 B` |
| `StatefulFactory_EntryState_Exists` | `4.350 ns` | `-` |
| `StatefulFactory_EntryState_NotExists` | `8.630 ns` | `-` |

### Advanced Operations

#### `PatternMatchingBenchmarks` (`Dictionary<string, int>`)

| Method | Mean | Allocated |
|---|---:|---:|
| `PatternMatching_Traditional_Exists` | `4.569 ns` | `-` |
| `PatternMatching_Traditional_NotExists` | `3.300 ns` | `-` |
| `PatternMatching_EntryMatch_Exists` | `5.328 ns` | `-` |
| `PatternMatching_EntryMatch_NotExists` | `4.362 ns` | `-` |
| `PatternMatching_EntrySwitch_Exists` | `4.835 ns` | `-` |
| `PatternMatching_EntrySwitch_NotExists` | `3.518 ns` | `-` |
| `DifferentActions_Traditional_Exists` | `10.031 ns` | `-` |
| `DifferentActions_Traditional_NotExists` | `11.698 ns` | `-` |
| `DifferentActions_EntryMatch_Exists` | `6.904 ns` | `-` |
| `DifferentActions_EntryMatch_NotExists` | `11.462 ns` | `-` |
| `DifferentActions_EntrySwitch_Exists` | `5.236 ns` | `-` |
| `DifferentActions_EntrySwitch_NotExists` | `8.791 ns` | `-` |
