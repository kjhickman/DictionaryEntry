using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace EntryKit;

public ref struct Entry<TKey, TValue> where TKey : notnull
{
    private readonly Dictionary<TKey, TValue> _dictionary;
    private readonly TKey _key;
    private bool _exists;
    private ref TValue _valueRef;

    internal Entry(Dictionary<TKey, TValue> dictionary, TKey key)
    {
        _dictionary = dictionary;
        _key = key;

        // Check if the key exists and get a reference to the value
        _valueRef = ref CollectionsMarshal.GetValueRefOrNullRef(dictionary, key);
        _exists = !Unsafe.IsNullRef(ref _valueRef);
    }

    // Check if the key exists
    public bool Exists => _exists;

    // Get the key
    public TKey Key => _key;

    // Apply an action to the value if the key exists
    public Entry<TKey, TValue> AndModify(Action<TValue> action)
    {
        if (!_exists) return this;

        // Create a local variable that gets updated by the action
        var value = _valueRef;
        action(value);
        // Assign the modified value back to the dictionary
        _dictionary[_key] = value;
        return this;
    }

    public Entry<TKey, TValue> AndModify(Func<TValue, TValue> updateFunc)
    {
        if (!_exists) return this;

        // Calculate new value based on current value
        var newValue = updateFunc(_valueRef);

        // Store new value directly
        _dictionary[_key] = newValue;
        return this;
    }

    // Get or insert a value
    public ref TValue OrInsert(TValue defaultValue)
    {
        if (!_exists)
        {
            _dictionary.Add(_key, defaultValue);
            _valueRef = ref CollectionsMarshal.GetValueRefOrNullRef(_dictionary, _key);
            _exists = true;
        }

        return ref _valueRef;
    }

    // Get or insert a value using a factory
    public ref TValue OrInsertWith(Func<TValue> valueFactory)
    {
        if (!_exists)
        {
            var value = valueFactory();
            _dictionary.Add(_key, value);
            _valueRef = ref CollectionsMarshal.GetValueRefOrNullRef(_dictionary, _key);
            _exists = true;
        }

        return ref _valueRef;
    }

    // Get or insert a value using the key and a factory
    public ref TValue OrInsertWithKey(Func<TKey, TValue> valueFactory)
    {
        if (!_exists)
        {
            var value = valueFactory(_key);
            _dictionary.Add(_key, value);
            _valueRef = ref CollectionsMarshal.GetValueRefOrNullRef(_dictionary, _key);
            _exists = true;
        }

        return ref _valueRef;
    }

    // Get the value or throw if the key doesn't exist
    public ref TValue ValueOrThrow()
    {
        if (!_exists) ThrowKeyNotFound();

        return ref _valueRef;
    }

    // Remove the entry and return the value
    public TValue Remove()
    {
        if (!_exists) ThrowKeyNotFound();

        var value = _valueRef;
        _dictionary.Remove(_key);
        return value;
    }

    // Try to remove the entry
    public bool TryRemove(out TValue value)
    {
        if (!_exists)
        {
            value = default!;
            return false;
        }

        value = _valueRef;
        return _dictionary.Remove(_key);
    }

    private static void ThrowKeyNotFound() => throw new KeyNotFoundException("The key does not exist in the dictionary.");
}
