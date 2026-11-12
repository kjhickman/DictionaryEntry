using BenchmarkDotNet.Attributes;

namespace DictionaryEntry.Benchmarks.FactoryOps;

[BenchmarkCategory("FactoryOps")]
public class FactoryMethodBenchmarks : BenchmarkBase
{
    private Dictionary<string, int> _dictionary = null!;
    private const string ExistingKey = "existing";
    private const string NewKey = "new";

    [IterationSetup]
    public void Setup()
    {
        _dictionary = new Dictionary<string, int> { { ExistingKey, 10 } };
    }

    private static int ComputeExpensiveValue()
    {
        return DateTime.UtcNow.Second + DateTime.UtcNow.Minute;
    }

    private static int ComputeFromKey(string key)
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

    private int StatefulFactoryTraditional(string key, int multiplier)
    {
        if (!_dictionary.TryGetValue(key, out var value))
        {
            value = key.Length * multiplier;
            _dictionary[key] = value;
        }

        return value;
    }

    private int StatefulFactoryCapturingEntry(string key, int multiplier)
    {
        return _dictionary.Entry(key).OrInsertWithKey(k => k.Length * multiplier);
    }

    private int StatefulFactoryStateEntry(string key, int multiplier)
    {
        return _dictionary.Entry(key).OrInsertWithKey(multiplier, static (k, mult) => k.Length * mult);
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

    [Benchmark]
    public int StatefulFactory_Traditional_Exists() => StatefulFactoryTraditional(ExistingKey, 10);

    [Benchmark]
    public int StatefulFactory_Traditional_NotExists() => StatefulFactoryTraditional(NewKey, 10);

    [Benchmark]
    public int StatefulFactory_EntryCapturing_Exists() => StatefulFactoryCapturingEntry(ExistingKey, 10);

    [Benchmark]
    public int StatefulFactory_EntryCapturing_NotExists() => StatefulFactoryCapturingEntry(NewKey, 10);

    [Benchmark]
    public int StatefulFactory_EntryState_Exists() => StatefulFactoryStateEntry(ExistingKey, 10);

    [Benchmark]
    public int StatefulFactory_EntryState_NotExists() => StatefulFactoryStateEntry(NewKey, 10);
}
