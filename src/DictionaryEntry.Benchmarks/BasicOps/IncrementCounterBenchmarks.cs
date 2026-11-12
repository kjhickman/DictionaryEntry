using BenchmarkDotNet.Attributes;

namespace DictionaryEntry.Benchmarks.BasicOps;

[BenchmarkCategory("BasicOps")]
public class IncrementCounterBenchmarks : BenchmarkBase
{
    private Dictionary<string, int> _dictionary = null!;
    private const string ExistingKey = "existing";
    private const string NewKey = "new";

    [IterationSetup]
    public void Setup()
    {
        _dictionary = new Dictionary<string, int> { { ExistingKey, 10 } };
    }

    private void IncrementCounterTraditional(string key)
    {
        if (_dictionary.TryGetValue(key, out var count))
        {
            _dictionary[key] = count + 1;
        }
        else
        {
            _dictionary[key] = 1;
        }
    }

    private void IncrementCounterEntryAndModify(string key)
    {
        _dictionary.Entry(key).AndModify(count => count + 1).OrInsert(1);
    }

    private void IncrementByAmountTraditional(string key, int amount)
    {
        if (_dictionary.TryGetValue(key, out var count))
        {
            _dictionary[key] = count + amount;
        }
        else
        {
            _dictionary[key] = amount;
        }
    }

    private void IncrementByAmountEntryAndModify(string key, int amount)
    {
        _dictionary.Entry(key).AndModify(count => count + amount).OrInsert(amount);
    }

    private void IncrementByAmountEntryAndModifyState(string key, int amount)
    {
        _dictionary.Entry(key).AndModify(amount, static (count, amt) => count + amt).OrInsert(amount);
    }

    private void IncrementByAmountEntrySwitch(string key, int amount)
    {
        switch (_dictionary.Entry(key))
        {
            case OccupiedEntry<string, int> occupied:
                occupied.Insert(occupied.Value + amount);
                break;
            case VacantEntry<string, int> vacant:
                vacant.Insert(amount);
                break;
        }
    }

    [Benchmark(Baseline = true)]
    public void IncrementCounter_Traditional_Exists() => IncrementCounterTraditional(ExistingKey);

    [Benchmark]
    public void IncrementCounter_Traditional_NotExists() => IncrementCounterTraditional(NewKey);

    [Benchmark]
    public void IncrementCounter_EntryAndModify_Exists() => IncrementCounterEntryAndModify(ExistingKey);

    [Benchmark]
    public void IncrementCounter_EntryAndModify_NotExists() => IncrementCounterEntryAndModify(NewKey);

    [Benchmark]
    public void IncrementByAmount_Traditional_Exists() => IncrementByAmountTraditional(ExistingKey, 5);

    [Benchmark]
    public void IncrementByAmount_Traditional_NotExists() => IncrementByAmountTraditional(NewKey, 5);

    [Benchmark]
    public void IncrementByAmount_EntryAndModify_Exists() => IncrementByAmountEntryAndModify(ExistingKey, 5);

    [Benchmark]
    public void IncrementByAmount_EntryAndModify_NotExists() => IncrementByAmountEntryAndModify(NewKey, 5);

    [Benchmark]
    public void IncrementByAmount_EntryAndModifyState_Exists() => IncrementByAmountEntryAndModifyState(ExistingKey, 5);

    [Benchmark]
    public void IncrementByAmount_EntryAndModifyState_NotExists() => IncrementByAmountEntryAndModifyState(NewKey, 5);

    [Benchmark]
    public void IncrementByAmount_EntrySwitch_Exists() => IncrementByAmountEntrySwitch(ExistingKey, 5);

    [Benchmark]
    public void IncrementByAmount_EntrySwitch_NotExists() => IncrementByAmountEntrySwitch(NewKey, 5);
}
