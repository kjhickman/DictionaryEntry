using BenchmarkDotNet.Attributes;

namespace DictionaryEntry.Benchmarks.BasicOps;

[BenchmarkCategory("BasicOps")]
public class DictionaryContainsBenchmarks : BenchmarkBase
{
    private Dictionary<int, int> _existingDictionary = null!;
    private Dictionary<int, int> _emptyDictionary = null!;

    [GlobalSetup]
    public void Setup()
    {
        _existingDictionary = new Dictionary<int, int>(capacity: BatchSize * 2);
        _emptyDictionary = new Dictionary<int, int>(capacity: BatchSize * 2);
        for (var i = 0; i < BatchSize; i++)
        {
            _existingDictionary[i] = 1;
        }
    }

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
