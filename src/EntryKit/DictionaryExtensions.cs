namespace EntryKit;

public static class DictionaryExtensions
{
    public static Entry<TKey, TValue> Entry<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key)
        where TKey : notnull
    {
        return new Entry<TKey, TValue>(dictionary, key);
    }

    public static ref TValue GetOrInsert<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key,
        TValue defaultValue) where TKey : notnull
    {
        return ref dictionary.Entry(key).OrInsert(defaultValue);
    }
}
