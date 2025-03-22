namespace DictionaryEntry;

/// <summary>
/// Represents an entry that exists in the dictionary.
/// Provides methods for manipulating an existing dictionary entry.
/// </summary>
/// <typeparam name="TKey">The type of the key in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of the value in the dictionary.</typeparam>
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

    /// <summary>
    /// Gets the key associated with this entry.
    /// </summary>
    /// <returns>The key associated with this entry.</returns>
    public TKey Key() => _key;

    /// <summary>
    /// Gets the value associated with this entry.
    /// </summary>
    /// <returns>The value associated with this entry.</returns>
    public TValue Value() => _value;

    /// <summary>
    /// Updates the value of this entry in the dictionary.
    /// </summary>
    /// <param name="newValue">The new value to set.</param>
    /// <returns>The newly set value.</returns>
    public TValue Insert(TValue newValue)
    {
        _dictionary[_key] = newValue;
        return newValue;
    }

    /// <summary>
    /// Removes this entry from the dictionary and returns the value that was removed.
    /// </summary>
    /// <returns>The value that was removed from the dictionary.</returns>
    public TValue Remove()
    {
        _dictionary.Remove(_key);
        return _value;
    }

    /// <summary>
    /// Removes this entry from the dictionary and returns both the key and the value.
    /// </summary>
    /// <returns>A tuple containing the key and value that were removed from the dictionary.</returns>
    public (TKey, TValue) RemoveEntry()
    {
        _dictionary.Remove(_key, out var value);
        return (_key, value)!;
    }
}
