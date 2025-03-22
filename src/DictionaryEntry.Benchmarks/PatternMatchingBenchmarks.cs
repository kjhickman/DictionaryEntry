using BenchmarkDotNet.Attributes;

namespace DictionaryEntry.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(invocationCount: 10_000_000)]
[BenchmarkCategory("PatternMatching")]
public class PatternMatchingBenchmarks
{
    private Dictionary<string, int> _dictionary = null!;
    private const string ExistingKey = "existing";
    private const string NewKey = "new";

    [IterationSetup]
    public void Setup()
    {
        _dictionary = new Dictionary<string, int> { { ExistingKey, 10 } };
    }

    private string PatternMatchingTraditional(string key)
    {
        if (_dictionary.TryGetValue(key, out var value))
        {
            return value switch
            {
                > 100 => "Very large",
                > 50 => "Large",
                > 10 => "Medium",
                > 0 => "Small",
                _ => "Zero or negative"
            };
        }
        return "Not found";
    }

    private string PatternMatchingEntry(string key)
    {
        return _dictionary.Entry(key).Match(
            occupied =>
            {
                return occupied.Value() switch
                {
                    > 100 => "Very large",
                    > 50 => "Large",
                    > 10 => "Medium",
                    > 0 => "Small",
                    _ => "Zero or negative"
                };
            },
            _ => "Not found"
        );
    }

    private void DifferentActionsTraditional(string key)
    {
        if (_dictionary.TryGetValue(key, out var value))
        {
            _dictionary[key] = value * 2;
        }
        else
        {
            _dictionary[key] = 1;
        }
    }

    private void DifferentActionsEntry(string key)
    {
        _dictionary.Entry(key).Match(
            occupied => occupied.Insert(occupied.Value() * 2),
            vacant => vacant.Insert(1)
        );
    }

    [Benchmark]
    public string PatternMatching_Traditional_Exists() => PatternMatchingTraditional(ExistingKey);

    [Benchmark]
    public string PatternMatching_Traditional_NotExists() => PatternMatchingTraditional(NewKey);

    [Benchmark]
    public string PatternMatching_Entry_Exists() => PatternMatchingEntry(ExistingKey);

    [Benchmark]
    public string PatternMatching_Entry_NotExists() => PatternMatchingEntry(NewKey);

    [Benchmark]
    public void DifferentActions_Traditional_Exists() => DifferentActionsTraditional(ExistingKey);

    [Benchmark]
    public void DifferentActions_Traditional_NotExists() => DifferentActionsTraditional(NewKey);

    [Benchmark]
    public void DifferentActions_Entry_Exists() => DifferentActionsEntry(ExistingKey);

    [Benchmark]
    public void DifferentActions_Entry_NotExists() => DifferentActionsEntry(NewKey);
}
