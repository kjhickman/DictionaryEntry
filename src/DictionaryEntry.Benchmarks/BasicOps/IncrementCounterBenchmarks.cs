using BenchmarkDotNet.Attributes;

namespace DictionaryEntry.Benchmarks.BasicOps;

[BenchmarkCategory("BasicOps")]
public class IncrementCounterBenchmarks : BenchmarkBase
{
    private Dictionary<string, int> _existingDictionary = null!;
    private Dictionary<string, int> _emptyDictionary = null!;
    private string[] _keys = null!;

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

    private static void IncrementCounterTraditional(Dictionary<string, int> dict, string key)
    {
        if (dict.TryGetValue(key, out var count))
        {
            dict[key] = count + 1;
        }
        else
        {
            dict[key] = 1;
        }
    }

    private static void IncrementCounterEntryAndModify(Dictionary<string, int> dict, string key)
    {
        dict.Entry(key).AndModify(count => count + 1).OrInsert(1);
    }

    private static void IncrementByAmountTraditional(Dictionary<string, int> dict, string key, int amount)
    {
        if (dict.TryGetValue(key, out var count))
        {
            dict[key] = count + amount;
        }
        else
        {
            dict[key] = amount;
        }
    }

    private static void IncrementByAmountEntryAndModify(Dictionary<string, int> dict, string key, int amount)
    {
        dict.Entry(key).AndModify(count => count + amount).OrInsert(amount);
    }

    private static void IncrementByAmountEntryAndModifyState(Dictionary<string, int> dict, string key, int amount)
    {
        dict.Entry(key).AndModify(amount, static (count, amt) => count + amt).OrInsert(amount);
    }

    private static void IncrementByAmountEntrySwitch(Dictionary<string, int> dict, string key, int amount)
    {
        switch (dict.Entry(key))
        {
            case OccupiedEntry<string, int> occupied:
                occupied.Insert(occupied.Value + amount);
                break;
            case VacantEntry<string, int> vacant:
                vacant.Insert(amount);
                break;
        }
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = BatchSize)]
    public void IncrementCounter_Traditional_Exists()
    {
        for (var i = 0; i < BatchSize; i++)
        {
            IncrementCounterTraditional(_existingDictionary, _keys[i]);
        }
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public void IncrementCounter_Traditional_NotExists()
    {
        _emptyDictionary.Clear();
        for (var i = 0; i < BatchSize; i++)
        {
            IncrementCounterTraditional(_emptyDictionary, _keys[i]);
        }
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public void IncrementCounter_EntryAndModify_Exists()
    {
        for (var i = 0; i < BatchSize; i++)
        {
            IncrementCounterEntryAndModify(_existingDictionary, _keys[i]);
        }
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public void IncrementCounter_EntryAndModify_NotExists()
    {
        _emptyDictionary.Clear();
        for (var i = 0; i < BatchSize; i++)
        {
            IncrementCounterEntryAndModify(_emptyDictionary, _keys[i]);
        }
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public void IncrementByAmount_Traditional_Exists()
    {
        for (var i = 0; i < BatchSize; i++)
        {
            IncrementByAmountTraditional(_existingDictionary, _keys[i], 5);
        }
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public void IncrementByAmount_Traditional_NotExists()
    {
        _emptyDictionary.Clear();
        for (var i = 0; i < BatchSize; i++)
        {
            IncrementByAmountTraditional(_emptyDictionary, _keys[i], 5);
        }
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public void IncrementByAmount_EntryAndModify_Exists()
    {
        for (var i = 0; i < BatchSize; i++)
        {
            IncrementByAmountEntryAndModify(_existingDictionary, _keys[i], 5);
        }
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public void IncrementByAmount_EntryAndModify_NotExists()
    {
        _emptyDictionary.Clear();
        for (var i = 0; i < BatchSize; i++)
        {
            IncrementByAmountEntryAndModify(_emptyDictionary, _keys[i], 5);
        }
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public void IncrementByAmount_EntryAndModifyState_Exists()
    {
        for (var i = 0; i < BatchSize; i++)
        {
            IncrementByAmountEntryAndModifyState(_existingDictionary, _keys[i], 5);
        }
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public void IncrementByAmount_EntryAndModifyState_NotExists()
    {
        _emptyDictionary.Clear();
        for (var i = 0; i < BatchSize; i++)
        {
            IncrementByAmountEntryAndModifyState(_emptyDictionary, _keys[i], 5);
        }
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public void IncrementByAmount_EntrySwitch_Exists()
    {
        for (var i = 0; i < BatchSize; i++)
        {
            IncrementByAmountEntrySwitch(_existingDictionary, _keys[i], 5);
        }
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public void IncrementByAmount_EntrySwitch_NotExists()
    {
        _emptyDictionary.Clear();
        for (var i = 0; i < BatchSize; i++)
        {
            IncrementByAmountEntrySwitch(_emptyDictionary, _keys[i], 5);
        }
    }
}
