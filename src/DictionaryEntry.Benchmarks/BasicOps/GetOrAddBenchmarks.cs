using BenchmarkDotNet.Attributes;

namespace DictionaryEntry.Benchmarks.BasicOps;

public class GetOrAddBenchmarks : IntIntBenchmarkBase
{
    private static int GetOrAddTraditional(Dictionary<int, int> dict, int key)
    {
        if (dict.TryGetValue(key, out var value))
        {
            return value;
        }

        value = 1;
        dict[key] = value;
        return value;
    }

    private static int GetOrAddEntry(Dictionary<int, int> dict, int key)
    {
        return dict.Entry(key).OrInsert(1);
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = BatchSize)]
    public int GetOrAdd_Traditional_Exists()
    {
        var sum = 0;
        for (var i = 0; i < BatchSize; i++)
        {
            sum += GetOrAddTraditional(_existingDictionary, i);
        }

        return sum;
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public int GetOrAdd_Traditional_NotExists()
    {
        _emptyDictionary.Clear();
        var sum = 0;
        for (var i = 0; i < BatchSize; i++)
        {
            sum += GetOrAddTraditional(_emptyDictionary, i);
        }

        return sum;
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public int GetOrAdd_Entry_Exists()
    {
        var sum = 0;
        for (var i = 0; i < BatchSize; i++)
        {
            sum += GetOrAddEntry(_existingDictionary, i);
        }

        return sum;
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public int GetOrAdd_Entry_NotExists()
    {
        _emptyDictionary.Clear();
        var sum = 0;
        for (var i = 0; i < BatchSize; i++)
        {
            sum += GetOrAddEntry(_emptyDictionary, i);
        }

        return sum;
    }
}
