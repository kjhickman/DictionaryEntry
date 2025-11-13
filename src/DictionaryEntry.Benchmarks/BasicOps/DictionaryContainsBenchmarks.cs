using BenchmarkDotNet.Attributes;

namespace DictionaryEntry.Benchmarks.BasicOps;

[BenchmarkCategory("BasicOps")]
public class DictionaryContainsBenchmarks : BenchmarkBase
{
    private Dictionary<int, int> _dictionary = null!;
    private const int ExistingKey = 42;
    private const int MissingKey = 21;

    [GlobalSetup]
    public void Setup()
    {
        _dictionary = new Dictionary<int, int> { { ExistingKey, 1 } };
    }

    [Benchmark(Baseline = true)]
    public bool TryGetValue_Found() => _dictionary.TryGetValue(ExistingKey, out _);

    [Benchmark]
    public bool Entry_IsOccupied_Found() => _dictionary.Entry(ExistingKey).IsOccupied();

    [Benchmark]
    public bool TryGetValue_NotFound() => _dictionary.TryGetValue(MissingKey, out _);

    [Benchmark]
    public bool Entry_IsOccupied_NotFound() => _dictionary.Entry(MissingKey).IsOccupied();
}
