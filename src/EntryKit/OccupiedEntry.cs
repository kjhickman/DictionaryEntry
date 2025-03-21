namespace EntryKit;

public readonly struct OccupiedEntry<TKey, TValue> where TKey : notnull
{
    private readonly Dictionary<TKey, TValue> _dictionary;
    private readonly TKey _key;
    private readonly TValue _value;

    internal OccupiedEntry(Dictionary<TKey, TValue> dictionary, TKey key, TValue value)
    {
        _dictionary = dictionary;
        _key = key;
        _value = value;
    }

    public TKey Key() => _key;
    public TValue Value() => _value;

    public TValue Insert(TValue newValue)
    {
        _dictionary[_key] = newValue;
        return newValue;
    }

    public TValue Remove()
    {
        _dictionary.Remove(_key);
        return _value;
    }

    public (TKey, TValue) RemoveEntry()
    {
        _dictionary.Remove(_key, out var value);
        return (_key, value)!;
    }
}
