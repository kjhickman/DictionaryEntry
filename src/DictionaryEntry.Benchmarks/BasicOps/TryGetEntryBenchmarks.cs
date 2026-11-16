using BenchmarkDotNet.Attributes;

namespace DictionaryEntry.Benchmarks.BasicOps;

public class TryGetEntryBenchmarks : StringIntBenchmarkBase
{
    private static int? TryLookupTraditional(Dictionary<string, int> dict, string key)
    {
        return dict.TryGetValue(key, out var value) ? value : null;
    }

    private static int? TryLookupTryGetOccupied(Dictionary<string, int> dict, string key)
    {
        if (dict.Entry(key).TryGetOccupied(out var occupied))
        {
            return occupied.Value;
        }

        return null;
    }

    private static int? TryLookupIsPattern(Dictionary<string, int> dict, string key)
    {
        if (dict.Entry(key) is OccupiedEntry<string, int> occupied)
        {
            return occupied.Value;
        }

        return null;
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = BatchSize), BenchmarkCategory("Exists")]
    public int TryLookup_Traditional_Exists()
    {
        var sum = 0;
        for (var i = 0; i < BatchSize; i++)
        {
            sum += TryLookupTraditional(_existingDictionary, _keys[i]) ?? 0;
        }

        return sum;
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = BatchSize), BenchmarkCategory("NotExists")]
    public int TryLookup_Traditional_NotExists()
    {
        var sum = 0;
        for (var i = 0; i < BatchSize; i++)
        {
            sum += TryLookupTraditional(_emptyDictionary, _keys[i]) ?? 0;
        }

        return sum;
    }

    [Benchmark(OperationsPerInvoke = BatchSize), BenchmarkCategory("Exists")]
    public int TryLookup_TryGetOccupied_Exists()
    {
        var sum = 0;
        for (var i = 0; i < BatchSize; i++)
        {
            sum += TryLookupTryGetOccupied(_existingDictionary, _keys[i]) ?? 0;
        }

        return sum;
    }

    [Benchmark(OperationsPerInvoke = BatchSize), BenchmarkCategory("NotExists")]
    public int TryLookup_TryGetOccupied_NotExists()
    {
        var sum = 0;
        for (var i = 0; i < BatchSize; i++)
        {
            sum += TryLookupTryGetOccupied(_emptyDictionary, _keys[i]) ?? 0;
        }

        return sum;
    }

    [Benchmark(OperationsPerInvoke = BatchSize), BenchmarkCategory("Exists")]
    public int TryLookup_IsPattern_Exists()
    {
        var sum = 0;
        for (var i = 0; i < BatchSize; i++)
        {
            sum += TryLookupIsPattern(_existingDictionary, _keys[i]) ?? 0;
        }

        return sum;
    }

    [Benchmark(OperationsPerInvoke = BatchSize), BenchmarkCategory("NotExists")]
    public int TryLookup_IsPattern_NotExists()
    {
        var sum = 0;
        for (var i = 0; i < BatchSize; i++)
        {
            sum += TryLookupIsPattern(_emptyDictionary, _keys[i]) ?? 0;
        }

        return sum;
    }
}
