using BenchmarkDotNet.Attributes;

namespace DictionaryEntry.Benchmarks.BasicOps;

[BenchmarkCategory("BasicOps")]
public class InitializeAbsentBenchmarks : BenchmarkBase
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

    private static bool InitializeAbsentTraditional(Dictionary<string, int> dict, string key)
    {
        return dict.TryAdd(key, 1);
    }

    private static bool InitializeAbsentEntryApi(Dictionary<string, int> dict, string key)
    {
        if (dict.Entry(key).TryGetVacant(out var vacant))
        {
            vacant.Insert(1);
            return true;
        }

        return false;
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = BatchSize)]
    public int InitializeAbsent_Traditional_Exists()
    {
        var count = 0;
        for (var i = 0; i < BatchSize; i++)
        {
            if (InitializeAbsentTraditional(_existingDictionary, _keys[i])) count++;
        }

        return count;
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public int InitializeAbsent_Traditional_NotExists()
    {
        _emptyDictionary.Clear();
        var count = 0;
        for (var i = 0; i < BatchSize; i++)
        {
            if (InitializeAbsentTraditional(_emptyDictionary, _keys[i])) count++;
        }

        return count;
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public int InitializeAbsent_Entry_Exists()
    {
        var count = 0;
        for (var i = 0; i < BatchSize; i++)
        {
            if (InitializeAbsentEntryApi(_existingDictionary, _keys[i])) count++;
        }

        return count;
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public int InitializeAbsent_Entry_NotExists()
    {
        _emptyDictionary.Clear();
        var count = 0;
        for (var i = 0; i < BatchSize; i++)
        {
            if (InitializeAbsentEntryApi(_emptyDictionary, _keys[i])) count++;
        }

        return count;
    }
}
