using System.Runtime.CompilerServices;

namespace DictionaryEntry;

/// <summary>
/// Provides an extension method for working with dictionaries using the entry API pattern.
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
}
