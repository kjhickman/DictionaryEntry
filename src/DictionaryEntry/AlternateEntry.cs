using System.Runtime.CompilerServices;

namespace DictionaryEntry;

/// <summary>
/// Represents a dictionary entry looked up via a <see cref="Dictionary{TKey, TValue}.AlternateLookup{TAlternateKey}"/>
/// that may or may not exist within the dictionary.
/// Provides single-lookup operations without allocating a <typeparamref name="TKey"/> when the entry already exists
/// or is inspected without insertion.
/// </summary>
/// <typeparam name="TKey">The type of the key stored in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of the value stored in the dictionary.</typeparam>
/// <typeparam name="TAlternateKey">The type of the alternate key used for lookup (such as <see cref="ReadOnlySpan{T}"/>).</typeparam>
/// <remarks>
/// Because <typeparamref name="TAlternateKey"/> allows <c>ref struct</c> types, <see cref="AlternateEntry{TKey, TValue, TAlternateKey}"/>,
/// <see cref="AlternateOccupiedEntry{TKey, TValue, TAlternateKey}"/>, and <see cref="AlternateVacantEntry{TKey, TValue, TAlternateKey}"/>
/// are <c>ref struct</c>s and do not participate in C# 15 <c>[Union]</c> type pattern matching or <c>async</c>/<c>await</c> boundaries.
/// Use fluent methods, property patterns (e.g. <c>{ IsOccupied: true }</c>), <see cref="TryGetOccupied"/>, <see cref="TryGetVacant"/>,
/// or <c>Match</c> instead.
/// </remarks>
public ref struct AlternateEntry<TKey, TValue, TAlternateKey>
    where TKey : notnull
    where TAlternateKey : notnull, allows ref struct
{
    private readonly Dictionary<TKey, TValue> _dictionary;
    private readonly TAlternateKey _key;
    private ref TValue _valueRef;
    // Occupied: (prevOrBucket << 32) | entryIndex; Vacant: (version << 32) | hashCode; Transitioned: DictionaryAccess.UnknownLocation.
    private ulong _packedState;
    // Vacant only: the empty bucket found by the lookup (null if the probed chain was non-empty), reused on insertion.
    private ref int _bucket;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal AlternateEntry(Dictionary<TKey, TValue> dictionary, TAlternateKey key)
    {
        _dictionary = dictionary;
        _key = key;
        _valueRef = ref DictionaryAccess<TKey, TValue>.FindAlternateValue(dictionary, key, out ulong packedState, out BucketRef bucket);
        _packedState = packedState;
        _bucket = ref bucket.Value;
    }

    /// <summary>
    /// Gets the alternate key associated with this entry.
    /// </summary>
    public readonly TAlternateKey Key => _key;

    /// <summary>
    /// Gets a value indicating whether the entry exists in the dictionary.
    /// </summary>
    public readonly bool IsOccupied
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => !Unsafe.IsNullRef(ref _valueRef);
    }

    /// <summary>
    /// Gets a value indicating whether the entry does not exist in the dictionary.
    /// </summary>
    /// <remarks>
    /// A <c>default</c> entry is neither occupied nor vacant.
    /// </remarks>
    public readonly bool IsVacant
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Unsafe.IsNullRef(ref _valueRef) && _dictionary is not null;
    }

    /// <summary>
    /// Gets a reference to the value associated with this entry in the dictionary.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if this is a <c>default</c> entry.</exception>
    /// <exception cref="KeyNotFoundException">Thrown if the entry does not exist in the dictionary.</exception>
    /// <remarks>
    /// Check <see cref="IsOccupied"/> before accessing this property to mutate an existing entry in place
    /// without copying the value or inserting when absent. Do not mutate or resize the dictionary externally
    /// while holding the returned reference.
    /// </remarks>
    public readonly ref TValue ValueRef
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if (Unsafe.IsNullRef(ref _valueRef))
            {
                DictionaryThrowHelper.ThrowAlternateVacantOrDefault<TKey, TValue, TAlternateKey>(_dictionary, _key);
            }

            return ref _valueRef;
        }
    }

    /// <summary>
    /// Applies a modification function to the value if the entry exists in the dictionary.
    /// </summary>
    /// <param name="updateFunc">A function that takes the current value and returns the new value. This function is only called if the entry exists.</param>
    /// <returns>The entry instance to allow for method chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown if this is a <c>default</c> entry.</exception>
    /// <remarks>
    /// If the entry does not exist, this method does nothing and returns the unchanged entry.
    /// Do not mutate or resize the dictionary inside <paramref name="updateFunc"/>.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly AlternateEntry<TKey, TValue, TAlternateKey> AndModify(Func<TValue, TValue> updateFunc)
    {
        if (Unsafe.IsNullRef(ref _valueRef))
        {
            DictionaryThrowHelper.ThrowIfDefault(_dictionary);

            return this;
        }

        _valueRef = updateFunc(_valueRef);
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
    /// <remarks>
    /// Do not mutate or resize the dictionary inside <paramref name="updateFunc"/>.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly AlternateEntry<TKey, TValue, TAlternateKey> AndModify<TState>(
        TState state,
        Func<TValue, TState, TValue> updateFunc)
        where TState : allows ref struct
    {
        if (Unsafe.IsNullRef(ref _valueRef))
        {
            DictionaryThrowHelper.ThrowIfDefault(_dictionary);

            return this;
        }

        _valueRef = updateFunc(_valueRef, state);
        return this;
    }

    /// <summary>
    /// Applies an in-place modification action to a reference to the value if the entry exists in the dictionary.
    /// </summary>
    /// <param name="updateAction">An action that mutates the existing value by reference. This action is only called if the entry exists.</param>
    /// <returns>The entry instance to allow for method chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown if this is a <c>default</c> entry.</exception>
    /// <remarks>
    /// If the entry does not exist, this method does nothing and returns the unchanged entry.
    /// Prefer this overload when <typeparamref name="TValue"/> is a value type to mutate fields in place without copying the struct.
    /// Do not mutate or resize the dictionary inside <paramref name="updateAction"/>.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly AlternateEntry<TKey, TValue, TAlternateKey> AndModify(RefAction<TValue> updateAction)
    {
        if (Unsafe.IsNullRef(ref _valueRef))
        {
            DictionaryThrowHelper.ThrowIfDefault(_dictionary);

            return this;
        }

        updateAction(ref _valueRef);
        return this;
    }

    /// <summary>
    /// Applies an in-place modification action using the provided state to a reference to the value if the entry exists in the dictionary,
    /// avoiding struct copies and closure allocations when paired with a <c>static</c> lambda.
    /// </summary>
    /// <typeparam name="TState">The type of the state value passed to <paramref name="updateAction"/>.</typeparam>
    /// <param name="state">State passed through to <paramref name="updateAction"/>.</param>
    /// <param name="updateAction">An action that mutates the existing value by reference using <paramref name="state"/>. This action is only called if the entry exists.</param>
    /// <returns>The entry instance to allow for method chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown if this is a <c>default</c> entry.</exception>
    /// <remarks>
    /// If the entry does not exist, this method does nothing and returns the unchanged entry.
    /// Prefer this overload when <typeparamref name="TValue"/> is a value type to mutate fields in place without copying the struct.
    /// Do not mutate or resize the dictionary inside <paramref name="updateAction"/>.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly AlternateEntry<TKey, TValue, TAlternateKey> AndModify<TState>(
        TState state,
        RefAction<TValue, TState> updateAction)
        where TState : allows ref struct
    {
        if (Unsafe.IsNullRef(ref _valueRef))
        {
            DictionaryThrowHelper.ThrowIfDefault(_dictionary);

            return this;
        }

        updateAction(ref _valueRef, state);
        return this;
    }

    /// <summary>
    /// Gets a reference to the value in the dictionary if it exists, or inserts the specified value if it doesn't.
    /// </summary>
    /// <param name="defaultValue">The value to insert if the entry does not exist.</param>
    /// <returns>A reference to the value in the dictionary.</returns>
    /// <exception cref="InvalidOperationException">Thrown if this is a <c>default</c> entry.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref TValue OrInsert(TValue defaultValue)
    {
        if (!Unsafe.IsNullRef(ref _valueRef))
        {
            return ref _valueRef;
        }

        DictionaryThrowHelper.ThrowIfDefault(_dictionary);

        return ref InsertVacant(defaultValue);
    }

    /// <summary>
    /// Gets a reference to the value in the dictionary if it exists, or computes and inserts a new value if it doesn't.
    /// </summary>
    /// <param name="valueFactory">A function that computes the value to insert if the entry does not exist.</param>
    /// <returns>A reference to the value in the dictionary.</returns>
    /// <exception cref="InvalidOperationException">Thrown if this is a <c>default</c> entry.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref TValue OrInsertWith(Func<TValue> valueFactory)
    {
        if (!Unsafe.IsNullRef(ref _valueRef))
        {
            return ref _valueRef;
        }

        DictionaryThrowHelper.ThrowIfDefault(_dictionary);

        return ref InsertVacant(valueFactory());
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref TValue OrInsertWith<TState>(TState state, Func<TState, TValue> valueFactory)
        where TState : allows ref struct
    {
        if (!Unsafe.IsNullRef(ref _valueRef))
        {
            return ref _valueRef;
        }

        DictionaryThrowHelper.ThrowIfDefault(_dictionary);

        return ref InsertVacant(valueFactory(state));
    }

    /// <summary>
    /// Gets a reference to the value in the dictionary if it exists, or computes and inserts a new value based on the alternate key if it doesn't.
    /// </summary>
    /// <param name="valueFactory">A function that takes the alternate key and computes the value to insert if the entry does not exist.</param>
    /// <returns>A reference to the value in the dictionary.</returns>
    /// <exception cref="InvalidOperationException">Thrown if this is a <c>default</c> entry.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref TValue OrInsertWithKey(Func<TAlternateKey, TValue> valueFactory)
    {
        if (!Unsafe.IsNullRef(ref _valueRef))
        {
            return ref _valueRef;
        }

        DictionaryThrowHelper.ThrowIfDefault(_dictionary);

        return ref InsertVacant(valueFactory(_key));
    }

    /// <summary>
    /// Gets a reference to the value in the dictionary if it exists, or computes and inserts a new value based on the alternate key and provided state if it doesn't,
    /// avoiding closure allocations when paired with a <c>static</c> lambda.
    /// </summary>
    /// <typeparam name="TState">The type of the state value passed to <paramref name="valueFactory"/>.</typeparam>
    /// <param name="state">State passed through to <paramref name="valueFactory"/>.</param>
    /// <param name="valueFactory">A function that takes the alternate key and <paramref name="state"/> and computes the value to insert if the entry does not exist.</param>
    /// <returns>A reference to the value in the dictionary.</returns>
    /// <exception cref="InvalidOperationException">Thrown if this is a <c>default</c> entry.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref TValue OrInsertWithKey<TState>(TState state, Func<TAlternateKey, TState, TValue> valueFactory)
        where TState : allows ref struct
    {
        if (!Unsafe.IsNullRef(ref _valueRef))
        {
            return ref _valueRef;
        }

        DictionaryThrowHelper.ThrowIfDefault(_dictionary);

        return ref InsertVacant(valueFactory(_key, state));
    }

    /// <summary>
    /// Gets a reference to the value in the dictionary if it exists, or inserts the default value for the type if it doesn't.
    /// </summary>
    /// <returns>A reference to the value in the dictionary.</returns>
    /// <exception cref="InvalidOperationException">Thrown if this is a <c>default</c> entry.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref TValue OrDefault()
    {
        if (!Unsafe.IsNullRef(ref _valueRef))
        {
            return ref _valueRef;
        }

        DictionaryThrowHelper.ThrowIfDefault(_dictionary);

        _valueRef = ref DictionaryAccess<TKey, TValue>.AddAlternateVacant(_dictionary, _key, _packedState, ref _bucket);
        _packedState = DictionaryAccess.UnknownLocation;
        return ref _valueRef;
    }

    /// <summary>
    /// Creates an occupied entry with the specified value. If the entry already exists, the value is updated.
    /// </summary>
    /// <param name="value">The value to insert or update.</param>
    /// <returns>An <see cref="AlternateOccupiedEntry{TKey, TValue, TAlternateKey}"/> representing the entry in the dictionary.</returns>
    /// <exception cref="InvalidOperationException">Thrown if this is a <c>default</c> entry.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AlternateOccupiedEntry<TKey, TValue, TAlternateKey> InsertEntry(TValue value)
    {
        if (!Unsafe.IsNullRef(ref _valueRef))
        {
            _valueRef = value;
            return new AlternateOccupiedEntry<TKey, TValue, TAlternateKey>(_dictionary, _key, value, _packedState);
        }

        DictionaryThrowHelper.ThrowIfDefault(_dictionary);

        InsertVacant(value);
        return new AlternateOccupiedEntry<TKey, TValue, TAlternateKey>(_dictionary, _key, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ref TValue InsertVacant(TValue value)
    {
        _valueRef = ref DictionaryAccess<TKey, TValue>.AddAlternateVacant(_dictionary, _key, _packedState, ref _bucket);
        _valueRef = value;
        _packedState = DictionaryAccess.UnknownLocation;
        return ref _valueRef;
    }

    /// <summary>
    /// Converts the entry to an occupied entry if it exists in the dictionary.
    /// </summary>
    /// <returns>An <see cref="AlternateOccupiedEntry{TKey, TValue, TAlternateKey}"/> representing the entry in the dictionary.</returns>
    /// <exception cref="InvalidOperationException">Thrown if this is a <c>default</c> entry.</exception>
    /// <exception cref="KeyNotFoundException">Thrown if the entry does not exist in the dictionary.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly AlternateOccupiedEntry<TKey, TValue, TAlternateKey> ToOccupied() =>
        new(_dictionary, _key, ValueRef, _packedState);

    /// <summary>
    /// Converts the entry to a vacant entry if it does not exist in the dictionary.
    /// </summary>
    /// <returns>An <see cref="AlternateVacantEntry{TKey, TValue, TAlternateKey}"/> representing the entry not in the dictionary.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the entry exists in the dictionary, or if this is a <c>default</c> entry.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly AlternateVacantEntry<TKey, TValue, TAlternateKey> ToVacant()
    {
        if (!Unsafe.IsNullRef(ref _valueRef) || _dictionary is null)
        {
            DictionaryThrowHelper.ThrowAlternateOccupiedOrDefault<TKey, TValue, TAlternateKey>(_dictionary, _key);
        }

        return new AlternateVacantEntry<TKey, TValue, TAlternateKey>(_dictionary, _key, _packedState);
    }

    /// <summary>
    /// Performs pattern matching on the entry, executing different actions based on whether the entry exists.
    /// </summary>
    /// <param name="occupiedAction">The action to execute if the entry exists.</param>
    /// <param name="vacantAction">The action to execute if the entry does not exist.</param>
    /// <exception cref="InvalidOperationException">Thrown if this is a <c>default</c> entry.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Match(
        Action<AlternateOccupiedEntry<TKey, TValue, TAlternateKey>> occupiedAction,
        Action<AlternateVacantEntry<TKey, TValue, TAlternateKey>> vacantAction)
    {
        if (!Unsafe.IsNullRef(ref _valueRef))
        {
            occupiedAction(new AlternateOccupiedEntry<TKey, TValue, TAlternateKey>(_dictionary, _key, _valueRef, _packedState));
        }
        else
        {
            DictionaryThrowHelper.ThrowIfDefault(_dictionary);

            vacantAction(new AlternateVacantEntry<TKey, TValue, TAlternateKey>(_dictionary, _key, _packedState));
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Match<TState>(
        TState state,
        Action<AlternateOccupiedEntry<TKey, TValue, TAlternateKey>, TState> occupiedAction,
        Action<AlternateVacantEntry<TKey, TValue, TAlternateKey>, TState> vacantAction)
        where TState : allows ref struct
    {
        if (!Unsafe.IsNullRef(ref _valueRef))
        {
            occupiedAction(new AlternateOccupiedEntry<TKey, TValue, TAlternateKey>(_dictionary, _key, _valueRef, _packedState), state);
        }
        else
        {
            DictionaryThrowHelper.ThrowIfDefault(_dictionary);

            vacantAction(new AlternateVacantEntry<TKey, TValue, TAlternateKey>(_dictionary, _key, _packedState), state);
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly TResult Match<TResult>(
        Func<AlternateOccupiedEntry<TKey, TValue, TAlternateKey>, TResult> occupiedFunc,
        Func<AlternateVacantEntry<TKey, TValue, TAlternateKey>, TResult> vacantFunc)
    {
        if (!Unsafe.IsNullRef(ref _valueRef))
        {
            return occupiedFunc(new AlternateOccupiedEntry<TKey, TValue, TAlternateKey>(_dictionary, _key, _valueRef, _packedState));
        }

        DictionaryThrowHelper.ThrowIfDefault(_dictionary);

        return vacantFunc(new AlternateVacantEntry<TKey, TValue, TAlternateKey>(_dictionary, _key, _packedState));
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly TResult Match<TState, TResult>(
        TState state,
        Func<AlternateOccupiedEntry<TKey, TValue, TAlternateKey>, TState, TResult> occupiedFunc,
        Func<AlternateVacantEntry<TKey, TValue, TAlternateKey>, TState, TResult> vacantFunc)
        where TState : allows ref struct
    {
        if (!Unsafe.IsNullRef(ref _valueRef))
        {
            return occupiedFunc(new AlternateOccupiedEntry<TKey, TValue, TAlternateKey>(_dictionary, _key, _valueRef, _packedState), state);
        }

        DictionaryThrowHelper.ThrowIfDefault(_dictionary);

        return vacantFunc(new AlternateVacantEntry<TKey, TValue, TAlternateKey>(_dictionary, _key, _packedState), state);
    }

    /// <summary>
    /// Tries to get an occupied entry if the key exists in the dictionary.
    /// </summary>
    /// <param name="occupied">When this method returns, contains the occupied entry if the key was found; otherwise, the default value.</param>
    /// <returns><c>true</c> if the key was found; otherwise, <c>false</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool TryGetOccupied(out AlternateOccupiedEntry<TKey, TValue, TAlternateKey> occupied)
    {
        if (!Unsafe.IsNullRef(ref _valueRef))
        {
            occupied = new AlternateOccupiedEntry<TKey, TValue, TAlternateKey>(_dictionary, _key, _valueRef, _packedState);
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
    public readonly bool TryGetVacant(out AlternateVacantEntry<TKey, TValue, TAlternateKey> vacant)
    {
        if (Unsafe.IsNullRef(ref _valueRef) && _dictionary is not null)
        {
            vacant = new AlternateVacantEntry<TKey, TValue, TAlternateKey>(_dictionary, _key, _packedState);
            return true;
        }

        vacant = default;
        return false;
    }
}
