using BenchmarkDotNet.Attributes;

namespace DictionaryEntry.Benchmarks.AdvancedOps;

[BenchmarkCategory("AdvancedOps")]
public class PatternMatchingBenchmarks : BenchmarkBase
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

    private string PatternMatchingEntryMatch(string key)
    {
        return _dictionary.Entry(key).Match(
            occupied => occupied.Value switch
            {
                > 100 => "Very large",
                > 50 => "Large",
                > 10 => "Medium",
                > 0 => "Small",
                _ => "Zero or negative"
            },
            _ => "Not found");
    }

    private string PatternMatchingEntrySwitch(string key)
    {
        return _dictionary.Entry(key) switch
        {
            OccupiedEntry<string, int> { Value: > 100 } => "Very large",
            OccupiedEntry<string, int> { Value: > 50 } => "Large",
            OccupiedEntry<string, int> { Value: > 10 } => "Medium",
            OccupiedEntry<string, int> { Value: > 0 } => "Small",
            OccupiedEntry<string, int> => "Zero or negative",
            VacantEntry<string, int> => "Not found",
        };
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

    private void DifferentActionsEntryMatch(string key)
    {
        _dictionary.Entry(key).Match(
            occupied => occupied.Insert(occupied.Value * 2),
            vacant => vacant.Insert(1));
    }

    private void DifferentActionsEntrySwitch(string key)
    {
        switch (_dictionary.Entry(key))
        {
            case OccupiedEntry<string, int> occupied:
                occupied.Insert(occupied.Value * 2);
                break;
            case VacantEntry<string, int> vacant:
                vacant.Insert(1);
                break;
        }
    }

    [Benchmark(Baseline = true)]
    public string PatternMatching_Traditional_Exists() => PatternMatchingTraditional(ExistingKey);

    [Benchmark]
    public string PatternMatching_Traditional_NotExists() => PatternMatchingTraditional(NewKey);

    [Benchmark]
    public string PatternMatching_EntryMatch_Exists() => PatternMatchingEntryMatch(ExistingKey);

    [Benchmark]
    public string PatternMatching_EntryMatch_NotExists() => PatternMatchingEntryMatch(NewKey);

    [Benchmark]
    public string PatternMatching_EntrySwitch_Exists() => PatternMatchingEntrySwitch(ExistingKey);

    [Benchmark]
    public string PatternMatching_EntrySwitch_NotExists() => PatternMatchingEntrySwitch(NewKey);

    [Benchmark]
    public void DifferentActions_Traditional_Exists() => DifferentActionsTraditional(ExistingKey);

    [Benchmark]
    public void DifferentActions_Traditional_NotExists() => DifferentActionsTraditional(NewKey);

    [Benchmark]
    public void DifferentActions_EntryMatch_Exists() => DifferentActionsEntryMatch(ExistingKey);

    [Benchmark]
    public void DifferentActions_EntryMatch_NotExists() => DifferentActionsEntryMatch(NewKey);

    [Benchmark]
    public void DifferentActions_EntrySwitch_Exists() => DifferentActionsEntrySwitch(ExistingKey);

    [Benchmark]
    public void DifferentActions_EntrySwitch_NotExists() => DifferentActionsEntrySwitch(NewKey);
}
