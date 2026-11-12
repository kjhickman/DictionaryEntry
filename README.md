# DictionaryEntry

DictionaryEntry is a small library that brings Rust's "Entry" API pattern to C# `Dictionary<TKey, TValue>`, allowing for ergonomic single-lookup dictionary operations. It combines a fluent API with **zero-boxing C# 15 union support**, matching or nearly matching hand-written `Dictionary` performance (see [benchmarks](#benchmarks)).

## Installation

```bash
dotnet add package DictionaryEntry
```

Targets `net8.0`, `net9.0`, `net10.0`, and `net11.0` (AOT-compatible).

## Examples

The library provides three main types:

- `Entry<TKey, TValue>`: A `ref struct` union of `OccupiedEntry<TKey, TValue>` and `VacantEntry<TKey, TValue>` returned by `.Entry(key)`
- `OccupiedEntry<TKey, TValue>`: A `readonly struct` representing an entry for an existing key
- `VacantEntry<TKey, TValue>`: A `readonly struct` representing an entry for a missing key

### Insert-or-Update (Upsert)

```csharp
// Traditional approach
if (scores.TryGetValue(player, out var score))
{
    score += points;
    scores[player] = score;
}
else
{
    scores[player] = points;
}

// Using DictionaryEntry (fluent)
scores.Entry(player).AndModify(score => score + points).OrInsert(points);

// Zero-allocation fluent with TState + static lambda
scores.Entry(player).AndModify(points, static (score, pts) => score + pts).OrInsert(points);

// Or via ref return
scores.Entry(player).OrDefault() += points;
```

### Get-or-Add

```csharp
// Traditional approach
if (!cache.TryGetValue(key, out var value))
{
    value = defaultValue;
    cache[key] = value;
}
return value;

// Using DictionaryEntry
return cache.Entry(key).OrInsert(defaultValue);
```

### Get-or-Initialize with Factory

```csharp
// Traditional approach
if (!cache.TryGetValue(key, out var value))
{
    value = ComputeExpensiveValue(key);
    cache[key] = value;
}
return value;

// Using DictionaryEntry
return cache.Entry(key).OrInsertWithKey(ComputeExpensiveValue);
```

### Pattern Matching (C# 15 Unions)

`Entry<TKey, TValue>` implements C# 15's custom non-boxing `[Union]` pattern over `OccupiedEntry<TKey, TValue>` and `VacantEntry<TKey, TValue>`, enabling zero-allocation, compiler-verified exhaustive `switch` expressions and `is` patterns:

```csharp
// Exhaustive switch expression (no '_' discard arm required)
string status = users.Entry(userId) switch
{
    OccupiedEntry<Guid, User> occupied => $"User found: {occupied.Value.Name}",
    VacantEntry<Guid, User> vacant => "User not found",
};

// 'is' pattern matching (Rust 'if let' style)
if (dict.Entry(key) is OccupiedEntry<string, int> occupied)
{
    Console.WriteLine($"Found: {occupied.Key} -> {occupied.Value}");
}

// Property and positional deconstruction patterns
string tier = scores.Entry(player) switch
{
    OccupiedEntry<string, int> { Value: >= 1000 } => "Gold",
    OccupiedEntry<string, int>(_, >= 100) => "Silver",
    OccupiedEntry<string, int> => "Bronze",
    VacantEntry<string, int> => "Unranked",
};

// Switch statement with local state (no lambdas or closure allocations)
switch (users.Entry(userId))
{
    case OccupiedEntry<Guid, User> occupied:
        ProcessExistingUser(occupied.Value);
        break;
    case VacantEntry<Guid, User> vacant:
        vacant.Insert(CreateNewUser(userId));
        break;
}
```

### Pattern Matching with `Match` / `TryGetOccupied`

On earlier C# versions—or when you prefer type inference over writing `<TKey, TValue>`—you can use `Match`, `TryGetOccupied`, and `TryGetVacant`:

```csharp
users.Entry(userId).Match(
    occupied => ProcessExistingUser(occupied.Value),
    vacant => vacant.Insert(CreateNewUser(userId))
);

if (dict.Entry(key).TryGetOccupied(out var occupied))
{
    Console.WriteLine($"Found: {occupied.Key} -> {occupied.Value}");
}
```

### Reference Semantics

```csharp
// Modify value in-place
ref var user = ref users.Entry(userId).OrInsert(new User());
user.LastLoginDate = DateTime.UtcNow;
user.LoginCount++;

// No need to write back to the dictionary!
```

## Benchmarks

The library is designed around single-lookup access and zero-boxing structs. Because `Entry<TKey, TValue>` implements C# 15's non-boxing union access pattern (`TryGetValue` / `HasValue` via explicit `IUnionMembers`) rather than `object`-backed union storage, union `switch` and `is` operations never box `OccupiedEntry` or `VacantEntry`.

### Key Benchmarks (.NET 11)

```text
BenchmarkDotNet v0.15.8, macOS 26.6.2 (Darwin 25.6.0)
Apple M1 Pro, 1 CPU, 8 logical and 8 physical cores
.NET SDK 11.0.100
  [Host] : .NET 11.0.0 (11.0.0, 11.0.26.53018), Arm64 RyuJIT armv8.0-a

InvocationCount=100000000  IterationCount=15  WarmupCount=10
```

Get-or-Add (`OrInsert`):

```text
| Method                         | Mean     | Allocated |
|------------------------------- |---------:|----------:|
| GetOrAdd_Traditional_Exists    | 2.494 ns |         - |
| GetOrAdd_Traditional_NotExists | 2.489 ns |         - |
| GetOrAdd_Entry_Exists          | 2.579 ns |         - |
| GetOrAdd_Entry_NotExists       | 2.513 ns |         - |
```

Get-or-Default (`OrDefault`):

```text
| Method                              | Mean     | Allocated |
|------------------------------------ |---------:|----------:|
| DefaultValue_Traditional_Exists     | 3.372 ns |         - |
| DefaultValue_Traditional_NotExists  | 7.541 ns |         - |
| DefaultValue_Entry_Exists           | 3.362 ns |         - |
| DefaultValue_Entry_NotExists        | 3.088 ns |         - |
```

Pattern Matching (`.Match(...)` vs. `switch`):

```text
| Method                                   | Mean      | Allocated |
|----------------------------------------- |----------:|----------:|
| PatternMatching_Traditional_Exists       |  3.556 ns |         - |
| PatternMatching_Traditional_NotExists    |  2.204 ns |         - |
| PatternMatching_EntryMatch_Exists        |  4.294 ns |         - |
| PatternMatching_EntryMatch_NotExists     |  3.983 ns |         - |
| PatternMatching_EntrySwitch_Exists       |  3.726 ns |         - |
| PatternMatching_EntrySwitch_NotExists    |  2.970 ns |         - |
| DifferentActions_Traditional_Exists      |  7.111 ns |         - |
| DifferentActions_Traditional_NotExists   |  6.634 ns |         - |
| DifferentActions_EntryMatch_Exists       | 10.449 ns |         - |
| DifferentActions_EntryMatch_NotExists    |  9.595 ns |         - |
| DifferentActions_EntrySwitch_Exists      |  9.108 ns |         - |
| DifferentActions_EntrySwitch_NotExists   |  8.297 ns |         - |
```

Increment Counter (`AndModify` vs. `AndModify(state, ...)` vs. `switch`):

```text
| Method                                          | Mean      | Allocated |
|------------------------------------------------ |----------:|----------:|
| IncrementCounter_Traditional_Exists             |  7.124 ns |         - |
| IncrementCounter_Traditional_NotExists          |  6.639 ns |         - |
| IncrementCounter_EntryAndModify_Exists          |  9.528 ns |         - |
| IncrementCounter_EntryAndModify_NotExists       |  8.646 ns |         - |
| IncrementByAmount_Traditional_Exists            |  7.160 ns |         - |
| IncrementByAmount_Traditional_NotExists         |  6.626 ns |         - |
| IncrementByAmount_EntryAndModify_Exists         | 13.216 ns |      24 B |
| IncrementByAmount_EntryAndModify_NotExists      | 12.339 ns |      24 B |
| IncrementByAmount_EntryAndModifyState_Exists    |  9.478 ns |         - |
| IncrementByAmount_EntryAndModifyState_NotExists |  8.657 ns |         - |
| IncrementByAmount_EntrySwitch_Exists            |  9.712 ns |         - |
| IncrementByAmount_EntrySwitch_NotExists         |  9.101 ns |         - |
```

Stateful Factory (`OrInsertWithKey` capturing vs. `TState` overload):

```text
| Method                                   | Mean     | Allocated |
|----------------------------------------- |---------:|----------:|
| StatefulFactory_Traditional_Exists       | 3.388 ns |         - |
| StatefulFactory_Traditional_NotExists    | 3.090 ns |         - |
| StatefulFactory_EntryCapturing_Exists    | 7.726 ns |      24 B |
| StatefulFactory_EntryCapturing_NotExists | 7.378 ns |      24 B |
| StatefulFactory_EntryState_Exists        | 3.512 ns |         - |
| StatefulFactory_EntryState_NotExists     | 3.205 ns |         - |
```

To run the benchmarks yourself:

```bash
cd src/DictionaryEntry.Benchmarks
dotnet run -c Release
```

*(Earlier full-suite v0.1.0 results on .NET 9 x64 are also archived in [benchmarks/results-0.1.0.md](./benchmarks/results-0.1.0.md).)*

### Performance Considerations with Lambdas and Closures

Methods that accept delegates (`AndModify`, `OrInsertWith`, `OrInsertWithKey`, and `Match`) are zero-allocation when passed a non-capturing lambda or `static` method group, because C# caches a singleton delegate instance.

When a lambda **captures local variables or `this`**, C# allocates a closure object on the heap—even on cache hits before the method is called (`24 B` on .NET 10/11 where the delegate itself is stack-allocated by JIT escape analysis, and `88 B` on .NET 8/9):

```csharp
// Captures 'amount' -> allocates a 24 B closure on .NET 10/11 (88 B on .NET 8/9)
private void IncrementByAmountCapturing(string key, int amount)
{
    _dictionary.Entry(key).AndModify(count => count + amount).OrInsert(amount);
}
```

On hot paths that need local state without closure allocations, use the **`TState` overloads** with `static` lambdas, a **union `switch`**, or **`ref` returns**:

```csharp
// Zero-allocation with TState overload + static lambda (also supports Span/ReadOnlySpan state on .NET 9+)
private void IncrementByAmountState(string key, int amount)
{
    _dictionary.Entry(key).AndModify(amount, static (count, amt) => count + amt).OrInsert(amount);
}

// Zero-allocation lazy factory with TState (passes value tuples or ReadOnlySpan<T> on .NET 9+)
private int GetOrComputeScaled(string key, int multiplier)
{
    return _dictionary.Entry(key).OrInsertWithKey(multiplier, static (k, mult) => k.Length * mult);
}

// Zero-allocation with switch (no delegates or closures)
private void IncrementByAmountSwitch(string key, int amount)
{
    switch (_dictionary.Entry(key))
    {
        case OccupiedEntry<string, int> occupied:
            occupied.Insert(occupied.Value + amount);
            break;
        case VacantEntry<string, int> vacant:
            vacant.Insert(amount);
            break;
    }
}

// Zero-allocation with ref return
private void IncrementByAmountRef(string key, int amount)
{
    _dictionary.Entry(key).OrDefault() += amount;
}
```

## Limitations

### C# 15 Union Pattern Matching

- **Requires C# 15 (.NET 11 SDK):** Native union `switch` and `is` patterns require compiling with C# 15 (`net11.0` uses C# 15 by default; projects targeting `net8.0`–`net10.0` on the .NET 11 SDK can also opt in with `<LangVersion>15.0</LangVersion>` or `<LangVersion>latest</LangVersion>`). On older C# versions/SDKs, all fluent methods, `Match`, `TryGetOccupied`, and `TryGetVacant` continue to work normally.
- **`Entry` is live, cases are snapshots:** `OccupiedEntry.Value` is captured when the entry is obtained and is not updated by `Insert` or later dictionary changes. Converting an `OccupiedEntry` / `VacantEntry` back to `Entry<TKey, TValue>` (e.g. `Entry<string, int> e = occupied;`) performs a fresh lookup, so the result reflects the dictionary's *current* state and may be a different case if the dictionary changed in between. `OccupiedEntry.Remove()` / `RemoveEntry()` return the value actually removed, and throw `KeyNotFoundException` if the key is no longer present.
- **Explicit generic type arguments in type patterns:** Because C# 15 unions are type unions rather than nominal enum variants, C# does not infer `<TKey, TValue>` in `OccupiedEntry<TKey, TValue>` / `VacantEntry<TKey, TValue>` patterns. When `<TKey, TValue>` is verbose, `TryGetOccupied(out var occupied)`, `Match(...)`, or a `using` type alias avoids repeating the type parameters.

### `ref struct` & Async Operations

`Entry<TKey, TValue>` is a `ref struct` holding an interior `ref TValue` into the dictionary, which means:

- An `Entry<TKey, TValue>` instance cannot be boxed, stored in fields of classes or regular structs, captured in lambdas, or preserved across `await` / `yield return` boundaries:
  ```csharp
  // This does NOT work
  var user = cache.Entry(userId).OrInsertWith(async () => await db.LoadAsync(userId));
  ```
- **Async with C# 15 union `switch`:** `OccupiedEntry<TKey, TValue>` and `VacantEntry<TKey, TValue>` are regular `readonly struct`s (not `ref struct`s). Switching on `cache.Entry(userId)` unwraps into `OccupiedEntry` or `VacantEntry` *before* hitting an `await`, so `async`/`await` works directly inside `switch` expressions and statements:
  ```csharp
  var user = cache.Entry(userId) switch
  {
      OccupiedEntry<Guid, User> occupied => occupied.Value,
      VacantEntry<Guid, User> vacant => vacant.Insert(await db.LoadUserAsync(vacant.Key)),
  };
  ```
- **Async on earlier C# versions:** Return a `Task` from `Match` and `await` the result:
  ```csharp
  var user = await cache.Entry(userId).Match(
      occupied => Task.FromResult(occupied.Value),
      async vacant => vacant.Insert(await db.LoadUserAsync(vacant.Key)));
  ```
- **Do not mutate the dictionary externally while holding an `Entry` local:** Because `Entry<TKey, TValue>` caches a `CollectionsMarshal` value reference, mutating or resizing the same `Dictionary<TKey, TValue>` directly (outside the `Entry`'s own methods) invalidates that `Entry` instance.

### Thread Safety

`Dictionary<TKey, TValue>` is not thread-safe. In concurrent scenarios, protect access with synchronization or use a concurrent collection.

## Potential Features

- Support for `ConcurrentDictionary<TKey, TValue>` and other dictionary-like collections
- `Dictionary<TKey, TValue>.AlternateLookup<TAlternateKey>` support for span-based lookups

## License

DictionaryEntry is [MIT Licensed](https://github.com/kjhickman/DictionaryEntry/blob/main/LICENSE).
