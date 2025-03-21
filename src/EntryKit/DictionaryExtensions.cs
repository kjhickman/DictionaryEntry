namespace EntryKit;

public static class DictionaryExtensions
{
    public static Entry<TKey, TValue> Entry<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key)
        where TKey : notnull
    {
        return new Entry<TKey, TValue>(dictionary, key);
    }
}
