namespace EntryKit;

public readonly struct VacantEntry<TKey, TValue> where TKey : notnull
{
    private readonly Dictionary<TKey, TValue> _dictionary;
    private readonly TKey _key;

    internal VacantEntry(Dictionary<TKey, TValue> dictionary, TKey key)
    {
        _dictionary = dictionary;
        _key = key;
    }

    public TKey Key() => _key;

    public TValue Insert(TValue value)
    {
        _dictionary[_key] = value;
        return value;
    }

    public OccupiedEntry<TKey, TValue> ToOccupiedEntry(TValue value)
    {
        _dictionary[_key] = value;
        return new OccupiedEntry<TKey, TValue>(_dictionary, _key, value);
    }
}
