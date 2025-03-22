using BenchmarkDotNet.Attributes;

namespace DictionaryEntry.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(invocationCount: 10_000_000)]
[BenchmarkCategory("FactoryMethod")]
public class FactoryMethodBenchmarks
{
    private Dictionary<string, int> _dictionary = null!;
    private const string ExistingKey = "existing";
    private const string NewKey = "new";

    [IterationSetup]
    public void Setup()
    {
        _dictionary = new Dictionary<string, int> { { ExistingKey, 10 } };
    }

    private int ComputeExpensiveValue()
    {
        return DateTime.UtcNow.Second + DateTime.UtcNow.Minute;
    }

    private int ComputeFromKey(string key)
    {
        return key.Length * 10;
    }

    private int FactoryMethodTraditional(string key)
    {
        if (!_dictionary.TryGetValue(key, out var value))
        {
            value = ComputeExpensiveValue();
            _dictionary[key] = value;
        }
        return value;
    }

    private int KeyBasedFactoryTraditional(string key)
    {
        if (!_dictionary.TryGetValue(key, out var value))
        {
            value = ComputeFromKey(key);
            _dictionary[key] = value;
        }
        return value;
    }

    private int FactoryMethodEntry(string key)
    {
        return _dictionary.Entry(key).OrInsertWith(ComputeExpensiveValue);
    }

    private int KeyBasedFactoryEntry(string key)
    {
        return _dictionary.Entry(key).OrInsertWithKey(ComputeFromKey);
    }

    [Benchmark(Baseline = true)]
    public int FactoryMethod_Traditional_Exists() => FactoryMethodTraditional(ExistingKey);

    [Benchmark]
    public int FactoryMethod_Traditional_NotExists() => FactoryMethodTraditional(NewKey);

    [Benchmark]
    public int FactoryMethod_Entry_Exists() => FactoryMethodEntry(ExistingKey);

    [Benchmark]
    public int FactoryMethod_Entry_NotExists() => FactoryMethodEntry(NewKey);

    [Benchmark]
    public int KeyBasedFactory_Traditional_Exists() => KeyBasedFactoryTraditional(ExistingKey);

    [Benchmark]
    public int KeyBasedFactory_Traditional_NotExists() => KeyBasedFactoryTraditional(NewKey);

    [Benchmark]
    public int KeyBasedFactory_Entry_Exists() => KeyBasedFactoryEntry(ExistingKey);

    [Benchmark]
    public int KeyBasedFactory_Entry_NotExists() => KeyBasedFactoryEntry(NewKey);
}
