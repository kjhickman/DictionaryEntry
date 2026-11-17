using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace DictionaryEntry;

[StackTraceHidden]
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
    public static void ThrowDefaultEntry() =>
        throw CreateDefaultEntryException();

    [DoesNotReturn]
    public static void ThrowKeyNull() =>
        throw new ArgumentNullException("key");

    [DoesNotReturn]
    public static void ThrowConcurrentOperations() =>
        throw new InvalidOperationException("Operations that change non-concurrent collections must have exclusive access.");

    [DoesNotReturn]
    public static void ThrowVacantOrDefault<TKey>(object? dictionary, TKey key) =>
        throw CreateVacantOrDefaultException(dictionary, key);

    [DoesNotReturn]
    public static void ThrowOccupiedOrDefault<TKey>(object? dictionary, TKey key) =>
        throw CreateOccupiedOrDefaultException(dictionary, key);

    [DoesNotReturn]
    public static void ThrowAlternateVacantOrDefault<TKey, TValue, TAlternateKey>(
        Dictionary<TKey, TValue>? dictionary,
        TAlternateKey key)
        where TKey : notnull
        where TAlternateKey : notnull, allows ref struct =>
        throw CreateAlternateVacantOrDefaultException(dictionary, key);

    [DoesNotReturn]
    public static void ThrowAlternateOccupiedOrDefault<TKey, TValue, TAlternateKey>(
        Dictionary<TKey, TValue>? dictionary,
        TAlternateKey key)
        where TKey : notnull
        where TAlternateKey : notnull, allows ref struct =>
        throw CreateAlternateOccupiedOrDefaultException(dictionary, key);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Exception CreateVacantOrDefaultException<TKey>(object? dictionary, TKey key) =>
        dictionary is null
            ? CreateDefaultEntryException()
            : new KeyNotFoundException($"The key '{key}' was not found in the dictionary.");

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static InvalidOperationException CreateOccupiedOrDefaultException<TKey>(object? dictionary, TKey key) =>
        dictionary is null
            ? CreateDefaultEntryException()
            : new InvalidOperationException($"The key '{key}' already exists in the dictionary.");

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Exception CreateAlternateVacantOrDefaultException<TKey, TValue, TAlternateKey>(
        Dictionary<TKey, TValue>? dictionary,
        TAlternateKey key)
        where TKey : notnull
        where TAlternateKey : notnull, allows ref struct =>
        dictionary is null
            ? CreateDefaultEntryException()
            : new KeyNotFoundException($"The key {FormatAlternateKey(dictionary, key)} was not found in the dictionary.");

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static InvalidOperationException CreateAlternateOccupiedOrDefaultException<TKey, TValue, TAlternateKey>(
        Dictionary<TKey, TValue>? dictionary,
        TAlternateKey key)
        where TKey : notnull
        where TAlternateKey : notnull, allows ref struct =>
        dictionary is null
            ? CreateDefaultEntryException()
            : new InvalidOperationException($"The key {FormatAlternateKey(dictionary, key)} already exists in the dictionary.");

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static InvalidOperationException CreateDefaultEntryException() =>
        new("The entry is not associated with a dictionary. Obtain entries with Dictionary.Entry(key) rather than default.");

    internal static string FormatAlternateKey<TKey, TValue, TAlternateKey>(
        Dictionary<TKey, TValue> dictionary,
        TAlternateKey key)
        where TKey : notnull
        where TAlternateKey : notnull, allows ref struct =>
        dictionary.Comparer is IAlternateEqualityComparer<TAlternateKey, TKey> alt ? $"'{alt.Create(key)}'" : "given";
}
