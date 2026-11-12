using BenchmarkDotNet.Attributes;

namespace DictionaryEntry.Benchmarks.BasicOps;

[BenchmarkCategory("BasicOps")]
public class DefaultValueBenchmarks : BenchmarkBase
{
    private Dictionary<string, int> _existingDictionary = null!;
    private Dictionary<string, int> _emptyDictionary = null!;
    private Dictionary<string, string?> _emptyStringDictionary = null!;
    private string[] _keys = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _existingDictionary = new Dictionary<string, int>(capacity: BatchSize * 2);
        _emptyDictionary = new Dictionary<string, int>(capacity: BatchSize * 2);
        _emptyStringDictionary = new Dictionary<string, string?>(capacity: BatchSize * 2);
        _keys = new string[BatchSize];
        for (var i = 0; i < BatchSize; i++)
        {
            var key = $"key_{i}";
            _keys[i] = key;
            _existingDictionary[key] = 10;
        }
    }

    private static int DefaultValueTraditional(Dictionary<string, int> dict, string key)
    {
        if (!dict.TryGetValue(key, out var value))
        {
            value = default;
            dict[key] = value;
        }

        return value;
    }

    private static string? DefaultValueTraditionalString(Dictionary<string, string?> dict, string key)
    {
        if (!dict.TryGetValue(key, out var value))
        {
            value = default;
            dict[key] = value;
        }

        return value;
    }

    private static int DefaultValueEntry(Dictionary<string, int> dict, string key)
    {
        return dict.Entry(key).OrDefault();
    }

    private static string? DefaultValueEntryString(Dictionary<string, string?> dict, string key)
    {
        return dict.Entry(key).OrDefault();
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = BatchSize)]
    public int DefaultValue_Traditional_Exists()
    {
        var sum = 0;
        for (var i = 0; i < BatchSize; i++)
        {
            sum += DefaultValueTraditional(_existingDictionary, _keys[i]);
        }

        return sum;
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public int DefaultValue_Traditional_NotExists()
    {
        _emptyDictionary.Clear();
        var sum = 0;
        for (var i = 0; i < BatchSize; i++)
        {
            sum += DefaultValueTraditional(_emptyDictionary, _keys[i]);
        }

        return sum;
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public int DefaultValue_Entry_Exists()
    {
        var sum = 0;
        for (var i = 0; i < BatchSize; i++)
        {
            sum += DefaultValueEntry(_existingDictionary, _keys[i]);
        }

        return sum;
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public int DefaultValue_Entry_NotExists()
    {
        _emptyDictionary.Clear();
        var sum = 0;
        for (var i = 0; i < BatchSize; i++)
        {
            sum += DefaultValueEntry(_emptyDictionary, _keys[i]);
        }

        return sum;
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public string? DefaultValue_TraditionalString_NotExists()
    {
        _emptyStringDictionary.Clear();
        string? last = null;
        for (var i = 0; i < BatchSize; i++)
        {
            last = DefaultValueTraditionalString(_emptyStringDictionary, _keys[i]);
        }

        return last;
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public string? DefaultValue_EntryString_NotExists()
    {
        _emptyStringDictionary.Clear();
        string? last = null;
        for (var i = 0; i < BatchSize; i++)
        {
            last = DefaultValueEntryString(_emptyStringDictionary, _keys[i]);
        }

        return last;
    }
}
