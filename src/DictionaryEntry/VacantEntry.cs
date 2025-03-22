using System.Runtime.CompilerServices;

namespace DictionaryEntry;

/// <summary>
/// Represents an entry that does not exist in the dictionary.
/// Provides methods for inserting values into the dictionary.
/// </summary>
/// <typeparam name="TKey">The type of the key in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of the value in the dictionary.</typeparam>
public readonly struct VacantEntry<TKey, TValue> where TKey : notnull
{
    private readonly Dictionary<TKey, TValue> _dictionary;
    private readonly TKey _key;

    internal VacantEntry(Dictionary<TKey, TValue> dictionary, TKey key)
    {
        _dictionary = dictionary;
        _key = key;
    }

    /// <summary>
    /// Gets the key associated with this entry.
    /// </summary>
    /// <returns>The key associated with this entry.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TKey Key() => _key;

    /// <summary>
    /// Inserts a value into the dictionary with the key associated with this entry.
    /// </summary>
    /// <param name="value">The value to insert.</param>
    /// <returns>The inserted value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TValue Insert(TValue value)
    {
        _dictionary[_key] = value;
        return value;
    }

    /// <summary>
    /// Inserts a value into the dictionary and returns an occupied entry for the newly inserted entry.
    /// </summary>
    /// <param name="value">The value to insert.</param>
    /// <returns>An <see cref="OccupiedEntry{TKey, TValue}"/> representing the newly inserted entry.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public OccupiedEntry<TKey, TValue> InsertEntry(TValue value)
    {
        _dictionary[_key] = value;
        return new OccupiedEntry<TKey, TValue>(_dictionary, _key, value);
    }
}
