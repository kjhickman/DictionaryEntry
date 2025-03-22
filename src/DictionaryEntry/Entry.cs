using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DictionaryEntry;

/// <summary>
/// Represents a dictionary entry that may or may not exist within the dictionary.
/// Provides a unified API for dictionary operations without duplicating key lookups.
/// </summary>
/// <typeparam name="TKey">The type of the key in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of the value in the dictionary.</typeparam>
/// <remarks>
/// The struct is marked as 'ref' to prevent copying of potentially large dictionaries.
/// </remarks>
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

    /// <summary>
    /// Gets the key associated with this entry.
    /// </summary>
    /// <returns>The key associated with this entry.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TKey Key() => _key;

    /// <summary>
    /// Determines whether the entry exists in the dictionary.
    /// </summary>
    /// <returns><c>true</c> if the entry exists in the dictionary; otherwise, <c>false</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsOccupied() => _exists;

    /// <summary>
    /// Determines whether the entry does not exist in the dictionary.
    /// </summary>
    /// <returns><c>true</c> if the entry does not exist in the dictionary; otherwise, <c>false</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsVacant() => !_exists;

    /// <summary>
    /// Applies a modification function to the value if the entry exists in the dictionary.
    /// </summary>
    /// <param name="updateFunc">A function that takes the current value and returns the new value.</param>
    /// <returns>The entry instance to allow for method chaining.</returns>
    /// <remarks>
    /// If the entry does not exist, this method does nothing and returns the unchanged entry.
    /// </remarks>
    public Entry<TKey, TValue> AndModify(Func<TValue, TValue> updateFunc)
    {
        if (!_exists)
        {
            return this;
        }

        _dictionary[_key] = updateFunc(_valueRef);
        return this;
    }

    /// <summary>
    /// Gets a reference to the value in the dictionary if it exists, or inserts the specified value if it doesn't.
    /// </summary>
    /// <param name="defaultValue">The value to insert if the entry does not exist.</param>
    /// <returns>A reference to the value in the dictionary.</returns>
    public ref TValue OrInsert(TValue defaultValue)
    {
        if (_exists)
        {
            return ref _valueRef;
        }

        _dictionary.Add(_key, defaultValue);
        _valueRef = ref CollectionsMarshal.GetValueRefOrNullRef(_dictionary, _key);
        _exists = true;
        return ref _valueRef;
    }

    /// <summary>
    /// Gets a reference to the value in the dictionary if it exists, or computes and inserts a new value if it doesn't.
    /// </summary>
    /// <param name="valueFactory">A function that computes the value to insert if the entry does not exist.</param>
    /// <returns>A reference to the value in the dictionary.</returns>
    public ref TValue OrInsertWith(Func<TValue> valueFactory)
    {
        if (_exists)
        {
            return ref _valueRef;
        }

        var value = valueFactory();
        _dictionary.Add(_key, value);
        _valueRef = ref CollectionsMarshal.GetValueRefOrNullRef(_dictionary, _key);
        _exists = true;
        return ref _valueRef;
    }

    /// <summary>
    /// Gets a reference to the value in the dictionary if it exists, or computes and inserts a new value based on the key if it doesn't.
    /// </summary>
    /// <param name="valueFactory">A function that takes the key and computes the value to insert if the entry does not exist.</param>
    /// <returns>A reference to the value in the dictionary.</returns>
    public ref TValue OrInsertWithKey(Func<TKey, TValue> valueFactory)
    {
        if (_exists)
        {
            return ref _valueRef;
        }

        var value = valueFactory(_key);
        _dictionary.Add(_key, value);
        _valueRef = ref CollectionsMarshal.GetValueRefOrNullRef(_dictionary, _key);
        _exists = true;
        return ref _valueRef;
    }

    /// <summary>
    /// Gets a reference to the value in the dictionary if it exists, or inserts the default value for the type if it doesn't.
    /// </summary>
    /// <returns>A reference to the value in the dictionary.</returns>
    public ref TValue OrDefault()
    {
        if (_exists)
        {
            return ref _valueRef;
        }

        _dictionary.Add(_key, default!);
        _valueRef = ref CollectionsMarshal.GetValueRefOrNullRef(_dictionary, _key);
        _exists = true;
        return ref _valueRef;
    }

    /// <summary>
    /// Creates an occupied entry with the specified value. If the entry already exists, the value is updated.
    /// </summary>
    /// <param name="value">The value to insert or update.</param>
    /// <returns>An <see cref="OccupiedEntry{TKey, TValue}"/> representing the entry in the dictionary.</returns>
    public OccupiedEntry<TKey, TValue> InsertEntry(TValue value)
    {
        if (_exists)
        {
            _dictionary[_key] = value;
            return new OccupiedEntry<TKey, TValue>(_dictionary, _key, value);
        }

        _dictionary.Add(_key, value);
        _valueRef = ref CollectionsMarshal.GetValueRefOrNullRef(_dictionary, _key);
        _exists = true;
        return new OccupiedEntry<TKey, TValue>(_dictionary, _key, _valueRef);
    }

    /// <summary>
    /// Converts the entry to an occupied entry if it exists in the dictionary.
    /// </summary>
    /// <returns>An <see cref="OccupiedEntry{TKey, TValue}"/> representing the entry in the dictionary.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if the entry does not exist in the dictionary.</exception>
    public OccupiedEntry<TKey, TValue> ToOccupied()
    {
        if (!_exists)
        {
            throw new KeyNotFoundException($"The key '{_key}' was not found in the dictionary.");
        }

        return new OccupiedEntry<TKey, TValue>(_dictionary, _key, _valueRef);
    }

    /// <summary>
    /// Converts the entry to a vacant entry if it does not exist in the dictionary.
    /// </summary>
    /// <returns>A <see cref="VacantEntry{TKey, TValue}"/> representing the entry not in the dictionary.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the entry exists in the dictionary.</exception>
    public VacantEntry<TKey, TValue> ToVacant()
    {
        if (_exists)
        {
            throw new InvalidOperationException($"The key '{_key}' already exists in the dictionary.");
        }

        return new VacantEntry<TKey, TValue>(_dictionary, _key);
    }

    /// <summary>
    /// Performs pattern matching on the entry, executing different actions based on whether the entry exists.
    /// </summary>
    /// <param name="occupiedAction">The action to execute if the entry exists.</param>
    /// <param name="vacantAction">The action to execute if the entry does not exist.</param>
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

    /// <summary>
    /// Performs pattern matching on the entry, executing different functions based on whether the entry exists and returning a result.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="occupiedFunc">The function to execute if the entry exists.</param>
    /// <param name="vacantFunc">The function to execute if the entry does not exist.</param>
    /// <returns>The result of the executed function.</returns>
    public TResult Match<TResult>(Func<OccupiedEntry<TKey, TValue>, TResult> occupiedFunc,
        Func<VacantEntry<TKey, TValue>, TResult> vacantFunc)
    {
        if (_exists)
        {
            return occupiedFunc(new OccupiedEntry<TKey, TValue>(_dictionary, _key, _valueRef));
        }

        return vacantFunc(new VacantEntry<TKey, TValue>(_dictionary, _key));
    }

    /// <summary>
    /// Tries to get an occupied entry if the key exists in the dictionary.
    /// </summary>
    /// <param name="occupied">When this method returns, contains the occupied entry if the key was found; otherwise, the default value.</param>
    /// <returns><c>true</c> if the key was found; otherwise, <c>false</c>.</returns>
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

    /// <summary>
    /// Tries to get a vacant entry if the key does not exist in the dictionary.
    /// </summary>
    /// <param name="vacant">When this method returns, contains the vacant entry if the key was not found; otherwise, the default value.</param>
    /// <returns><c>true</c> if the key was not found; otherwise, <c>false</c>.</returns>
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
}
