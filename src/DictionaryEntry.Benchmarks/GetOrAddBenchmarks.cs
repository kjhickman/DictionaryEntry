using BenchmarkDotNet.Attributes;

namespace DictionaryEntry.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(invocationCount: 10_000_000)]
[BenchmarkCategory("GetOrAdd")]
public class GetOrAddBenchmarks
{
    private Dictionary<int, int> _dictionary = null!;
    private const int ExistingKey = 42;
    private const int NewKey = 123;

    [GlobalSetup]
    public void Setup()
    {
        _dictionary = new Dictionary<int, int> { { ExistingKey, 10 } };
    }

    [IterationCleanup]
    public void Cleanup()
    {
        _dictionary.Clear();
        _dictionary[ExistingKey] = 10;
    }

    private int GetOrAddTraditional(int key)
    {
        if (_dictionary.TryGetValue(key, out var val))
        {
            return val;
        }

        val = 1;
        _dictionary[key] = val;
        return val;
    }

    private int GetOrAddEntry(int key)
    {
        return _dictionary.Entry(key).OrInsert(1);
    }

    [Benchmark(Baseline = true)]
    public int GetOrAdd_Traditional_Exists() => GetOrAddTraditional(ExistingKey);

    [Benchmark]
    public int GetOrAdd_Traditional_NotExists() => GetOrAddTraditional(NewKey);

    [Benchmark]
    public int GetOrAdd_Entry_Exists() => GetOrAddEntry(ExistingKey);

    [Benchmark]
    public int GetOrAdd_Entry_NotExists() => GetOrAddEntry(NewKey);
}
