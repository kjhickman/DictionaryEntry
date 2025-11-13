using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;

namespace DictionaryEntry.Benchmarks;

[SimpleJob(RunStrategy.Throughput, iterationCount: 15, warmupCount: 10, invocationCount: 100_000_000)]
[MemoryDiagnoser(displayGenColumns: false)]
[HideColumns("Error", "StdDev", "Median", "Ratio", "Alloc Ratio", "RatioSD")]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByJob)]
[CategoriesColumn]
public abstract class BenchmarkBase;
