using System.Runtime.CompilerServices;

namespace DictionaryEntry;

/// <summary>
/// Represents an entry looked up via an alternate key that exists in the dictionary.
/// Provides members for inspecting and manipulating an existing dictionary entry without allocating a <typeparamref name="TKey"/>.
/// </summary>
/// <typeparam name="TKey">The type of the key stored in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of the value stored in the dictionary.</typeparam>
/// <typeparam name="TAlternateKey">The type of the alternate key used for lookup.</typeparam>
public readonly ref struct AlternateOccupiedEntry<TKey, TValue, TAlternateKey>
    where TKey : notnull
    where TAlternateKey : notnull, allows ref struct
{
    private readonly Dictionary<TKey, TValue>.AlternateLookup<TAlternateKey> _lookup;
    private readonly TAlternateKey _key;
    private readonly TValue _value;
    private readonly ulong _location;
    private readonly ulong _stamp;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal AlternateOccupiedEntry(
        Dictionary<TKey, TValue>.AlternateLookup<TAlternateKey> lookup,
        TAlternateKey key,
        TValue value,
        ulong location)
    {
        _lookup = lookup;
        _key = key;
        _value = value;
        _location = location;
        _stamp = DictionaryAccess<TKey, TValue>.GetOccupiedStamp(lookup.Dictionary);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal AlternateOccupiedEntry(
        Dictionary<TKey, TValue>.AlternateLookup<TAlternateKey> lookup,
        TAlternateKey key,
        TValue value)
    {
        _lookup = lookup;
        _key = key;
        _value = value;
        _location = ulong.MaxValue;
        _stamp = 0;
    }

    /// <summary>
    /// Gets the alternate key associated with this entry.
    /// </summary>
    public TAlternateKey Key
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _key;
    }

    /// <summary>
    /// Gets the value associated with this entry at the time the entry was obtained.
    /// </summary>
    /// <remarks>
    /// This is a snapshot: it is not updated by <see cref="Insert"/> or by later changes to the dictionary.
    /// </remarks>
    public TValue Value
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _value;
    }

    /// <summary>
    /// Deconstructs this entry into its alternate key and snapshot value.
    /// </summary>
    /// <param name="key">The alternate key associated with this entry.</param>
    /// <param name="value">The value associated with this entry.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deconstruct(out TAlternateKey key, out TValue value)
    {
        key = _key;
        value = _value;
    }

    /// <summary>
    /// Updates the value of this entry in the dictionary.
    /// </summary>
    /// <param name="newValue">The new value to set.</param>
    /// <returns>The newly set value.</returns>
    /// <exception cref="InvalidOperationException">Thrown if this is a <c>default</c> entry.</exception>
    /// <remarks>
    /// <see cref="Value"/> is a snapshot and is not updated by this method.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TValue Insert(TValue newValue)
    {
        var dictionary = _lookup.Dictionary;
        if (dictionary is null)
        {
            DictionaryThrowHelper.ThrowDefaultEntry();
        }

        if (DictionaryAccess<TKey, TValue>.GetOccupiedStamp(dictionary) == _stamp && (int)_location >= 0)
        {
            DictionaryAccess<TKey, TValue>.WriteOccupiedValue(dictionary, _location, newValue);
            return newValue;
        }

        return InsertFallback(_lookup, _key, newValue);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static TValue InsertFallback(
        Dictionary<TKey, TValue>.AlternateLookup<TAlternateKey> lookup,
        TAlternateKey key,
        TValue newValue)
    {
        lookup[key] = newValue;
        return newValue;
    }

    /// <summary>
    /// Removes this entry from the dictionary and returns the value that was removed.
    /// </summary>
    /// <returns>
    /// The value that was actually removed from the dictionary, which reflects any changes made since the entry
    /// was obtained (for example via <see cref="Insert"/>), and may therefore differ from <see cref="Value"/>.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown if this is a <c>default</c> entry.</exception>
    /// <exception cref="KeyNotFoundException">Thrown if the key is no longer present in the dictionary.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TValue Remove()
    {
        var dictionary = _lookup.Dictionary;
        if (dictionary is null)
        {
            DictionaryThrowHelper.ThrowDefaultEntry();
        }

        if (DictionaryAccess<TKey, TValue>.GetOccupiedStamp(dictionary) == _stamp && (int)_location >= 0)
        {
            return DictionaryAccess<TKey, TValue>.RemoveOccupied(dictionary, _location);
        }

        return RemoveEntryFallback(_lookup, _key).Value;
    }

    /// <summary>
    /// Removes this entry from the dictionary and returns both the stored <typeparamref name="TKey"/> and the removed <typeparamref name="TValue"/>.
    /// </summary>
    /// <returns>
    /// A tuple containing the actual <typeparamref name="TKey"/> and <typeparamref name="TValue"/> removed from the dictionary,
    /// which reflects any changes made since the entry was obtained and may therefore differ from <see cref="Value"/>.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown if this is a <c>default</c> entry.</exception>
    /// <exception cref="KeyNotFoundException">Thrown if the key is no longer present in the dictionary.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (TKey Key, TValue Value) RemoveEntry()
    {
        var dictionary = _lookup.Dictionary;
        if (dictionary is null)
        {
            DictionaryThrowHelper.ThrowDefaultEntry();
        }

        if (DictionaryAccess<TKey, TValue>.GetOccupiedStamp(dictionary) == _stamp && (int)_location >= 0)
        {
            var value = DictionaryAccess<TKey, TValue>.RemoveOccupied(dictionary, _location, out var actualKey);
            return (actualKey, value);
        }

        return RemoveEntryFallback(_lookup, _key);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (TKey Key, TValue Value) RemoveEntryFallback(
        Dictionary<TKey, TValue>.AlternateLookup<TAlternateKey> lookup,
        TAlternateKey key)
    {
        if (!lookup.Remove(key, out var actualKey, out var value))
        {
            DictionaryAccess<TKey, TValue>.ThrowAlternateKeyNotFound(lookup.Dictionary, key);
        }

        return (actualKey, value);
    }
}
