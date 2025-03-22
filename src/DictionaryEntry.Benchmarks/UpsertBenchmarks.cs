using BenchmarkDotNet.Attributes;

namespace DictionaryEntry.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(invocationCount: 10_000_000)]
[BenchmarkCategory("Upsert")]
public class UpsertBenchmarks
{
    private Dictionary<string, int> _dictionary = null!;
    private const string ExistingKey = "existing";
    private const string NewKey = "new";

    [IterationSetup]
    public void Setup()
    {
        _dictionary = new Dictionary<string, int> { { ExistingKey, 10 } };
    }

    private void UpsertTraditional(string key)
    {
        _dictionary[key] = 15;
    }

    private void UpsertEntry(string key)
    {
        _dictionary.Entry(key).InsertEntry(15);
    }

    [Benchmark(Baseline = true)]
    public void Upsert_Traditional_Exists() => UpsertTraditional(ExistingKey);

    [Benchmark]
    public void Upsert_Traditional_NotExists() => UpsertTraditional(NewKey);

    [Benchmark]
    public void Upsert_Entry_Exists() => UpsertEntry(ExistingKey);

    [Benchmark]
    public void Upsert_Entry_NotExists() => UpsertEntry(NewKey);
}
