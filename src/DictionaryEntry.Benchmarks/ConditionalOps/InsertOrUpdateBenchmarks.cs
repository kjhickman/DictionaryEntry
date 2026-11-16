using BenchmarkDotNet.Attributes;

namespace DictionaryEntry.Benchmarks.ConditionalOps;

[BenchmarkCategory("ConditionalOps")]
public class InsertOrUpdateBenchmarks : StringIntMutableBenchmarkBase
{
    private static void InsertOrUpdateTraditional(Dictionary<string, int> dict, string key)
    {
        if (dict.TryGetValue(key, out var value))
        {
            value += 3;
            dict[key] = value;
        }
        else
        {
            dict[key] = 1;
        }
    }

    private static void InsertOrUpdateEntry(Dictionary<string, int> dict, string key)
    {
        dict.Entry(key).AndModify(x => x + 3).OrInsert(1);
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = BatchSize)]
    public void InsertOrUpdate_Traditional_Exists()
    {
        for (var i = 0; i < BatchSize; i++)
        {
            InsertOrUpdateTraditional(_existingDictionary, _keys[i]);
        }
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public void InsertOrUpdate_Traditional_NotExists()
    {
        _emptyDictionary.Clear();
        for (var i = 0; i < BatchSize; i++)
        {
            InsertOrUpdateTraditional(_emptyDictionary, _keys[i]);
        }
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public void InsertOrUpdate_Entry_Exists()
    {
        for (var i = 0; i < BatchSize; i++)
        {
            InsertOrUpdateEntry(_existingDictionary, _keys[i]);
        }
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public void InsertOrUpdate_Entry_NotExists()
    {
        _emptyDictionary.Clear();
        for (var i = 0; i < BatchSize; i++)
        {
            InsertOrUpdateEntry(_emptyDictionary, _keys[i]);
        }
    }
}
