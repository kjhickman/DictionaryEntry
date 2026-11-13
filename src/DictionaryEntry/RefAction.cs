namespace DictionaryEntry;

/// <summary>
/// Encapsulates a method that mutates a value in place by reference and does not return a value.
/// </summary>
/// <typeparam name="TValue">The type of the value to mutate by reference.</typeparam>
/// <param name="value">A mutable reference to the value.</param>
public delegate void RefAction<TValue>(ref TValue value);

/// <summary>
/// Encapsulates a method that mutates a value in place by reference using the provided state and does not return a value.
/// </summary>
/// <typeparam name="TValue">The type of the value to mutate by reference.</typeparam>
/// <typeparam name="TState">The type of the state value passed to the method.</typeparam>
/// <param name="value">A mutable reference to the value.</param>
/// <param name="state">State passed through to the method.</param>
public delegate void RefAction<TValue, in TState>(ref TValue value, TState state)
    where TState : allows ref struct;
