using BenchmarkDotNet.Attributes;

namespace DictionaryEntry.Benchmarks.BasicOps;

[BenchmarkCategory("BasicOps")]
public class RetrieveAndRemoveBenchmarks : BenchmarkBase
{
    private Dictionary<string, int> _dictionary = null!;
    private Dictionary<string, int> _emptyDictionary = null!;
    private string[] _keys = null!;

    [GlobalSetup]
    public void Setup()
    {
        _dictionary = new Dictionary<string, int>(capacity: BatchSize * 2);
        _emptyDictionary = new Dictionary<string, int>(capacity: BatchSize * 2);
        _keys = new string[BatchSize];
        for (var i = 0; i < BatchSize; i++)
        {
            _keys[i] = $"key_{i}";
        }
    }

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
            _dictionary[_keys[i]] = 10;
        }

        var sum = 0;
        for (var i = 0; i < BatchSize; i++)
        {
            sum += GetAndRemoveTraditional(_dictionary, _keys[i]) ?? 0;
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
            _dictionary[_keys[i]] = 10;
        }

        var sum = 0;
        for (var i = 0; i < BatchSize; i++)
        {
            sum += GetAndRemoveEntry(_dictionary, _keys[i]) ?? 0;
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
