using BenchmarkDotNet.Attributes;

namespace DictionaryEntry.Benchmarks.BasicOps;

[BenchmarkCategory("BasicOps")]
public class DictionaryContainsBenchmarks : IntIntBenchmarkBase
{
    [Benchmark(Baseline = true, OperationsPerInvoke = BatchSize)]
    public int TryGetValue_Found()
    {
        var count = 0;
        for (var i = 0; i < BatchSize; i++)
        {
            if (_existingDictionary.TryGetValue(i, out _)) count++;
        }

        return count;
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public int Entry_IsOccupied_Found()
    {
        var count = 0;
        for (var i = 0; i < BatchSize; i++)
        {
            if (_existingDictionary.Entry(i).IsOccupied) count++;
        }

        return count;
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public int TryGetValue_NotFound()
    {
        var count = 0;
        for (var i = 0; i < BatchSize; i++)
        {
            if (_emptyDictionary.TryGetValue(i, out _)) count++;
        }

        return count;
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public int Entry_IsOccupied_NotFound()
    {
        var count = 0;
        for (var i = 0; i < BatchSize; i++)
        {
            if (_emptyDictionary.Entry(i).IsOccupied) count++;
        }

        return count;
    }
}
