using BenchmarkDotNet.Attributes;

namespace DictionaryEntry.Benchmarks.AdvancedOps;

[BenchmarkCategory("AdvancedOps")]
public class PatternMatchingBenchmarks : BenchmarkBase
{
    private Dictionary<string, int> _existingDictionary = null!;
    private Dictionary<string, int> _emptyDictionary = null!;
    private string[] _keys = null!;

    [GlobalSetup]
    public void Setup()
    {
        _existingDictionary = new Dictionary<string, int>(capacity: BatchSize * 2);
        _emptyDictionary = new Dictionary<string, int>(capacity: BatchSize * 2);
        _keys = new string[BatchSize];
        for (var i = 0; i < BatchSize; i++)
        {
            var key = $"key_{i}";
            _keys[i] = key;
            _existingDictionary[key] = 10;
        }
    }

    private static string PatternMatchingTraditional(Dictionary<string, int> dict, string key)
    {
        if (dict.TryGetValue(key, out var value))
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

    private static string PatternMatchingEntryMatch(Dictionary<string, int> dict, string key)
    {
        return dict.Entry(key).Match(
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

    private static string PatternMatchingEntrySwitch(Dictionary<string, int> dict, string key)
    {
        return dict.Entry(key) switch
        {
            OccupiedEntry<string, int> { Value: > 100 } => "Very large",
            OccupiedEntry<string, int> { Value: > 50 } => "Large",
            OccupiedEntry<string, int> { Value: > 10 } => "Medium",
            OccupiedEntry<string, int> { Value: > 0 } => "Small",
            OccupiedEntry<string, int> => "Zero or negative",
            VacantEntry<string, int> => "Not found",
        };
    }

    private static void DifferentActionsTraditional(Dictionary<string, int> dict, string key)
    {
        if (dict.TryGetValue(key, out var value))
        {
            dict[key] = value * 2;
        }
        else
        {
            dict[key] = 1;
        }
    }

    private static void DifferentActionsEntryMatch(Dictionary<string, int> dict, string key)
    {
        dict.Entry(key).Match(
            occupied => occupied.Insert(occupied.Value * 2),
            vacant => vacant.Insert(1));
    }

    private static void DifferentActionsEntrySwitch(Dictionary<string, int> dict, string key)
    {
        switch (dict.Entry(key))
        {
            case OccupiedEntry<string, int> occupied:
                occupied.Insert(occupied.Value * 2);
                break;
            case VacantEntry<string, int> vacant:
                vacant.Insert(1);
                break;
        }
    }

    private static void StatefulActionsEntryMatchCapturing(Dictionary<string, int> dict, string key, int factor)
    {
        dict.Entry(key).Match(
            occupied => occupied.Insert(occupied.Value * factor),
            vacant => vacant.Insert(factor));
    }

    private static void StatefulActionsEntryMatchState(Dictionary<string, int> dict, string key, int factor)
    {
        dict.Entry(key).Match(
            factor,
            static (occupied, f) => occupied.Insert(occupied.Value * f),
            static (vacant, f) => vacant.Insert(f));
    }

    private static void StatefulActionsEntrySwitch(Dictionary<string, int> dict, string key, int factor)
    {
        switch (dict.Entry(key))
        {
            case OccupiedEntry<string, int> occupied:
                occupied.Insert(occupied.Value * factor);
                break;
            case VacantEntry<string, int> vacant:
                vacant.Insert(factor);
                break;
        }
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = BatchSize)]
    public string PatternMatching_Traditional_Exists()
    {
        var last = "";
        for (var i = 0; i < BatchSize; i++)
        {
            last = PatternMatchingTraditional(_existingDictionary, _keys[i]);
        }

        return last;
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public string PatternMatching_Traditional_NotExists()
    {
        var last = "";
        for (var i = 0; i < BatchSize; i++)
        {
            last = PatternMatchingTraditional(_emptyDictionary, _keys[i]);
        }

        return last;
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public string PatternMatching_EntryMatch_Exists()
    {
        var last = "";
        for (var i = 0; i < BatchSize; i++)
        {
            last = PatternMatchingEntryMatch(_existingDictionary, _keys[i]);
        }

        return last;
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public string PatternMatching_EntryMatch_NotExists()
    {
        var last = "";
        for (var i = 0; i < BatchSize; i++)
        {
            last = PatternMatchingEntryMatch(_emptyDictionary, _keys[i]);
        }

        return last;
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public string PatternMatching_EntrySwitch_Exists()
    {
        var last = "";
        for (var i = 0; i < BatchSize; i++)
        {
            last = PatternMatchingEntrySwitch(_existingDictionary, _keys[i]);
        }

        return last;
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public string PatternMatching_EntrySwitch_NotExists()
    {
        var last = "";
        for (var i = 0; i < BatchSize; i++)
        {
            last = PatternMatchingEntrySwitch(_emptyDictionary, _keys[i]);
        }

        return last;
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public void DifferentActions_Traditional_Exists()
    {
        for (var i = 0; i < BatchSize; i++)
        {
            DifferentActionsTraditional(_existingDictionary, _keys[i]);
        }
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public void DifferentActions_Traditional_NotExists()
    {
        _emptyDictionary.Clear();
        for (var i = 0; i < BatchSize; i++)
        {
            DifferentActionsTraditional(_emptyDictionary, _keys[i]);
        }
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public void DifferentActions_EntryMatch_Exists()
    {
        for (var i = 0; i < BatchSize; i++)
        {
            DifferentActionsEntryMatch(_existingDictionary, _keys[i]);
        }
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public void DifferentActions_EntryMatch_NotExists()
    {
        _emptyDictionary.Clear();
        for (var i = 0; i < BatchSize; i++)
        {
            DifferentActionsEntryMatch(_emptyDictionary, _keys[i]);
        }
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public void DifferentActions_EntrySwitch_Exists()
    {
        for (var i = 0; i < BatchSize; i++)
        {
            DifferentActionsEntrySwitch(_existingDictionary, _keys[i]);
        }
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public void DifferentActions_EntrySwitch_NotExists()
    {
        _emptyDictionary.Clear();
        for (var i = 0; i < BatchSize; i++)
        {
            DifferentActionsEntrySwitch(_emptyDictionary, _keys[i]);
        }
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public void StatefulActions_EntryMatchCapturing_Exists()
    {
        for (var i = 0; i < BatchSize; i++)
        {
            StatefulActionsEntryMatchCapturing(_existingDictionary, _keys[i], 2);
        }
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public void StatefulActions_EntryMatchCapturing_NotExists()
    {
        _emptyDictionary.Clear();
        for (var i = 0; i < BatchSize; i++)
        {
            StatefulActionsEntryMatchCapturing(_emptyDictionary, _keys[i], 2);
        }
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public void StatefulActions_EntryMatchState_Exists()
    {
        for (var i = 0; i < BatchSize; i++)
        {
            StatefulActionsEntryMatchState(_existingDictionary, _keys[i], 2);
        }
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public void StatefulActions_EntryMatchState_NotExists()
    {
        _emptyDictionary.Clear();
        for (var i = 0; i < BatchSize; i++)
        {
            StatefulActionsEntryMatchState(_emptyDictionary, _keys[i], 2);
        }
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public void StatefulActions_EntrySwitch_Exists()
    {
        for (var i = 0; i < BatchSize; i++)
        {
            StatefulActionsEntrySwitch(_existingDictionary, _keys[i], 2);
        }
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public void StatefulActions_EntrySwitch_NotExists()
    {
        _emptyDictionary.Clear();
        for (var i = 0; i < BatchSize; i++)
        {
            StatefulActionsEntrySwitch(_emptyDictionary, _keys[i], 2);
        }
    }
}
