using System.Runtime.CompilerServices;

namespace DictionaryEntry;

/// <summary>
/// Provides extension methods for working with dictionaries using the entry API pattern.
/// </summary>
public static class DictionaryExtensions
{
    /// <summary>
    /// Gets an entry for the specified key in the dictionary, whether the key exists or not.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
    /// <param name="dictionary">The dictionary to operate on.</param>
    /// <param name="key">The key to look up in the dictionary.</param>
    /// <returns>An <see cref="Entry{TKey, TValue}"/> that can be used to manipulate the dictionary.</returns>
    /// <remarks>
    /// This method performs a single dictionary lookup and returns an entry that can be used
    /// for conditional operations without additional lookups.
    /// </remarks>
    /// <example>
    /// <code>
    /// var dict = new Dictionary&lt;string, int&gt;();
    /// dict.Entry("key").OrInsert(42);
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Entry<TKey, TValue> Entry<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key)
        where TKey : notnull
    {
        return new Entry<TKey, TValue>(dictionary, key);
    }

    /// <summary>
    /// Gets an entry for the specified alternate key in the dictionary lookup, whether the key exists or not.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
    /// <typeparam name="TAlternateKey">The type of the alternate key used for lookup (for example, <see cref="ReadOnlySpan{T}"/>).</typeparam>
    /// <param name="lookup">The alternate lookup obtained from <see cref="Dictionary{TKey, TValue}.GetAlternateLookup{TAlternateKey}"/>.</param>
    /// <param name="key">The alternate key to look up in the dictionary.</param>
    /// <returns>An <see cref="AlternateEntry{TKey, TValue, TAlternateKey}"/> that can be used to manipulate the dictionary without allocating <typeparamref name="TKey"/> on hits.</returns>
    /// <remarks>
    /// This method performs a single dictionary lookup using the alternate key and only materializes a
    /// <typeparamref name="TKey"/> instance if an insertion occurs on a missing key.
    /// </remarks>
    /// <example>
    /// <code>
    /// var dict = new Dictionary&lt;string, int&gt;(StringComparer.Ordinal);
    /// var lookup = dict.GetAlternateLookup&lt;ReadOnlySpan&lt;char&gt;&gt;();
    /// lookup.Entry("key".AsSpan()).OrInsert(42);
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AlternateEntry<TKey, TValue, TAlternateKey> Entry<TKey, TValue, TAlternateKey>(
        this Dictionary<TKey, TValue>.AlternateLookup<TAlternateKey> lookup,
        TAlternateKey key)
        where TKey : notnull
        where TAlternateKey : notnull, allows ref struct
    {
        return new AlternateEntry<TKey, TValue, TAlternateKey>(lookup, key);
    }

    /// <summary>
    /// Gets an entry for the specified <see cref="ReadOnlySpan{T}"/> of <see cref="char"/> key in a
    /// <see cref="string"/>-keyed dictionary, whether the key exists or not.
    /// </summary>
    /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
    /// <param name="dictionary">The <see cref="string"/>-keyed dictionary to operate on.</param>
    /// <param name="key">The character span key to look up in the dictionary.</param>
    /// <returns>
    /// An <see cref="AlternateEntry{TKey, TValue, TAlternateKey}"/> that can be used to manipulate the dictionary
    /// without allocating a <see cref="string"/> on hits.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <paramref name="dictionary"/> uses a custom <see cref="IEqualityComparer{T}"/> that does not implement
    /// <see cref="IAlternateEqualityComparer{TAlternate, T}"/> for <see cref="ReadOnlySpan{T}"/> of <see cref="char"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Marked with <see cref="OverloadResolutionPriorityAttribute"/> (<c>-1</c>) so passing a <see cref="string"/>
    /// (such as <c>dict.Entry("key")</c>) continues to resolve to <see cref="Entry{TKey, TValue}(Dictionary{TKey, TValue}, TKey)"/>
    /// rather than implicitly converting to <see cref="ReadOnlySpan{T}"/> under C# 14+ span conversion rules.
    /// </para>
    /// <para>
    /// In tight loops, caching <see cref="Dictionary{TKey, TValue}.GetAlternateLookup{TAlternateKey}"/> outside the loop
    /// and calling <see cref="Entry{TKey, TValue, TAlternateKey}"/> avoids repeated comparer compatibility checks.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var dict = new Dictionary&lt;string, int&gt;(StringComparer.Ordinal);
    /// ReadOnlySpan&lt;char&gt; key = "key=42".AsSpan(0, 3);
    /// dict.Entry(key).OrInsert(42);
    /// </code>
    /// </example>
    [OverloadResolutionPriority(-1)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AlternateEntry<string, TValue, ReadOnlySpan<char>> Entry<TValue>(
        this Dictionary<string, TValue> dictionary,
        ReadOnlySpan<char> key)
    {
        return new AlternateEntry<string, TValue, ReadOnlySpan<char>>(
            dictionary.GetAlternateLookup<ReadOnlySpan<char>>(),
            key);
    }
}
