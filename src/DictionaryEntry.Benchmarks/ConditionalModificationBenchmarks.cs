using BenchmarkDotNet.Attributes;

namespace DictionaryEntry.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(invocationCount: 10_000_000)]
[BenchmarkCategory("ConditionalModification")]
public class ConditionalModificationBenchmarks
{
    private Dictionary<string, int> _dictionary = null!;
    private const string ExistingKey = "existing";
    private const string NewKey = "new";
    private const int Threshold = 5;

    [IterationSetup]
    public void Setup()
    {
        _dictionary = new Dictionary<string, int> { { ExistingKey, 10 } };
    }

    private void ConditionalModifyTraditional(string key)
    {
        if (_dictionary.TryGetValue(key, out var value))
        {
            if (value > Threshold)
            {
                _dictionary[key] = value * 2;
            }
            else
            {
                _dictionary[key] = value + 1;
            }
        }
        else
        {
            _dictionary[key] = 1;
        }
    }

    private void ConditionalModifyEntry(string key)
    {
        _dictionary.Entry(key).Match(
            occupied =>
            {
                var value = occupied.Value();
                if (value > Threshold)
                {
                    occupied.Insert(value * 2);
                }
                else
                {
                    occupied.Insert(value + 1);
                }
            },
            vacant => vacant.Insert(1)
        );
    }

    [Benchmark]
    public void ConditionalModify_Traditional_Exists() => ConditionalModifyTraditional(ExistingKey);

    [Benchmark]
    public void ConditionalModify_Traditional_NotExists() => ConditionalModifyTraditional(NewKey);

    [Benchmark]
    public void ConditionalModify_Entry_Exists() => ConditionalModifyEntry(ExistingKey);

    [Benchmark]
    public void ConditionalModify_Entry_NotExists() => ConditionalModifyEntry(NewKey);
}
