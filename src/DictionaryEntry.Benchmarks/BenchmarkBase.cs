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

public abstract class IntIntBenchmarkBase : BenchmarkBase
{
    protected Dictionary<int, int> _existingDictionary = null!;
    protected Dictionary<int, int> _emptyDictionary = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _existingDictionary = new Dictionary<int, int>(capacity: BatchSize * 2);
        _emptyDictionary = new Dictionary<int, int>(capacity: BatchSize * 2);
        for (var i = 0; i < BatchSize; i++)
        {
            _existingDictionary[i] = 10;
        }
    }
}

public abstract class StringIntBenchmarkBase : BenchmarkBase
{
    protected Dictionary<string, int> _existingDictionary = null!;
    protected Dictionary<string, int> _emptyDictionary = null!;
    protected string[] _keys = null!;

    [GlobalSetup]
    public virtual void GlobalSetup()
    {
        _existingDictionary = new Dictionary<string, int>(capacity: BatchSize * 2);
        _emptyDictionary = new Dictionary<string, int>(capacity: BatchSize * 2);
        _keys = new string[BatchSize];
        for (var i = 0; i < BatchSize; i++)
        {
            var key = $"key_{i}";
            _keys[i] = key;
            _existingDictionary[key] = 10;
        }
    }
}

public abstract class StringIntMutableBenchmarkBase : StringIntBenchmarkBase
{
    [IterationSetup]
    public void IterationSetup()
    {
        for (var i = 0; i < BatchSize; i++)
        {
            _existingDictionary[_keys[i]] = 10;
        }
    }
}
