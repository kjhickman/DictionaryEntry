using BenchmarkDotNet.Attributes;

namespace DictionaryEntry.Benchmarks.ConditionalOps;

[BenchmarkCategory("ConditionalOps")]
public class ConditionalModificationBenchmarks : BenchmarkBase
{
    private Dictionary<string, int> _existingDictionary = null!;
    private Dictionary<string, int> _emptyDictionary = null!;
    private string[] _keys = null!;
    private const int Threshold = 5;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _existingDictionary = new Dictionary<string, int>(capacity: BatchSize * 2);
        _emptyDictionary = new Dictionary<string, int>(capacity: BatchSize * 2);
        _keys = new string[BatchSize];
        for (var i = 0; i < BatchSize; i++)
        {
            _keys[i] = $"key_{i}";
        }
    }

    [IterationSetup]
    public void IterationSetup()
    {
        // Half above Threshold (10), half below Threshold (4); updates toggle 10 <-> 4 so neither overflows
        for (var i = 0; i < BatchSize; i++)
        {
            _existingDictionary[_keys[i]] = (i & 1) == 0 ? 10 : 4;
        }
    }

    private static void ConditionalModifyTraditional(Dictionary<string, int> dict, string key)
    {
        if (dict.TryGetValue(key, out var value))
        {
            if (value > Threshold)
            {
                dict[key] = value - 6;
            }
            else
            {
                dict[key] = value + 6;
            }
        }
        else
        {
            dict[key] = 1;
        }
    }

    private static void ConditionalModifyEntry(Dictionary<string, int> dict, string key)
    {
        dict.Entry(key).Match(
            occupied =>
            {
                var value = occupied.Value;
                if (value > Threshold)
                {
                    occupied.Insert(value - 6);
                }
                else
                {
                    occupied.Insert(value + 6);
                }
            },
            vacant => vacant.Insert(1));
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = BatchSize)]
    public void ConditionalModify_Traditional_Exists()
    {
        for (var i = 0; i < BatchSize; i++)
        {
            ConditionalModifyTraditional(_existingDictionary, _keys[i]);
        }
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public void ConditionalModify_Traditional_NotExists()
    {
        _emptyDictionary.Clear();
        for (var i = 0; i < BatchSize; i++)
        {
            ConditionalModifyTraditional(_emptyDictionary, _keys[i]);
        }
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public void ConditionalModify_Entry_Exists()
    {
        for (var i = 0; i < BatchSize; i++)
        {
            ConditionalModifyEntry(_existingDictionary, _keys[i]);
        }
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public void ConditionalModify_Entry_NotExists()
    {
        _emptyDictionary.Clear();
        for (var i = 0; i < BatchSize; i++)
        {
            ConditionalModifyEntry(_emptyDictionary, _keys[i]);
        }
    }
}
