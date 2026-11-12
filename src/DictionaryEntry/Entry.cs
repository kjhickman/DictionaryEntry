using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DictionaryEntry;

/// <summary>
/// Represents a dictionary entry that may or may not exist within the dictionary.
/// Provides a unified API and C# union support (<see cref="OccupiedEntry{TKey, TValue}"/> or <see cref="VacantEntry{TKey, TValue}"/>)
/// for dictionary operations without duplicating key lookups.
/// </summary>
/// <typeparam name="TKey">The type of the key in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of the value in the dictionary.</typeparam>
[Union]
public ref struct Entry<TKey, TValue> : Entry<TKey, TValue>.IUnionMembers
    where TKey : notnull
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
    public TKey Key
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _key;
    }

    /// <summary>
    /// Gets a value indicating whether the entry exists in the dictionary.
    /// </summary>
    public bool IsOccupied
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _exists;
    }

    /// <summary>
    /// Gets a value indicating whether the entry does not exist in the dictionary.
    /// </summary>
    /// <remarks>
    /// A <c>default</c> entry is neither occupied nor vacant.
    /// </remarks>
    public bool IsVacant
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => !_exists && _dictionary is not null;
    }

    /// <summary>
    /// Applies a modification function to the value if the entry exists in the dictionary.
    /// </summary>
    /// <param name="updateFunc">A function that takes the current value and returns the new value. This function is only called if the entry exists.</param>
    /// <returns>The entry instance to allow for method chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown if this is a <c>default</c> entry.</exception>
    /// <remarks>
    /// If the entry does not exist, this method does nothing and returns the unchanged entry.
    /// The update function is applied atomically to the dictionary entry.
    /// </remarks>
    public Entry<TKey, TValue> AndModify(Func<TValue, TValue> updateFunc)
    {
        if (!_exists)
        {
            if (_dictionary is null)
            {
                ThrowDefaultEntry();
            }

            return this;
        }

        var newValue = updateFunc(_valueRef);

        // Re-acquire the ref rather than writing through the indexer: updateFunc may have mutated the
        // dictionary (e.g. triggered a resize), which would leave _valueRef pointing at stale storage
        // for chained calls like .OrInsert(). This costs the same single lookup as the indexer set,
        // and still re-adds the key if updateFunc removed it.
        _valueRef = ref CollectionsMarshal.GetValueRefOrAddDefault(_dictionary, _key, out _)!;
        _valueRef = newValue;
        return this;
    }

    /// <summary>
    /// Applies a modification function using the provided state to the value if the entry exists in the dictionary,
    /// avoiding closure allocations when paired with a <c>static</c> lambda.
    /// </summary>
    /// <typeparam name="TState">The type of the state value passed to <paramref name="updateFunc"/>.</typeparam>
    /// <param name="state">State passed through to <paramref name="updateFunc"/>.</param>
    /// <param name="updateFunc">A function that takes the current value and <paramref name="state"/> and returns the new value. This function is only called if the entry exists.</param>
    /// <returns>The entry instance to allow for method chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown if this is a <c>default</c> entry.</exception>
    public Entry<TKey, TValue> AndModify<TState>(TState state, Func<TValue, TState, TValue> updateFunc)
#if NET9_0_OR_GREATER
        where TState : allows ref struct
#endif
    {
        if (!_exists)
        {
            if (_dictionary is null)
            {
                ThrowDefaultEntry();
            }

            return this;
        }

        var newValue = updateFunc(_valueRef, state);

        _valueRef = ref CollectionsMarshal.GetValueRefOrAddDefault(_dictionary, _key, out _)!;
        _valueRef = newValue;
        return this;
    }

    /// <summary>
    /// Gets a reference to the value in the dictionary if it exists, or inserts the specified value if it doesn't.
    /// </summary>
    /// <param name="defaultValue">The value to insert if the entry does not exist.</param>
    /// <returns>A reference to the value in the dictionary.</returns>
    /// <exception cref="InvalidOperationException">Thrown if this is a <c>default</c> entry.</exception>
    public ref TValue OrInsert(TValue defaultValue)
    {
        if (_exists)
        {
            return ref _valueRef;
        }

        if (_dictionary is null)
        {
            ThrowDefaultEntry();
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
    /// <exception cref="InvalidOperationException">Thrown if this is a <c>default</c> entry.</exception>
    public ref TValue OrInsertWith(Func<TValue> valueFactory)
    {
        if (_exists)
        {
            return ref _valueRef;
        }

        if (_dictionary is null)
        {
            ThrowDefaultEntry();
        }

        var value = valueFactory();
        _dictionary.Add(_key, value);
        _valueRef = ref CollectionsMarshal.GetValueRefOrNullRef(_dictionary, _key);
        _exists = true;
        return ref _valueRef;
    }

    /// <summary>
    /// Gets a reference to the value in the dictionary if it exists, or computes and inserts a new value using the provided state if it doesn't,
    /// avoiding closure allocations when paired with a <c>static</c> lambda.
    /// </summary>
    /// <typeparam name="TState">The type of the state value passed to <paramref name="valueFactory"/>.</typeparam>
    /// <param name="state">State passed through to <paramref name="valueFactory"/>.</param>
    /// <param name="valueFactory">A function that takes <paramref name="state"/> and computes the value to insert if the entry does not exist.</param>
    /// <returns>A reference to the value in the dictionary.</returns>
    /// <exception cref="InvalidOperationException">Thrown if this is a <c>default</c> entry.</exception>
    public ref TValue OrInsertWith<TState>(TState state, Func<TState, TValue> valueFactory)
#if NET9_0_OR_GREATER
        where TState : allows ref struct
#endif
    {
        if (_exists)
        {
            return ref _valueRef;
        }

        if (_dictionary is null)
        {
            ThrowDefaultEntry();
        }

        var value = valueFactory(state);
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
    /// <exception cref="InvalidOperationException">Thrown if this is a <c>default</c> entry.</exception>
    public ref TValue OrInsertWithKey(Func<TKey, TValue> valueFactory)
    {
        if (_exists)
        {
            return ref _valueRef;
        }

        if (_dictionary is null)
        {
            ThrowDefaultEntry();
        }

        var value = valueFactory(_key);
        _dictionary.Add(_key, value);
        _valueRef = ref CollectionsMarshal.GetValueRefOrNullRef(_dictionary, _key);
        _exists = true;
        return ref _valueRef;
    }

    /// <summary>
    /// Gets a reference to the value in the dictionary if it exists, or computes and inserts a new value based on the key and provided state if it doesn't,
    /// avoiding closure allocations when paired with a <c>static</c> lambda.
    /// </summary>
    /// <typeparam name="TState">The type of the state value passed to <paramref name="valueFactory"/>.</typeparam>
    /// <param name="state">State passed through to <paramref name="valueFactory"/>.</param>
    /// <param name="valueFactory">A function that takes the key and <paramref name="state"/> and computes the value to insert if the entry does not exist.</param>
    /// <returns>A reference to the value in the dictionary.</returns>
    /// <exception cref="InvalidOperationException">Thrown if this is a <c>default</c> entry.</exception>
    public ref TValue OrInsertWithKey<TState>(TState state, Func<TKey, TState, TValue> valueFactory)
#if NET9_0_OR_GREATER
        where TState : allows ref struct
#endif
    {
        if (_exists)
        {
            return ref _valueRef;
        }

        if (_dictionary is null)
        {
            ThrowDefaultEntry();
        }

        var value = valueFactory(_key, state);
        _dictionary.Add(_key, value);
        _valueRef = ref CollectionsMarshal.GetValueRefOrNullRef(_dictionary, _key);
        _exists = true;
        return ref _valueRef;
    }

    /// <summary>
    /// Gets a reference to the value in the dictionary if it exists, or inserts the default value for the type if it doesn't.
    /// </summary>
    /// <returns>A reference to the value in the dictionary.</returns>
    /// <exception cref="InvalidOperationException">Thrown if this is a <c>default</c> entry.</exception>
    /// <remarks>
    /// For reference types, the default value is null. For value types, the default value is the zero-initialized value.
    /// </remarks>
    public ref TValue OrDefault()
    {
        if (_exists)
        {
            return ref _valueRef;
        }

        if (_dictionary is null)
        {
            ThrowDefaultEntry();
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
    /// <exception cref="InvalidOperationException">Thrown if this is a <c>default</c> entry.</exception>
    /// <remarks>
    /// This method always results in an occupied entry, regardless of whether the key previously existed.
    /// </remarks>
    public OccupiedEntry<TKey, TValue> InsertEntry(TValue value)
    {
        if (_exists)
        {
            _dictionary[_key] = value;
            return new OccupiedEntry<TKey, TValue>(_dictionary, _key, value);
        }

        if (_dictionary is null)
        {
            ThrowDefaultEntry();
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
    /// <exception cref="InvalidOperationException">Thrown if this is a <c>default</c> entry.</exception>
    /// <exception cref="KeyNotFoundException">Thrown if the entry does not exist in the dictionary.</exception>
    public OccupiedEntry<TKey, TValue> ToOccupied()
    {
        if (!_exists)
        {
            if (_dictionary is null)
            {
                ThrowDefaultEntry();
            }

            ThrowKeyNotFound(_key);
        }

        return new OccupiedEntry<TKey, TValue>(_dictionary, _key, _valueRef);
    }

    /// <summary>
    /// Converts the entry to a vacant entry if it does not exist in the dictionary.
    /// </summary>
    /// <returns>A <see cref="VacantEntry{TKey, TValue}"/> representing the entry not in the dictionary.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the entry exists in the dictionary, or if this is a <c>default</c> entry.</exception>
    public VacantEntry<TKey, TValue> ToVacant()
    {
        if (_exists)
        {
            ThrowKeyAlreadyExists(_key);
        }

        if (_dictionary is null)
        {
            ThrowDefaultEntry();
        }

        return new VacantEntry<TKey, TValue>(_dictionary, _key);
    }

    /// <summary>
    /// Performs pattern matching on the entry, executing different actions based on whether the entry exists.
    /// </summary>
    /// <param name="occupiedAction">The action to execute if the entry exists.</param>
    /// <param name="vacantAction">The action to execute if the entry does not exist.</param>
    /// <exception cref="InvalidOperationException">Thrown if this is a <c>default</c> entry.</exception>
    public void Match(Action<OccupiedEntry<TKey, TValue>> occupiedAction,
        Action<VacantEntry<TKey, TValue>> vacantAction)
    {
        if (_exists)
        {
            occupiedAction(new OccupiedEntry<TKey, TValue>(_dictionary, _key, _valueRef));
        }
        else
        {
            if (_dictionary is null)
            {
                ThrowDefaultEntry();
            }

            vacantAction(new VacantEntry<TKey, TValue>(_dictionary, _key));
        }
    }

    /// <summary>
    /// Performs pattern matching on the entry using the provided state, executing different actions based on whether the entry exists
    /// and avoiding closure allocations when paired with <c>static</c> lambdas.
    /// </summary>
    /// <typeparam name="TState">The type of the state value passed to the branch actions.</typeparam>
    /// <param name="state">State passed through to <paramref name="occupiedAction"/> or <paramref name="vacantAction"/>.</param>
    /// <param name="occupiedAction">The action to execute if the entry exists.</param>
    /// <param name="vacantAction">The action to execute if the entry does not exist.</param>
    /// <exception cref="InvalidOperationException">Thrown if this is a <c>default</c> entry.</exception>
    public void Match<TState>(
        TState state,
        Action<OccupiedEntry<TKey, TValue>, TState> occupiedAction,
        Action<VacantEntry<TKey, TValue>, TState> vacantAction)
#if NET9_0_OR_GREATER
        where TState : allows ref struct
#endif
    {
        if (_exists)
        {
            occupiedAction(new OccupiedEntry<TKey, TValue>(_dictionary, _key, _valueRef), state);
        }
        else
        {
            if (_dictionary is null)
            {
                ThrowDefaultEntry();
            }

            vacantAction(new VacantEntry<TKey, TValue>(_dictionary, _key), state);
        }
    }

    /// <summary>
    /// Performs pattern matching on the entry, executing different functions based on whether the entry exists and returning a result.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="occupiedFunc">The function to execute if the entry exists.</param>
    /// <param name="vacantFunc">The function to execute if the entry does not exist.</param>
    /// <returns>The result of the executed function.</returns>
    /// <exception cref="InvalidOperationException">Thrown if this is a <c>default</c> entry.</exception>
    public TResult Match<TResult>(Func<OccupiedEntry<TKey, TValue>, TResult> occupiedFunc,
        Func<VacantEntry<TKey, TValue>, TResult> vacantFunc)
    {
        if (_exists)
        {
            return occupiedFunc(new OccupiedEntry<TKey, TValue>(_dictionary, _key, _valueRef));
        }

        if (_dictionary is null)
        {
            ThrowDefaultEntry();
        }

        return vacantFunc(new VacantEntry<TKey, TValue>(_dictionary, _key));
    }

    /// <summary>
    /// Performs pattern matching on the entry using the provided state, executing different functions based on whether the entry exists,
    /// returning a result, and avoiding closure allocations when paired with <c>static</c> lambdas.
    /// </summary>
    /// <typeparam name="TState">The type of the state value passed to the branch functions.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="state">State passed through to <paramref name="occupiedFunc"/> or <paramref name="vacantFunc"/>.</param>
    /// <param name="occupiedFunc">The function to execute if the entry exists.</param>
    /// <param name="vacantFunc">The function to execute if the entry does not exist.</param>
    /// <returns>The result of the executed function.</returns>
    /// <exception cref="InvalidOperationException">Thrown if this is a <c>default</c> entry.</exception>
    public TResult Match<TState, TResult>(
        TState state,
        Func<OccupiedEntry<TKey, TValue>, TState, TResult> occupiedFunc,
        Func<VacantEntry<TKey, TValue>, TState, TResult> vacantFunc)
#if NET9_0_OR_GREATER
        where TState : allows ref struct
#endif
    {
        if (_exists)
        {
            return occupiedFunc(new OccupiedEntry<TKey, TValue>(_dictionary, _key, _valueRef), state);
        }

        if (_dictionary is null)
        {
            ThrowDefaultEntry();
        }

        return vacantFunc(new VacantEntry<TKey, TValue>(_dictionary, _key), state);
    }

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowDefaultEntry() =>
        throw new InvalidOperationException(
            "The entry is not associated with a dictionary. Obtain entries with Dictionary.Entry(key) rather than default.");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowKeyNotFound(TKey key) =>
        throw new KeyNotFoundException($"The key '{key}' was not found in the dictionary.");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowKeyAlreadyExists(TKey key) =>
        throw new InvalidOperationException($"The key '{key}' already exists in the dictionary.");

    /// <summary>
    /// Tries to get an occupied entry if the key exists in the dictionary.
    /// </summary>
    /// <param name="occupied">When this method returns, contains the occupied entry if the key was found; otherwise, the default value.</param>
    /// <returns><c>true</c> if the key was found; otherwise, <c>false</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetVacant(out VacantEntry<TKey, TValue> vacant)
    {
        if (!_exists && _dictionary is not null)
        {
            vacant = new VacantEntry<TKey, TValue>(_dictionary, _key);
            return true;
        }

        vacant = default;
        return false;
    }

    /// <summary>
    /// Provides compiler union members for C# union conversions and non-boxing pattern matching.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IUnionMembers
    {
        /// <summary>Creates an <see cref="Entry{TKey, TValue}"/> from an <see cref="OccupiedEntry{TKey, TValue}"/>.</summary>
        /// <remarks>
        /// Performs a fresh dictionary lookup: the resulting entry reflects the dictionary's current state, not the
        /// snapshot held by <paramref name="value"/>. If the key was removed since the occupied entry was obtained,
        /// the result is vacant; if the value changed, the result carries the current value.
        /// A <c>default</c> occupied entry produces a <c>default</c> (null) entry.
        /// </remarks>
        static Entry<TKey, TValue> Create(OccupiedEntry<TKey, TValue> value) =>
            value.Dictionary is not null ? new Entry<TKey, TValue>(value.Dictionary, value.Key) : default;

        /// <summary>Creates an <see cref="Entry{TKey, TValue}"/> from a <see cref="VacantEntry{TKey, TValue}"/>.</summary>
        /// <remarks>
        /// Performs a fresh dictionary lookup: if the key was inserted since the vacant entry was obtained,
        /// the result is occupied. A <c>default</c> vacant entry produces a <c>default</c> (null) entry.
        /// </remarks>
        static Entry<TKey, TValue> Create(VacantEntry<TKey, TValue> value) =>
            value.Dictionary is not null ? new Entry<TKey, TValue>(value.Dictionary, value.Key) : default;

        /// <summary>Gets the boxed case value of the union, or <c>null</c> for a default instance.</summary>
        object? Value { get; }

        /// <summary>Gets a value indicating whether the union contains a value.</summary>
        bool HasValue { get; }

        /// <summary>Tries to extract the <see cref="OccupiedEntry{TKey, TValue}"/> case without boxing.</summary>
        bool TryGetValue(out OccupiedEntry<TKey, TValue> value);

        /// <summary>Tries to extract the <see cref="VacantEntry{TKey, TValue}"/> case without boxing.</summary>
        bool TryGetValue(out VacantEntry<TKey, TValue> value);
    }

    object? IUnionMembers.Value => _dictionary is null
        ? null
        : _exists
            ? new OccupiedEntry<TKey, TValue>(_dictionary, _key, _valueRef)
            : new VacantEntry<TKey, TValue>(_dictionary, _key);

    bool IUnionMembers.HasValue
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _dictionary is not null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    bool IUnionMembers.TryGetValue(out OccupiedEntry<TKey, TValue> value)
    {
        if (_exists)
        {
            value = new OccupiedEntry<TKey, TValue>(_dictionary, _key, _valueRef);
            return true;
        }

        value = default;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    bool IUnionMembers.TryGetValue(out VacantEntry<TKey, TValue> value)
    {
        if (!_exists && _dictionary is not null)
        {
            value = new VacantEntry<TKey, TValue>(_dictionary, _key);
            return true;
        }

        value = default;
        return false;
    }
}
