#if NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal AlternateOccupiedEntry(
        Dictionary<TKey, TValue>.AlternateLookup<TAlternateKey> lookup,
        TAlternateKey key,
        TValue value)
    {
        _lookup = lookup;
        _key = key;
        _value = value;
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
        if (_lookup.Dictionary is null)
        {
            ThrowDefaultEntry();
        }

        ref var valueRef = ref CollectionsMarshal.GetValueRefOrNullRef(_lookup, _key);
        if (!Unsafe.IsNullRef(ref valueRef))
        {
            valueRef = newValue;
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
        if (_lookup.Dictionary is null)
        {
            ThrowDefaultEntry();
        }

        if (!_lookup.Remove(_key, out _, out var value))
        {
            ThrowKeyNotFound(_lookup.Dictionary, _key);
        }

        return value;
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
        if (_lookup.Dictionary is null)
        {
            ThrowDefaultEntry();
        }

        if (!_lookup.Remove(_key, out var actualKey, out var value))
        {
            ThrowKeyNotFound(_lookup.Dictionary, _key);
        }

        return (actualKey, value);
    }

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowDefaultEntry() =>
        throw new InvalidOperationException(
            "The occupied entry is not associated with a dictionary. Obtain entries with AlternateLookup.Entry(key) rather than default.");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowKeyNotFound(Dictionary<TKey, TValue> dictionary, TAlternateKey key)
    {
        if (dictionary.Comparer is IAlternateEqualityComparer<TAlternateKey, TKey> altComparer)
        {
            throw new KeyNotFoundException($"The key '{altComparer.Create(key)}' is no longer present in the dictionary.");
        }

        throw new KeyNotFoundException("The given key is no longer present in the dictionary.");
    }
}
#endif
