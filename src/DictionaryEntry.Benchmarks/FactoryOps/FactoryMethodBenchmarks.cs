using BenchmarkDotNet.Attributes;

namespace DictionaryEntry.Benchmarks.FactoryOps;

[BenchmarkCategory("FactoryOps")]
public class FactoryMethodBenchmarks : BenchmarkBase
{
    private Dictionary<string, int> _existingDictionary = null!;
    private Dictionary<string, int> _emptyDictionary = null!;
    private string[] _keys = null!;
    private int _multiplier = 10;

    [GlobalSetup]
    public void Setup()
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

    private static int ComputeExpensiveValue()
    {
        return DateTime.UtcNow.Second + DateTime.UtcNow.Minute;
    }

    private static int ComputeFromKey(string key)
    {
        return key.Length * 10;
    }

    private static int FactoryMethodTraditional(Dictionary<string, int> dict, string key)
    {
        if (!dict.TryGetValue(key, out var value))
        {
            value = ComputeExpensiveValue();
            dict[key] = value;
        }

        return value;
    }

    private static int KeyBasedFactoryTraditional(Dictionary<string, int> dict, string key)
    {
        if (!dict.TryGetValue(key, out var value))
        {
            value = ComputeFromKey(key);
            dict[key] = value;
        }

        return value;
    }

    private static int FactoryMethodEntry(Dictionary<string, int> dict, string key)
    {
        return dict.Entry(key).OrInsertWith(ComputeExpensiveValue);
    }

    private static int KeyBasedFactoryEntry(Dictionary<string, int> dict, string key)
    {
        return dict.Entry(key).OrInsertWithKey(ComputeFromKey);
    }

    private static int StatefulFactoryTraditional(Dictionary<string, int> dict, string key, int multiplier)
    {
        if (!dict.TryGetValue(key, out var value))
        {
            value = key.Length * multiplier;
            dict[key] = value;
        }

        return value;
    }

    private static int StatefulFactoryCapturingEntry(Dictionary<string, int> dict, string key, int multiplier)
    {
        return dict.Entry(key).OrInsertWithKey(k => k.Length * multiplier);
    }

    private static int StatefulFactoryStateEntry(Dictionary<string, int> dict, string key, int multiplier)
    {
        return dict.Entry(key).OrInsertWithKey(multiplier, static (k, mult) => k.Length * mult);
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = BatchSize)]
    public int FactoryMethod_Traditional_Exists()
    {
        var sum = 0;
        for (var i = 0; i < BatchSize; i++)
        {
            sum += FactoryMethodTraditional(_existingDictionary, _keys[i]);
        }

        return sum;
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public int FactoryMethod_Traditional_NotExists()
    {
        _emptyDictionary.Clear();
        var sum = 0;
        for (var i = 0; i < BatchSize; i++)
        {
            sum += FactoryMethodTraditional(_emptyDictionary, _keys[i]);
        }

        return sum;
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public int FactoryMethod_Entry_Exists()
    {
        var sum = 0;
        for (var i = 0; i < BatchSize; i++)
        {
            sum += FactoryMethodEntry(_existingDictionary, _keys[i]);
        }

        return sum;
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public int FactoryMethod_Entry_NotExists()
    {
        _emptyDictionary.Clear();
        var sum = 0;
        for (var i = 0; i < BatchSize; i++)
        {
            sum += FactoryMethodEntry(_emptyDictionary, _keys[i]);
        }

        return sum;
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public int KeyBasedFactory_Traditional_Exists()
    {
        var sum = 0;
        for (var i = 0; i < BatchSize; i++)
        {
            sum += KeyBasedFactoryTraditional(_existingDictionary, _keys[i]);
        }

        return sum;
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public int KeyBasedFactory_Traditional_NotExists()
    {
        _emptyDictionary.Clear();
        var sum = 0;
        for (var i = 0; i < BatchSize; i++)
        {
            sum += KeyBasedFactoryTraditional(_emptyDictionary, _keys[i]);
        }

        return sum;
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public int KeyBasedFactory_Entry_Exists()
    {
        var sum = 0;
        for (var i = 0; i < BatchSize; i++)
        {
            sum += KeyBasedFactoryEntry(_existingDictionary, _keys[i]);
        }

        return sum;
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public int KeyBasedFactory_Entry_NotExists()
    {
        _emptyDictionary.Clear();
        var sum = 0;
        for (var i = 0; i < BatchSize; i++)
        {
            sum += KeyBasedFactoryEntry(_emptyDictionary, _keys[i]);
        }

        return sum;
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public int StatefulFactory_Traditional_Exists()
    {
        var multiplier = _multiplier;
        var sum = 0;
        for (var i = 0; i < BatchSize; i++)
        {
            sum += StatefulFactoryTraditional(_existingDictionary, _keys[i], multiplier);
        }

        return sum;
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public int StatefulFactory_Traditional_NotExists()
    {
        _emptyDictionary.Clear();
        var multiplier = _multiplier;
        var sum = 0;
        for (var i = 0; i < BatchSize; i++)
        {
            sum += StatefulFactoryTraditional(_emptyDictionary, _keys[i], multiplier);
        }

        return sum;
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public int StatefulFactory_EntryCapturing_Exists()
    {
        var multiplier = _multiplier;
        var sum = 0;
        for (var i = 0; i < BatchSize; i++)
        {
            sum += StatefulFactoryCapturingEntry(_existingDictionary, _keys[i], multiplier);
        }

        return sum;
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public int StatefulFactory_EntryCapturing_NotExists()
    {
        _emptyDictionary.Clear();
        var multiplier = _multiplier;
        var sum = 0;
        for (var i = 0; i < BatchSize; i++)
        {
            sum += StatefulFactoryCapturingEntry(_emptyDictionary, _keys[i], multiplier);
        }

        return sum;
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public int StatefulFactory_EntryState_Exists()
    {
        var multiplier = _multiplier;
        var sum = 0;
        for (var i = 0; i < BatchSize; i++)
        {
            sum += StatefulFactoryStateEntry(_existingDictionary, _keys[i], multiplier);
        }

        return sum;
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public int StatefulFactory_EntryState_NotExists()
    {
        _emptyDictionary.Clear();
        var multiplier = _multiplier;
        var sum = 0;
        for (var i = 0; i < BatchSize; i++)
        {
            sum += StatefulFactoryStateEntry(_emptyDictionary, _keys[i], multiplier);
        }

        return sum;
    }
}
