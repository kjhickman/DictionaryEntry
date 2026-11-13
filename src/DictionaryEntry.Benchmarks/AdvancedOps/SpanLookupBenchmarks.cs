using BenchmarkDotNet.Attributes;

namespace DictionaryEntry.Benchmarks.AdvancedOps;

[BenchmarkCategory("AdvancedOps")]
public class SpanLookupBenchmarks : BenchmarkBase
{
    private Dictionary<string, int> _existingDictionary = null!;
    private Dictionary<string, int> _emptyDictionary = null!;
    private string _concatenatedBuffer = null!;
    private int[] _offsets = null!;
    private int[] _lengths = null!;

    [GlobalSetup]
    public void Setup()
    {
        _existingDictionary = new Dictionary<string, int>(capacity: BatchSize * 2, StringComparer.Ordinal);
        _emptyDictionary = new Dictionary<string, int>(capacity: BatchSize * 2, StringComparer.Ordinal);
        _offsets = new int[BatchSize];
        _lengths = new int[BatchSize];

        var sb = new System.Text.StringBuilder();
        for (var i = 0; i < BatchSize; i++)
        {
            var key = $"header_key_{i}";
            _offsets[i] = sb.Length;
            _lengths[i] = key.Length;
            sb.Append(key).Append(':');
            _existingDictionary[key] = 10;
        }

        _concatenatedBuffer = sb.ToString();
    }

    [Benchmark(Baseline = true, OperationsPerInvoke = BatchSize)]
    public int SpanLookup_TraditionalToString_Exists()
    {
        var buffer = _concatenatedBuffer.AsSpan();
        var sum = 0;
        for (var i = 0; i < BatchSize; i++)
        {
            var key = buffer.Slice(_offsets[i], _lengths[i]).ToString();
            if (!_existingDictionary.TryGetValue(key, out var val))
            {
                val = 1;
                _existingDictionary[key] = val;
            }

            sum += val;
        }

        return sum;
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public int SpanLookup_DirectEntry_Exists()
    {
        var buffer = _concatenatedBuffer.AsSpan();
        var sum = 0;
        for (var i = 0; i < BatchSize; i++)
        {
            var span = buffer.Slice(_offsets[i], _lengths[i]);
            sum += _existingDictionary.Entry(span).OrInsert(1);
        }

        return sum;
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public int SpanLookup_AlternateLookupEntry_Exists()
    {
        var buffer = _concatenatedBuffer.AsSpan();
        var lookup = _existingDictionary.GetAlternateLookup<ReadOnlySpan<char>>();
        var sum = 0;
        for (var i = 0; i < BatchSize; i++)
        {
            var span = buffer.Slice(_offsets[i], _lengths[i]);
            sum += lookup.Entry(span).OrInsert(1);
        }

        return sum;
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public int SpanLookup_TraditionalToString_NotExists()
    {
        _emptyDictionary.Clear();
        var buffer = _concatenatedBuffer.AsSpan();
        var sum = 0;
        for (var i = 0; i < BatchSize; i++)
        {
            var key = buffer.Slice(_offsets[i], _lengths[i]).ToString();
            if (!_emptyDictionary.TryGetValue(key, out var val))
            {
                val = 1;
                _emptyDictionary[key] = val;
            }

            sum += val;
        }

        return sum;
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public int SpanLookup_DirectEntry_NotExists()
    {
        _emptyDictionary.Clear();
        var buffer = _concatenatedBuffer.AsSpan();
        var sum = 0;
        for (var i = 0; i < BatchSize; i++)
        {
            var span = buffer.Slice(_offsets[i], _lengths[i]);
            sum += _emptyDictionary.Entry(span).OrInsert(1);
        }

        return sum;
    }

    [Benchmark(OperationsPerInvoke = BatchSize)]
    public int SpanLookup_AlternateLookupEntry_NotExists()
    {
        _emptyDictionary.Clear();
        var buffer = _concatenatedBuffer.AsSpan();
        var lookup = _emptyDictionary.GetAlternateLookup<ReadOnlySpan<char>>();
        var sum = 0;
        for (var i = 0; i < BatchSize; i++)
        {
            var span = buffer.Slice(_offsets[i], _lengths[i]);
            sum += lookup.Entry(span).OrInsert(1);
        }

        return sum;
    }
}
