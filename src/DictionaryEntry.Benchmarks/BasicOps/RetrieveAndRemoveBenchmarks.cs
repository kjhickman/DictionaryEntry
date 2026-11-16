using BenchmarkDotNet.Attributes;

namespace DictionaryEntry.Benchmarks.BasicOps;

public class RetrieveAndRemoveBenchmarks : StringIntBenchmarkBase
{
    private static int? GetAndRemoveTraditional(Dictionary<string, int> dict, string key)
    {
        return dict.Remove(key, out var value) ? value : null;
    }

    private static int? GetAndRemoveEntry(Dictionary<string, int> dict, string key)
    {
        if (dict.Entry(key).TryGetOccupied(out var occupied))
        {
            return occupied.Remove();
        }

        return null;
    }

    // Note: Because each removal deletes the key, Exists batches re-populate the 256 keys before removing them;
    // both Traditional and Entry pay the identical re-population cost so their relative delta reflects removal overhead.
    [Benchmark(Baseline = true, OperationsPerInvoke = BatchSize)]
    public int GetAndRemove_Traditional_Exists()
    {
        for (var i = 0; i < BatchSize; i++)
        {
            _existingDictionary[_keys[i]] = 10;
        }

        var sum = 0;
        for (var i = 0; i < BatchSize; i++)
        {
            sum += GetAndRemoveTraditional(_existingDictionary, _keys[i]) ?? 0;
        }

        return sum;
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public int GetAndRemove_Traditional_NotExists()
    {
        var sum = 0;
        for (var i = 0; i < BatchSize; i++)
        {
            sum += GetAndRemoveTraditional(_emptyDictionary, _keys[i]) ?? 0;
        }

        return sum;
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public int GetAndRemove_Entry_Exists()
    {
        for (var i = 0; i < BatchSize; i++)
        {
            _existingDictionary[_keys[i]] = 10;
        }

        var sum = 0;
        for (var i = 0; i < BatchSize; i++)
        {
            sum += GetAndRemoveEntry(_existingDictionary, _keys[i]) ?? 0;
        }

        return sum;
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public int GetAndRemove_Entry_NotExists()
    {
        var sum = 0;
        for (var i = 0; i < BatchSize; i++)
        {
            sum += GetAndRemoveEntry(_emptyDictionary, _keys[i]) ?? 0;
        }

        return sum;
    }
}
