using BenchmarkDotNet.Attributes;
// ReSharper disable PreferConcreteValueOverDefault

namespace DictionaryEntry.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(invocationCount: 10_000_000)]
[BenchmarkCategory("DefaultValue")]
public class DefaultValueBenchmarks
{
    private Dictionary<string, int> _dictionary = null!;
    private Dictionary<string, string?> _stringDictionary = null!;
    private const string ExistingKey = "existing";
    private const string ExistingStringKey = "existingString";
    private const string NewKey = "new";

    [IterationSetup]
    public void Setup()
    {
        _dictionary = new Dictionary<string, int> { { ExistingKey, 10 } };
        _stringDictionary = new Dictionary<string, string?> { { ExistingStringKey, "value" } };
    }

    private int DefaultValueTraditional(string key)
    {
        if (!_dictionary.TryGetValue(key, out var value))
        {
            value = default;
            _dictionary[key] = value;
        }
        return value;
    }

    private string? DefaultValueTraditionalString(string key)
    {
        if (!_stringDictionary.TryGetValue(key, out var value))
        {
            value = default;
            _stringDictionary[key] = value;
        }
        return value;
    }

    private int DefaultValueEntry(string key)
    {
        return _dictionary.Entry(key).OrDefault();
    }

    private string? DefaultValueEntryString(string key)
    {
        return _stringDictionary.Entry(key).OrDefault();
    }

    [Benchmark(Baseline = true)]
    public int DefaultValue_Traditional_Exists() => DefaultValueTraditional(ExistingKey);

    [Benchmark]
    public int DefaultValue_Traditional_NotExists() => DefaultValueTraditional(NewKey);

    [Benchmark]
    public int DefaultValue_Entry_Exists() => DefaultValueEntry(ExistingKey);

    [Benchmark]
    public int DefaultValue_Entry_NotExists() => DefaultValueEntry(NewKey);

    [Benchmark]
    public string? DefaultValue_TraditionalString_NotExists() => DefaultValueTraditionalString(NewKey);

    [Benchmark]
    public string? DefaultValue_EntryString_NotExists() => DefaultValueEntryString(NewKey);
}
