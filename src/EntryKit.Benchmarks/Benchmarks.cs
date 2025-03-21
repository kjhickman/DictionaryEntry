using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

namespace EntryKit.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
public class Benchmarks
{
    private Dictionary<string, int> _dictionary = null!;
    private const string ExistingKey = "existing";
    private const string NewKey = "new";

    [GlobalSetup]
    public void Setup()
    {
        _dictionary = new Dictionary<string, int> { { ExistingKey, 10 } };
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Insert if not exists, new key")]
    public void Traditional_OrInsert_NotExists()
    {
        if (!_dictionary.TryGetValue(NewKey, out _))
        {
            _dictionary[NewKey] = 1;
        }
    }

    [Benchmark]
    [BenchmarkCategory("Insert if not exists, new key")]
    public void EntryAPI_OrInsert_NotExists()
    {
        _dictionary.Entry(NewKey).OrInsert(1);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Insert if not exists, exists")]
    public void Traditional_OrInsert_Exists()
    {
        if (!_dictionary.TryGetValue(ExistingKey, out _))
        {
            _dictionary[ExistingKey] = 1;
        }
    }

    [Benchmark]
    [BenchmarkCategory("Insert if not exists, exists")]
    public void EntryAPI_OrInsert_Exists()
    {
        _dictionary.Entry(ExistingKey).OrInsert(1);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Update if exists, exists")]
    public void Traditional_Update_Exists()
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

    [Benchmark]
    [BenchmarkCategory("Update if exists, exists")]
    public void EntryAPI_Update_Exists()
    {
        _dictionary.Entry(ExistingKey).AndModify(x => x + 1).OrInsert(1);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Update if exists, new key")]
    public void Traditional_Update_NewKey()
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
    [BenchmarkCategory("Update if exists, new key")]
    public void EntryAPI_Update_NewKey()
    {
        _dictionary.Entry(NewKey).AndModify(x => x + 1).OrInsert(1);
    }

    private static int ComputeValue()
    {
        // Simulate some computation
        return DateTime.Now.Year;
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Insert with factory, new key")]
    public void Traditional_OrInsertWithFactory_NotExists()
    {
        if (!_dictionary.TryGetValue(NewKey, out _))
        {
            _dictionary[NewKey] = ComputeValue();
        }
    }

    [Benchmark]
    [BenchmarkCategory("Insert with factory, new key")]
    public void EntryAPI_OrInsertWithFactory_NotExists()
    {
        _dictionary.Entry(NewKey).OrInsertWith(ComputeValue);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Insert with factory, exists")]
    public void Traditional_OrInsertWithFactory_Exists()
    {
        if (!_dictionary.TryGetValue(ExistingKey, out _))
        {
            _dictionary[ExistingKey] = ComputeValue();
        }
    }

    [Benchmark]
    [BenchmarkCategory("Insert with factory, exists")]
    public void EntryAPI_OrInsertWithFactory_Exists()
    {
        _dictionary.Entry(ExistingKey).OrInsertWith(ComputeValue);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Insert if vacant, new key")]
    public void Traditional_InsertIfVacant_NotExists()
    {
        if (!_dictionary.TryGetValue(NewKey, out _))
        {
            _dictionary[NewKey] = 1;
        }
    }

    [Benchmark]
    [BenchmarkCategory("Insert if vacant, new key")]
    public void EntryAPI_InsertIfVacant_NotExists()
    {
        if (_dictionary.Entry(NewKey).TryGetVacant(out var vacant))
        {
            vacant.Insert(1);
        }
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Insert if vacant, exists")]
    public void Traditional_InsertIfVacant_Exists()
    {
        if (!_dictionary.TryGetValue(ExistingKey, out _))
        {
            _dictionary[ExistingKey] = 1;
        }
    }

    [Benchmark]
    [BenchmarkCategory("Insert if vacant, exists")]
    public void EntryAPI_InsertIfVacant_Exists()
    {
        if (_dictionary.Entry(ExistingKey).TryGetVacant(out var vacant))
        {
            vacant.Insert(1);
        }
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Conditional removal, exists")]
    public void Traditional_ConditionalRemove_Exists()
    {
        if (_dictionary.TryGetValue(ExistingKey, out var value) && value > 5)
        {
            _dictionary.Remove(ExistingKey);
        }
    }

    [Benchmark]
    [BenchmarkCategory("Conditional removal, exists")]
    public void EntryAPI_ConditionalRemove_Exists()
    {
        if (_dictionary.Entry(ExistingKey).TryGetOccupied(out var occupied) && occupied.Value() > 5)
        {
            occupied.Remove();
        }
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Conditional removal, not exists")]
    public void Traditional_ConditionalRemove_NotExists()
    {
        if (_dictionary.TryGetValue(NewKey, out var value) && value > 5)
        {
            _dictionary.Remove(NewKey);
        }
    }

    [Benchmark]
    [BenchmarkCategory("Conditional removal, not exists")]
    public void EntryAPI_ConditionalRemove_NotExists()
    {
        if (_dictionary.Entry(NewKey).TryGetOccupied(out var occupied) && occupied.Value() > 5)
        {
            occupied.Remove();
        }
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("GetOrCompute, exists")]
    public int Traditional_GetOrCompute_Exists()
    {
        if (!_dictionary.TryGetValue(ExistingKey, out var value))
        {
            value = ComputeExpensiveValue(ExistingKey);
            _dictionary[ExistingKey] = value;
        }

        return value;
    }

    [Benchmark]
    [BenchmarkCategory("GetOrCompute, exists")]
    public int EntryAPI_GetOrCompute_Exists()
    {
        return _dictionary.Entry(ExistingKey).Match(
            occupied => occupied.Value(),
            vacant => vacant.Insert(ComputeExpensiveValue(vacant.Key())));
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("GetOrCompute, not exists")]
    public int Traditional_GetOrCompute_NotExists()
    {
        if (!_dictionary.TryGetValue(NewKey, out var value))
        {
            value = ComputeExpensiveValue(NewKey);
            _dictionary[NewKey] = value;
        }

        return value;
    }

    [Benchmark]
    [BenchmarkCategory("GetOrCompute, not exists")]
    public int EntryAPI_GetOrCompute_NotExists()
    {
        return _dictionary.Entry(NewKey).Match(
            occupied => occupied.Value(),
            vacant => vacant.Insert(ComputeExpensiveValue(vacant.Key())));
    }

    private int ComputeExpensiveValue(string key)
    {
        // Simulate an expensive computation
        return key.Length * 10;
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Batch operations, exists")]
    public void Traditional_BatchOperations_Exists()
    {
        if (_dictionary.TryGetValue(ExistingKey, out var value))
        {
            // Multiple operations on the same key
            value *= 2;
            value = Math.Max(0, value);
            value += 5;
            _dictionary[ExistingKey] = value;
        }
        else
        {
            _dictionary[ExistingKey] = 5;
        }
    }

    [Benchmark]
    [BenchmarkCategory("Batch operations, exists")]
    public void EntryAPI_BatchOperations_Exists()
    {
        _dictionary.Entry(ExistingKey).Match(
            occupied =>
            {
                // Multiple operations on the same key
                var value = occupied.Value();
                value *= 2;
                value = Math.Max(0, value);
                value += 5;
                occupied.Insert(value);
            },
            vacant => vacant.Insert(5));
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Batch operations, not exists")]
    public void Traditional_BatchOperations_NotExists()
    {
        if (_dictionary.TryGetValue(NewKey, out var value))
        {
            // Multiple operations on the same key
            value *= 2;
            value = Math.Max(0, value);
            value += 5;
            _dictionary[NewKey] = value;
        }
        else
        {
            _dictionary[NewKey] = 5;
        }
    }

    [Benchmark]
    [BenchmarkCategory("Batch operations, not exists")]
    public void EntryAPI_BatchOperations_NotExists()
    {
        _dictionary.Entry(NewKey).Match(
            occupied =>
            {
                // Multiple operations on the same key
                var value = occupied.Value();
                value *= 2;
                value = Math.Max(0, value);
                value += 5;
                occupied.Insert(value);
            },
            vacant => vacant.Insert(5));
    }
}
