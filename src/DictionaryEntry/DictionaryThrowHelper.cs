using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace DictionaryEntry;

internal static class DictionaryThrowHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfDefault([NotNull] object? dictionary)
    {
        if (dictionary is null)
        {
            ThrowDefaultEntry();
        }
    }

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowDefaultEntry() =>
        throw new InvalidOperationException(
            "The entry is not associated with a dictionary. Obtain entries with Dictionary.Entry(key) rather than default.");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowKeyNull() =>
        throw new ArgumentNullException("key");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowConcurrentOperations() =>
        throw new InvalidOperationException("Operations that change non-concurrent collections must have exclusive access.");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowVacantOrDefault<TKey, TValue>(Dictionary<TKey, TValue>? dictionary, TKey key)
        where TKey : notnull
    {
        ThrowIfDefault(dictionary);
        throw new KeyNotFoundException($"The key '{key}' was not found in the dictionary.");
    }

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowOccupiedOrDefault<TKey, TValue>(Dictionary<TKey, TValue>? dictionary, TKey key)
        where TKey : notnull
    {
        ThrowIfDefault(dictionary);
        throw new InvalidOperationException($"The key '{key}' already exists in the dictionary.");
    }

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowAlternateVacantOrDefault<TKey, TValue, TAlternateKey>(
        Dictionary<TKey, TValue>? dictionary,
        TAlternateKey key)
        where TKey : notnull
        where TAlternateKey : notnull, allows ref struct
    {
        ThrowIfDefault(dictionary);
        throw new KeyNotFoundException($"The key {FormatAlternateKey(dictionary, key)} was not found in the dictionary.");
    }

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowAlternateOccupiedOrDefault<TKey, TValue, TAlternateKey>(
        Dictionary<TKey, TValue>? dictionary,
        TAlternateKey key)
        where TKey : notnull
        where TAlternateKey : notnull, allows ref struct
    {
        ThrowIfDefault(dictionary);
        throw new InvalidOperationException($"The key {FormatAlternateKey(dictionary, key)} already exists in the dictionary.");
    }

    internal static string FormatAlternateKey<TKey, TValue, TAlternateKey>(
        Dictionary<TKey, TValue> dictionary,
        TAlternateKey key)
        where TKey : notnull
        where TAlternateKey : notnull, allows ref struct =>
        dictionary.Comparer is IAlternateEqualityComparer<TAlternateKey, TKey> alt ? $"'{alt.Create(key)}'" : "given";
}
