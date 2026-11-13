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
    protected const int BatchSize = 256;

    private sealed class BenchmarkConfig : ManualConfig
    {
        public BenchmarkConfig()
        {
            AddJob(Job.Default
                .WithStrategy(RunStrategy.Throughput)
                .WithMinWarmupCount(5)
                .WithMaxWarmupCount(8)
                .WithMinIterationCount(8)
                .WithMaxIterationCount(15)
                .WithMaxRelativeError(0.01)
                .WithInvocationCount(64_000)
                .WithToolchain(InProcessNoEmitToolchain.Instance));
        }
    }
}
