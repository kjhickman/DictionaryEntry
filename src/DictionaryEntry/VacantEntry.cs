using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace DictionaryEntry;

/// <summary>
/// Represents an entry that does not exist in the dictionary.
/// Provides members for inserting values into the dictionary at the associated key.
/// </summary>
/// <typeparam name="TKey">The type of the key in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of the value in the dictionary.</typeparam>
public readonly struct VacantEntry<TKey, TValue> where TKey : notnull
{
    private readonly Dictionary<TKey, TValue> _dictionary;
    private readonly TKey _key;

    internal Dictionary<TKey, TValue> Dictionary => _dictionary;

    internal VacantEntry(Dictionary<TKey, TValue> dictionary, TKey key)
    {
        _dictionary = dictionary;
        _key = key;
    }

    /// <summary>
    /// Gets the key associated with this entry.
    /// </summary>
    public TKey Key
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _key;
    }

    /// <summary>
    /// Inserts a value into the dictionary with the key associated with this entry.
    /// </summary>
    /// <param name="value">The value to insert.</param>
    /// <returns>The inserted value.</returns>
    /// <exception cref="InvalidOperationException">Thrown if this is a <c>default</c> entry.</exception>
    /// <remarks>
    /// This method assumes the key does not already exist in the dictionary.
    /// If the key exists, the existing value will be overwritten.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TValue Insert(TValue value)
    {
        if (_dictionary is null)
        {
            ThrowDefaultEntry();
        }

        _dictionary[_key] = value;
        return value;
    }

    /// <summary>
    /// Inserts a value into the dictionary and returns an occupied entry for the newly inserted entry.
    /// </summary>
    /// <param name="value">The value to insert.</param>
    /// <returns>An <see cref="OccupiedEntry{TKey, TValue}"/> representing the newly inserted entry.</returns>
    /// <exception cref="InvalidOperationException">Thrown if this is a <c>default</c> entry.</exception>
    /// <remarks>
    /// This method provides a fluent interface for chaining operations on the newly inserted entry.
    /// If the key exists, the existing value will be overwritten.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public OccupiedEntry<TKey, TValue> InsertEntry(TValue value)
    {
        if (_dictionary is null)
        {
            ThrowDefaultEntry();
        }

        _dictionary[_key] = value;
        return new OccupiedEntry<TKey, TValue>(_dictionary, _key, value);
    }

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowDefaultEntry() =>
        throw new InvalidOperationException(
            "The vacant entry is not associated with a dictionary. Obtain entries with Dictionary.Entry(key) rather than default.");
}
