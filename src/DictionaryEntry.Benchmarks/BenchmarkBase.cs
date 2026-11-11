using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;

namespace DictionaryEntry.Benchmarks;

[Config(typeof(BenchmarkConfig))]
[MemoryDiagnoser(displayGenColumns: false)]
[HideColumns("Error", "StdDev", "Median", "Ratio", "Alloc Ratio", "RatioSD")]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByJob)]
[CategoriesColumn]
public abstract class BenchmarkBase
{
    private sealed class BenchmarkConfig : ManualConfig
    {
        public BenchmarkConfig()
        {
            AddJob(Job.Default
                .WithStrategy(RunStrategy.Throughput)
                .WithIterationCount(15)
                .WithWarmupCount(10)
                .WithInvocationCount(100_000_000)
                .WithToolchain(InProcessNoEmitToolchain.Instance));
        }
    }
}
