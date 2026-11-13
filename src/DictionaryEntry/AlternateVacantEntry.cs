#if NET9_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace DictionaryEntry;

/// <summary>
/// Represents an entry looked up via an alternate key that does not exist in the dictionary.
/// Provides members for inserting values into the dictionary, materializing a <typeparamref name="TKey"/> only upon insertion.
/// </summary>
/// <typeparam name="TKey">The type of the key stored in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of the value stored in the dictionary.</typeparam>
/// <typeparam name="TAlternateKey">The type of the alternate key used for lookup.</typeparam>
public readonly ref struct AlternateVacantEntry<TKey, TValue, TAlternateKey>
    where TKey : notnull
    where TAlternateKey : notnull, allows ref struct
{
    private readonly Dictionary<TKey, TValue>.AlternateLookup<TAlternateKey> _lookup;
    private readonly TAlternateKey _key;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal AlternateVacantEntry(Dictionary<TKey, TValue>.AlternateLookup<TAlternateKey> lookup, TAlternateKey key)
    {
        _lookup = lookup;
        _key = key;
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
    /// Inserts a value into the dictionary using the alternate key associated with this entry.
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
        if (_lookup.Dictionary is null)
        {
            ThrowDefaultEntry();
        }

        _lookup[_key] = value;
        return value;
    }

    /// <summary>
    /// Inserts a value into the dictionary and returns an occupied entry for the newly inserted entry.
    /// </summary>
    /// <param name="value">The value to insert.</param>
    /// <returns>An <see cref="AlternateOccupiedEntry{TKey, TValue, TAlternateKey}"/> representing the newly inserted entry.</returns>
    /// <exception cref="InvalidOperationException">Thrown if this is a <c>default</c> entry.</exception>
    /// <remarks>
    /// If the key exists, the existing value will be overwritten.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AlternateOccupiedEntry<TKey, TValue, TAlternateKey> InsertEntry(TValue value)
    {
        if (_lookup.Dictionary is null)
        {
            ThrowDefaultEntry();
        }

        _lookup[_key] = value;
        return new AlternateOccupiedEntry<TKey, TValue, TAlternateKey>(_lookup, _key, value);
    }

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowDefaultEntry() =>
        throw new InvalidOperationException(
            "The vacant entry is not associated with a dictionary. Obtain entries with AlternateLookup.Entry(key) rather than default.");
}
#endif
