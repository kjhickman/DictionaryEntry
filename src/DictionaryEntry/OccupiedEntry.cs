using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DictionaryEntry;

/// <summary>
/// Represents an entry that exists in the dictionary.
/// Provides members for inspecting and manipulating an existing dictionary entry.
/// </summary>
/// <typeparam name="TKey">The type of the key in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of the value in the dictionary.</typeparam>
public readonly struct OccupiedEntry<TKey, TValue> where TKey : notnull
{
    private readonly Dictionary<TKey, TValue> _dictionary;
    private readonly TKey _key;
    private readonly TValue _value;

    internal Dictionary<TKey, TValue> Dictionary
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _dictionary;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal OccupiedEntry(Dictionary<TKey, TValue> dictionary, TKey key, TValue value)
    {
        _dictionary = dictionary;
        _key = key;
        _value = value;
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
    /// Deconstructs this entry into its key and value.
    /// </summary>
    /// <param name="key">The key associated with this entry.</param>
    /// <param name="value">The value associated with this entry.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deconstruct(out TKey key, out TValue value)
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
        if (_dictionary is null)
        {
            ThrowDefaultEntry();
        }

        ref var valueRef = ref CollectionsMarshal.GetValueRefOrNullRef(_dictionary, _key);
        if (!Unsafe.IsNullRef(ref valueRef))
        {
            valueRef = newValue;
            return newValue;
        }

        return InsertFallback(_dictionary, _key, newValue);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static TValue InsertFallback(Dictionary<TKey, TValue> dictionary, TKey key, TValue newValue)
    {
        dictionary[key] = newValue;
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
        if (_dictionary is null)
        {
            ThrowDefaultEntry();
        }

        if (!_dictionary.Remove(_key, out var value))
        {
            ThrowKeyNotFound(_key);
        }

        return value;
    }

    /// <summary>
    /// Removes this entry from the dictionary and returns both the key and the value.
    /// </summary>
    /// <returns>
    /// A tuple containing the key and the value that was actually removed from the dictionary, which reflects any
    /// changes made since the entry was obtained and may therefore differ from <see cref="Value"/>.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown if this is a <c>default</c> entry.</exception>
    /// <exception cref="KeyNotFoundException">Thrown if the key is no longer present in the dictionary.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public (TKey, TValue) RemoveEntry()
    {
        if (_dictionary is null)
        {
            ThrowDefaultEntry();
        }

        if (!_dictionary.Remove(_key, out var value))
        {
            ThrowKeyNotFound(_key);
        }

        return (_key, value);
    }

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowDefaultEntry() =>
        throw new InvalidOperationException(
            "The occupied entry is not associated with a dictionary. Obtain entries with Dictionary.Entry(key) rather than default.");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowKeyNotFound(TKey key) =>
        throw new KeyNotFoundException($"The key '{key}' is no longer present in the dictionary.");
}
