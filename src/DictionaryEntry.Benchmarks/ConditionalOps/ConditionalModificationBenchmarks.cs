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

    private static void ConditionalModifyTraditional(Dictionary<string, int> dict, string key)
    {
        if (dict.TryGetValue(key, out var value))
        {
            if (value > Threshold)
            {
                dict[key] = value * 2;
            }
            else
            {
                dict[key] = value + 1;
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
                    occupied.Insert(value * 2);
                }
                else
                {
                    occupied.Insert(value + 1);
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
