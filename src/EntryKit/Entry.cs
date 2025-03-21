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
        _valueRef = ref CollectionsMarshal.GetValueRefOrNullRef(dictionary, key);
        _exists = !Unsafe.IsNullRef(ref _valueRef);
    }

    public TKey Key() => _key;
    public bool IsOccupied() => _exists;
    public bool IsVacant() => !_exists;

    public Entry<TKey, TValue> AndModify(Action<TValue> action)
    {
        if (!_exists) return this;

        var value = _valueRef;
        action(_valueRef);
        _dictionary[_key] = value;
        return this;
    }

    public Entry<TKey, TValue> AndModify(Func<TValue, TValue> updateFunc)
    {
        if (!_exists) return this;

        _dictionary[_key] = updateFunc(_valueRef);
        return this;
    }

    public ref TValue OrInsert(TValue defaultValue)
    {
        if (_exists) return ref _valueRef;

        _dictionary.Add(_key, defaultValue);
        _valueRef = ref CollectionsMarshal.GetValueRefOrNullRef(_dictionary, _key);
        _exists = true;
        return ref _valueRef;
    }

    public ref TValue OrInsertWith(Func<TValue> valueFactory)
    {
        if (_exists) return ref _valueRef;

        var value = valueFactory();
        _dictionary.Add(_key, value);
        _valueRef = ref CollectionsMarshal.GetValueRefOrNullRef(_dictionary, _key);
        _exists = true;
        return ref _valueRef;
    }

    public ref TValue OrInsertWithKey(Func<TKey, TValue> valueFactory)
    {
        if (_exists) return ref _valueRef;

        var value = valueFactory(_key);
        _dictionary.Add(_key, value);
        _valueRef = ref CollectionsMarshal.GetValueRefOrNullRef(_dictionary, _key);
        _exists = true;
        return ref _valueRef;
    }

    public ref TValue OrDefault()
    {
        if (_exists) return ref _valueRef;

        _dictionary.Add(_key, default!);
        _valueRef = ref CollectionsMarshal.GetValueRefOrNullRef(_dictionary, _key);
        _exists = true;
        return ref _valueRef;
    }

    // TODO: Insert

    public TValue Remove()
    {
        if (!_exists) ThrowKeyNotFound();

        var value = _valueRef;
        _dictionary.Remove(_key);
        return value;
    }

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

    public OccupiedEntry<TKey, TValue> ToOccupied()
    {
        if (!_exists) ThrowKeyNotFound();
        return new OccupiedEntry<TKey, TValue>(_dictionary, _key, _valueRef);
    }

    public VacantEntry<TKey, TValue> ToVacant()
    {
        if (_exists) throw new Exception("The entry is occupied.");
        return new VacantEntry<TKey, TValue>(_dictionary, _key);
    }

    public void Match(Action<OccupiedEntry<TKey, TValue>> occupiedAction,
        Action<VacantEntry<TKey, TValue>> vacantAction)
    {
        if (_exists)
        {
            occupiedAction(new OccupiedEntry<TKey, TValue>(_dictionary, _key, _valueRef));
        }
        else
        {
            vacantAction(new VacantEntry<TKey, TValue>(_dictionary, _key));
        }
    }

    public TResult Match<TResult>(Func<OccupiedEntry<TKey, TValue>, TResult> occupiedFunc,
        Func<VacantEntry<TKey, TValue>, TResult> vacantFunc)
    {
        return _exists
            ? occupiedFunc(new OccupiedEntry<TKey, TValue>(_dictionary, _key, _valueRef))
            : vacantFunc(new VacantEntry<TKey, TValue>(_dictionary, _key));
    }

    public bool TryGetOccupied(out OccupiedEntry<TKey, TValue> occupied)
    {
        if (_exists)
        {
            occupied = new OccupiedEntry<TKey, TValue>(_dictionary, _key, _valueRef);
            return true;
        }

        occupied = default;
        return false;
    }

    public bool TryGetVacant(out VacantEntry<TKey, TValue> vacant)
    {
        if (!_exists)
        {
            vacant = new VacantEntry<TKey, TValue>(_dictionary, _key);
            return true;
        }

        vacant = default;
        return false;
    }

    private static void ThrowKeyNotFound() => throw new KeyNotFoundException("The key does not exist in the dictionary.");
}
