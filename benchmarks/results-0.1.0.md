```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26100.3476)
AMD Ryzen 7 7800X3D, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.100-preview.1.25120.13
  [Host]     : .NET 9.0.3 (9.0.325.11113), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-LEFTJB : .NET 9.0.3 (9.0.325.11113), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

InvocationCount=10000000  

```
| Type                              | Method                                   | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |----------------------------------------- |----------:|----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| BatchOperationsBenchmarks         | BatchOperations_Traditional_Exists       | 13.832 ns | 0.2318 ns | 0.3609 ns | 13.687 ns |  1.00 |    0.04 |      - |         - |          NA |
| BatchOperationsBenchmarks         | BatchOperations_Entry_Exists             | 14.625 ns | 0.0285 ns | 0.0238 ns | 14.628 ns |  1.06 |    0.03 |      - |         - |          NA |
| BatchOperationsBenchmarks         | BatchOperations_Traditional_NotExists    | 12.512 ns | 0.0486 ns | 0.0406 ns | 12.513 ns |  0.91 |    0.02 |      - |         - |          NA |
| BatchOperationsBenchmarks         | BatchOperations_Entry_NotExists          | 13.772 ns | 0.0719 ns | 0.0561 ns | 13.750 ns |  1.00 |    0.03 |      - |         - |          NA |
| ConditionalGetOrCompute           | GetOrCompute_Traditional_Exists          |  6.238 ns | 0.0216 ns | 0.0180 ns |  6.232 ns |  0.45 |    0.01 |      - |         - |          NA |
| ConditionalGetOrCompute           | GetOrCompute_Traditional_NotExists       |  5.798 ns | 0.0441 ns | 0.0344 ns |  5.788 ns |  0.42 |    0.01 |      - |         - |          NA |
| ConditionalGetOrCompute           | GetOrCompute_Entry_Exists                |  9.238 ns | 0.0724 ns | 0.0677 ns |  9.240 ns |  0.67 |    0.02 | 0.0012 |      64 B |          NA |
| ConditionalGetOrCompute           | GetOrCompute_Entry_NotExists             |  8.941 ns | 0.1774 ns | 0.3012 ns |  8.799 ns |  0.65 |    0.03 | 0.0012 |      64 B |          NA |
| ConditionalGetOrCompute           | GetOrCompute_Extra_Traditional_Exists    |  6.188 ns | 0.0357 ns | 0.0316 ns |  6.198 ns |  0.45 |    0.01 |      - |         - |          NA |
| ConditionalGetOrCompute           | GetOrCompute_Extra_Traditional_NotExists |  5.841 ns | 0.1117 ns | 0.1287 ns |  5.793 ns |  0.42 |    0.01 |      - |         - |          NA |
| ConditionalGetOrCompute           | GetOrCompute_Extra_Entry_Exists          | 12.525 ns | 0.1359 ns | 0.1205 ns | 12.478 ns |  0.91 |    0.02 | 0.0019 |      96 B |          NA |
| ConditionalGetOrCompute           | GetOrCompute_Extra_Entry_NotExists       | 11.819 ns | 0.0710 ns | 0.0593 ns | 11.820 ns |  0.85 |    0.02 | 0.0019 |      96 B |          NA |
| ConditionalModificationBenchmarks | ConditionalModify_Traditional_Exists     | 13.212 ns | 0.0310 ns | 0.0275 ns | 13.209 ns |  0.96 |    0.02 |      - |         - |          NA |
| ConditionalModificationBenchmarks | ConditionalModify_Traditional_NotExists  | 12.334 ns | 0.1437 ns | 0.1122 ns | 12.305 ns |  0.89 |    0.02 |      - |         - |          NA |
| ConditionalModificationBenchmarks | ConditionalModify_Entry_Exists           | 15.428 ns | 0.0645 ns | 0.0634 ns | 15.432 ns |  1.12 |    0.03 |      - |         - |          NA |
| ConditionalModificationBenchmarks | ConditionalModify_Entry_NotExists        | 14.452 ns | 0.0458 ns | 0.0358 ns | 14.452 ns |  1.05 |    0.03 |      - |         - |          NA |
| DefaultValueBenchmarks            | DefaultValue_Traditional_Exists          |  6.229 ns | 0.0248 ns | 0.0207 ns |  6.230 ns |  0.45 |    0.01 |      - |         - |          NA |
| DefaultValueBenchmarks            | DefaultValue_Traditional_NotExists       |  5.738 ns | 0.0328 ns | 0.0290 ns |  5.740 ns |  0.42 |    0.01 |      - |         - |          NA |
| DefaultValueBenchmarks            | DefaultValue_Entry_Exists                |  6.179 ns | 0.0233 ns | 0.0182 ns |  6.180 ns |  0.45 |    0.01 |      - |         - |          NA |
| DefaultValueBenchmarks            | DefaultValue_Entry_NotExists             |  5.815 ns | 0.0516 ns | 0.0483 ns |  5.803 ns |  0.42 |    0.01 |      - |         - |          NA |
| DefaultValueBenchmarks            | DefaultValue_TraditionalString_NotExists |  5.662 ns | 0.1532 ns | 0.2197 ns |  5.593 ns |  0.41 |    0.02 |      - |         - |          NA |
| DefaultValueBenchmarks            | DefaultValue_EntryString_NotExists       |  5.597 ns | 0.1514 ns | 0.2123 ns |  5.476 ns |  0.40 |    0.02 |      - |         - |          NA |
| FactoryMethodBenchmarks           | FactoryMethod_Traditional_Exists         |  6.232 ns | 0.0924 ns | 0.0722 ns |  6.214 ns |  0.45 |    0.01 |      - |         - |          NA |
| FactoryMethodBenchmarks           | FactoryMethod_Traditional_NotExists      |  5.760 ns | 0.0381 ns | 0.0337 ns |  5.749 ns |  0.42 |    0.01 |      - |         - |          NA |
| FactoryMethodBenchmarks           | FactoryMethod_Entry_Exists               | 10.601 ns | 0.0514 ns | 0.0401 ns | 10.591 ns |  0.77 |    0.02 | 0.0012 |      64 B |          NA |
| FactoryMethodBenchmarks           | FactoryMethod_Entry_NotExists            |  9.040 ns | 0.1175 ns | 0.0918 ns |  9.055 ns |  0.65 |    0.02 | 0.0012 |      64 B |          NA |
| FactoryMethodBenchmarks           | KeyBasedFactory_Traditional_Exists       |  6.316 ns | 0.0646 ns | 0.0573 ns |  6.304 ns |  0.46 |    0.01 |      - |         - |          NA |
| FactoryMethodBenchmarks           | KeyBasedFactory_Traditional_NotExists    |  5.717 ns | 0.0337 ns | 0.0299 ns |  5.710 ns |  0.41 |    0.01 |      - |         - |          NA |
| FactoryMethodBenchmarks           | KeyBasedFactory_Entry_Exists             |  9.405 ns | 0.1250 ns | 0.1043 ns |  9.385 ns |  0.68 |    0.02 | 0.0012 |      64 B |          NA |
| FactoryMethodBenchmarks           | KeyBasedFactory_Entry_NotExists          |  8.956 ns | 0.1004 ns | 0.1116 ns |  8.944 ns |  0.65 |    0.02 | 0.0012 |      64 B |          NA |
| GetAndRemoveBenchmarks            | GetAndRemove_Traditional_Exists          |  4.929 ns | 0.1191 ns | 0.1990 ns |  4.827 ns |  0.36 |    0.02 |      - |         - |          NA |
| GetAndRemoveBenchmarks            | GetAndRemove_Traditional_NotExists       |  4.396 ns | 0.0469 ns | 0.0392 ns |  4.394 ns |  0.32 |    0.01 |      - |         - |          NA |
| GetAndRemoveBenchmarks            | GetAndRemove_Entry_Exists                |  5.244 ns | 0.0311 ns | 0.0276 ns |  5.241 ns |  0.38 |    0.01 |      - |         - |          NA |
| GetAndRemoveBenchmarks            | GetAndRemove_Entry_NotExists             |  4.869 ns | 0.1014 ns | 0.0899 ns |  4.854 ns |  0.35 |    0.01 |      - |         - |          NA |
| GetOrAddBenchmarks                | GetOrAdd_Traditional_Exists              |  2.514 ns | 0.0242 ns | 0.0189 ns |  2.507 ns |  0.18 |    0.00 |      - |         - |          NA |
| GetOrAddBenchmarks                | GetOrAdd_Traditional_NotExists           |  2.866 ns | 0.0334 ns | 0.0260 ns |  2.864 ns |  0.21 |    0.01 |      - |         - |          NA |
| GetOrAddBenchmarks                | GetOrAdd_Entry_Exists                    |  2.793 ns | 0.0222 ns | 0.0173 ns |  2.791 ns |  0.20 |    0.01 |      - |         - |          NA |
| GetOrAddBenchmarks                | GetOrAdd_Entry_NotExists                 |  2.552 ns | 0.0337 ns | 0.0281 ns |  2.543 ns |  0.18 |    0.00 |      - |         - |          NA |
| IncrementCounterBenchmarks        | IncrementCounter_Traditional_Exists      | 13.083 ns | 0.0494 ns | 0.0549 ns | 13.063 ns |  0.95 |    0.02 |      - |         - |          NA |
| IncrementCounterBenchmarks        | IncrementCounter_Traditional_NotExists   | 12.340 ns | 0.1864 ns | 0.2551 ns | 12.244 ns |  0.89 |    0.03 |      - |         - |          NA |
| IncrementCounterBenchmarks        | IncrementCounter_Entry_Exists            | 15.437 ns | 0.0683 ns | 0.0571 ns | 15.420 ns |  1.12 |    0.03 |      - |         - |          NA |
| IncrementCounterBenchmarks        | IncrementCounter_Entry_NotExists         | 15.053 ns | 0.0508 ns | 0.0424 ns | 15.039 ns |  1.09 |    0.03 |      - |         - |          NA |
| IncrementCounterBenchmarks        | IncrementByAmount_Traditional_Exists     | 13.169 ns | 0.0606 ns | 0.0506 ns | 13.159 ns |  0.95 |    0.02 |      - |         - |          NA |
| IncrementCounterBenchmarks        | IncrementByAmount_Traditional_NotExists  | 12.519 ns | 0.2666 ns | 0.3823 ns | 12.295 ns |  0.91 |    0.04 |      - |         - |          NA |
| IncrementCounterBenchmarks        | IncrementByAmount_Entry_Exists           | 17.795 ns | 0.0663 ns | 0.0620 ns | 17.796 ns |  1.29 |    0.03 | 0.0017 |      88 B |          NA |
| IncrementCounterBenchmarks        | IncrementByAmount_Entry_NotExists        | 17.074 ns | 0.2999 ns | 0.2805 ns | 16.958 ns |  1.24 |    0.04 | 0.0017 |      88 B |          NA |
| InitializeAbsentBenchmarks        | InitializeAbsent_Traditional_Exists      |  5.941 ns | 0.0353 ns | 0.0295 ns |  5.935 ns |  0.43 |    0.01 |      - |         - |          NA |
| InitializeAbsentBenchmarks        | InitializeAbsent_Traditional_NotExists   |  5.567 ns | 0.0235 ns | 0.0208 ns |  5.566 ns |  0.40 |    0.01 |      - |         - |          NA |
| InitializeAbsentBenchmarks        | InitializeAbsent_Entry_Exists            |  6.490 ns | 0.0423 ns | 0.0330 ns |  6.497 ns |  0.47 |    0.01 |      - |         - |          NA |
| InitializeAbsentBenchmarks        | InitializeAbsent_Entry_NotExists         |  6.003 ns | 0.0313 ns | 0.0262 ns |  6.007 ns |  0.43 |    0.01 |      - |         - |          NA |
| UpdateOrInsertBenchmarks          | InsertOrUpdate_Traditional_Exists        | 13.362 ns | 0.1269 ns | 0.1060 ns | 13.338 ns |  0.97 |    0.03 |      - |         - |          NA |
| UpdateOrInsertBenchmarks          | InsertOrUpdate_Traditional_NotExists     | 12.344 ns | 0.1969 ns | 0.2022 ns | 12.270 ns |  0.89 |    0.03 |      - |         - |          NA |
| UpdateOrInsertBenchmarks          | InsertOrUpdate_Entry_Exists              | 15.907 ns | 0.0624 ns | 0.0521 ns | 15.901 ns |  1.15 |    0.03 |      - |         - |          NA |
| UpdateOrInsertBenchmarks          | InsertOrUpdate_Entry_NotExists           | 14.853 ns | 0.3167 ns | 0.4837 ns | 14.574 ns |  1.07 |    0.04 |      - |         - |          NA |
| PatternMatchingBenchmarks         | PatternMatching_Traditional_Exists       |  5.804 ns | 0.0558 ns | 0.0466 ns |  5.787 ns |  0.42 |    0.01 |      - |         - |          NA |
| PatternMatchingBenchmarks         | PatternMatching_Traditional_NotExists    |  3.833 ns | 0.0664 ns | 0.0589 ns |  3.824 ns |  0.28 |    0.01 |      - |         - |          NA |
| PatternMatchingBenchmarks         | PatternMatching_Entry_Exists             |  7.373 ns | 0.0194 ns | 0.0162 ns |  7.372 ns |  0.53 |    0.01 |      - |         - |          NA |
| PatternMatchingBenchmarks         | PatternMatching_Entry_NotExists          |  5.357 ns | 0.1077 ns | 0.0955 ns |  5.331 ns |  0.39 |    0.01 |      - |         - |          NA |
| PatternMatchingBenchmarks         | DifferentActions_Traditional_Exists      | 13.064 ns | 0.0439 ns | 0.0366 ns | 13.066 ns |  0.95 |    0.02 |      - |         - |          NA |
| PatternMatchingBenchmarks         | DifferentActions_Traditional_NotExists   | 12.254 ns | 0.0474 ns | 0.0396 ns | 12.249 ns |  0.89 |    0.02 |      - |         - |          NA |
| PatternMatchingBenchmarks         | DifferentActions_Entry_Exists            | 14.366 ns | 0.0830 ns | 0.0648 ns | 14.360 ns |  1.04 |    0.03 |      - |         - |          NA |
| PatternMatchingBenchmarks         | DifferentActions_Entry_NotExists         | 13.666 ns | 0.1131 ns | 0.1210 ns | 13.650 ns |  0.99 |    0.03 |      - |         - |          NA |
| UpdateExistingBenchmarks          | Update_Traditional_Exists                | 13.186 ns | 0.0464 ns | 0.0363 ns | 13.179 ns |  0.95 |    0.02 |      - |         - |          NA |
| UpdateExistingBenchmarks          | Update_Traditional_NotExists             |  3.988 ns | 0.0566 ns | 0.0442 ns |  3.969 ns |  0.29 |    0.01 |      - |         - |          NA |
| UpdateExistingBenchmarks          | Update_Entry_Exists                      | 13.989 ns | 0.0278 ns | 0.0260 ns | 13.994 ns |  1.01 |    0.03 |      - |         - |          NA |
| UpdateExistingBenchmarks          | Update_Entry_NotExists                   |  5.339 ns | 0.1283 ns | 0.3701 ns |  5.204 ns |  0.39 |    0.03 |      - |         - |          NA |
| UpsertBenchmarks                  | Upsert_Traditional_Exists                |  6.423 ns | 0.0154 ns | 0.0129 ns |  6.427 ns |  0.46 |    0.01 |      - |         - |          NA |
| UpsertBenchmarks                  | Upsert_Traditional_NotExists             |  6.058 ns | 0.0227 ns | 0.0223 ns |  6.053 ns |  0.44 |    0.01 |      - |         - |          NA |
| UpsertBenchmarks                  | Upsert_Entry_Exists                      | 13.250 ns | 0.2066 ns | 0.2379 ns | 13.165 ns |  0.96 |    0.03 |      - |         - |          NA |
| UpsertBenchmarks                  | Upsert_Entry_NotExists                   | 12.230 ns | 0.0556 ns | 0.0464 ns | 12.209 ns |  0.88 |    0.02 |      - |         - |          NA |
