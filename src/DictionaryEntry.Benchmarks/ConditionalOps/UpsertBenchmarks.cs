using BenchmarkDotNet.Attributes;

namespace DictionaryEntry.Benchmarks.ConditionalOps;

[BenchmarkCategory("ConditionalOps")]
public class UpsertBenchmarks : StringIntBenchmarkBase
{
    private static void UpsertTraditional(Dictionary<string, int> dict, string key)
    {
        dict[key] = 15;
    }

    private static void UpsertEntry(Dictionary<string, int> dict, string key)
    {
        dict.Entry(key).InsertEntry(15);
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = BatchSize)]
    public void Upsert_Traditional_Exists()
    {
        for (var i = 0; i < BatchSize; i++)
        {
            UpsertTraditional(_existingDictionary, _keys[i]);
        }
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public void Upsert_Traditional_NotExists()
    {
        _emptyDictionary.Clear();
        for (var i = 0; i < BatchSize; i++)
        {
            UpsertTraditional(_emptyDictionary, _keys[i]);
        }
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public void Upsert_Entry_Exists()
    {
        for (var i = 0; i < BatchSize; i++)
        {
            UpsertEntry(_existingDictionary, _keys[i]);
        }
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public void Upsert_Entry_NotExists()
    {
        _emptyDictionary.Clear();
        for (var i = 0; i < BatchSize; i++)
        {
            UpsertEntry(_emptyDictionary, _keys[i]);
        }
    }
}
