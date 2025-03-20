using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

namespace EntryKit.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
public class DictionaryBenchmarks
{
    private Dictionary<string, int> _dictionary = null!;
    private const string ExistingKey = "existing";
    private const string NewKey = "new";

    [GlobalSetup]
    public void Setup()
    {
        _dictionary = new Dictionary<string, int> { { ExistingKey, 10 } };
    }

    // Benchmark 1: Increment a counter that exists
    [Benchmark]
    [BenchmarkCategory("Increment Existing")]
    public void Traditional_IncrementExisting()
    {
        if (_dictionary.TryGetValue(ExistingKey, out var value))
        {
            _dictionary[ExistingKey] = value + 1;
        }
        else
        {
            _dictionary[ExistingKey] = 1;
        }
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Increment Existing")]
    public void EntryAPI_IncrementExisting()
    {
        _dictionary.Entry(ExistingKey).AndModify(x => ++x).OrInsert(1);
    }

    // Benchmark 2: Increment or create a counter
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Increment new key")]
    public void Traditional_IncrementOrCreate()
    {
        if (_dictionary.TryGetValue(NewKey, out var value))
        {
            _dictionary[NewKey] = value + 1;
        }
        else
        {
            _dictionary[NewKey] = 1;
        }
    }

    [Benchmark]
    [BenchmarkCategory("Increment new key")]
    public void EntryAPI_IncrementOrCreate()
    {
        _dictionary.Entry(ExistingKey).AndModify(x => ++x).OrInsert(1);
    }

    // Benchmark 3: Create a value with factory function
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("OrInsertWith")]
    public int Traditional_CreateWithFactory()
    {
        if (!_dictionary.TryGetValue(NewKey, out var value))
        {
            value = DateTime.Now.Year;
            _dictionary[NewKey] = value;
        }

        return value;
    }

    [Benchmark]
    [BenchmarkCategory("OrInsertWith")]
    public int EntryAPI_CreateWithFactory()
    {
        return _dictionary.Entry(NewKey).OrInsertWith(() => DateTime.Now.Year);
    }

    // Benchmark 4: Create a value based on key
    [Benchmark(Baseline = true)]
    [BenchmarkCategory("OrInsertWithKey")]
    public int Traditional_CreateBasedOnKey()
    {
        if (!_dictionary.TryGetValue(NewKey, out var value))
        {
            value = NewKey.Length * 2;
            _dictionary[NewKey] = value;
        }

        return value;
    }

    [Benchmark]
    [BenchmarkCategory("OrInsertWithKey")]
    public int EntryAPI_CreateBasedOnKey()
    {
        return _dictionary.Entry(NewKey).OrInsertWithKey(key => key.Length * 2);
    }
}
