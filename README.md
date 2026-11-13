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
// Get-or-insert and modify value in-place
ref var user = ref users.Entry(userId).OrInsert(new User());
user.LastLoginDate = DateTime.UtcNow;
user.LoginCount++;

// Update in-place ONLY if the key already exists (1 lookup, 0 struct copies)
if (playerStats.Entry(playerId) is { IsOccupied: true } entry)
{
    ref var stats = ref entry.ValueRef;
    stats.HitsLanded++;
    stats.TotalDamage += damage;
}

// Or fluently mutate struct fields in-place with RefAction + OrInsert (1 lookup, 0 struct copies)
playerStats.Entry(playerId)
    .AndModify(damage, static (ref stats, dmg) =>
    {
        stats.HitsLanded++;
        stats.TotalDamage += dmg;
    })
    .OrInsert(new PlayerStats { HitsLanded = 1, TotalDamage = damage });

// No need to write back to the dictionary!
```

### Span & `AlternateLookup` (.NET 9+)

On .NET 9+, `AlternateLookup<TAlternateKey>.Entry(key)` provides the same fluent API over `ReadOnlySpan<char>`, `ReadOnlySpan<byte>`, or any custom `IAlternateEqualityComparer<TAlternateKey, TKey>`—allocating zero strings on cache hits and materializing the `TKey` only once when inserting a missing key:

```csharp
var counts = new Dictionary<string, int>(StringComparer.Ordinal);
var lookup = counts.GetAlternateLookup<ReadOnlySpan<char>>();

ReadOnlySpan<char> csv = "apple,banana,apple,cherry";
foreach (var range in csv.Split(','))
{
    ReadOnlySpan<char> word = csv[range];
    lookup.Entry(word).AndModify(static c => c + 1).OrInsert(1);
}
```

## Benchmarks

The library is designed around single-lookup access and zero-boxing structs. Because `Entry<TKey, TValue>` implements C# 15's non-boxing union access pattern (`TryGetValue` / `HasValue` via explicit `IUnionMembers`) rather than `object`-backed union storage, union `switch` and `is` operations never box `OccupiedEntry` or `VacantEntry`.

### Key Benchmarks (.NET 11)

```text
BenchmarkDotNet v0.15.8, macOS 26.6.2 (Darwin 25.6.0)
Apple M1 Pro, 1 CPU, 8 logical and 8 physical cores
.NET SDK 11.0.100
  [Host] : .NET 11.0.0 (11.0.0, 11.0.26.53018), Arm64 RyuJIT armv8.0-a

InvocationCount=250000  OperationsPerInvoke=256  IterationCount=15  WarmupCount=10
```

Get-or-Add (`OrInsert` on `Dictionary<int, int>`):

```text
| Method                         | Mean     | Allocated |
|------------------------------- |---------:|----------:|
| GetOrAdd_Traditional_Exists    | 1.837 ns |         - |
| GetOrAdd_Traditional_NotExists | 5.235 ns |         - |
| GetOrAdd_Entry_Exists          | 2.044 ns |         - |
| GetOrAdd_Entry_NotExists       | 5.779 ns |         - |
```

Get-or-Default (`OrDefault` on `Dictionary<string, int>` / `Dictionary<string, string?>`):

```text
| Method                                   | Mean      | Allocated |
|----------------------------------------- |----------:|----------:|
| DefaultValue_Traditional_Exists          |  4.148 ns |         - |
| DefaultValue_Traditional_NotExists       | 11.818 ns |         - |
| DefaultValue_Entry_Exists                |  4.380 ns |         - |
| DefaultValue_Entry_NotExists             | 12.601 ns |         - |
| DefaultValue_TraditionalString_NotExists | 10.581 ns |         - |
| DefaultValue_EntryString_NotExists       |  9.834 ns |         - |
```

Pattern Matching (`.Match(...)` vs. `switch` on `Dictionary<string, int>`):

```text
| Method                                 | Mean      | Allocated |
|--------------------------------------- |----------:|----------:|
| PatternMatching_Traditional_Exists     |  4.569 ns |         - |
| PatternMatching_Traditional_NotExists  |  3.300 ns |         - |
| PatternMatching_EntryMatch_Exists      |  5.328 ns |         - |
| PatternMatching_EntryMatch_NotExists   |  4.362 ns |         - |
| PatternMatching_EntrySwitch_Exists     |  4.835 ns |         - |
| PatternMatching_EntrySwitch_NotExists  |  3.518 ns |         - |
| DifferentActions_Traditional_Exists    |  9.908 ns |         - |
| DifferentActions_Traditional_NotExists | 11.587 ns |         - |
| DifferentActions_EntryMatch_Exists     | 11.736 ns |         - |
| DifferentActions_EntryMatch_NotExists  | 13.665 ns |         - |
| DifferentActions_EntrySwitch_Exists    |  9.167 ns |         - |
| DifferentActions_EntrySwitch_NotExists | 12.129 ns |         - |
```

Increment Counter (`AndModify` vs. `AndModify(state, ...)` vs. `switch` on `Dictionary<string, int>`):

```text
| Method                                          | Mean      | Allocated |
|------------------------------------------------ |----------:|----------:|
| IncrementCounter_Traditional_Exists             |  9.996 ns |         - |
| IncrementCounter_Traditional_NotExists          | 11.639 ns |         - |
| IncrementCounter_EntryAndModify_Exists          | 10.804 ns |         - |
| IncrementCounter_EntryAndModify_NotExists       | 12.808 ns |         - |
| IncrementByAmount_Traditional_Exists            |  9.052 ns |         - |
| IncrementByAmount_Traditional_NotExists         | 11.800 ns |         - |
| IncrementByAmount_EntryAndModify_Exists         | 27.079 ns |      80 B |
| IncrementByAmount_EntryAndModify_NotExists      | 24.381 ns |      80 B |
| IncrementByAmount_EntryAndModifyState_Exists    | 10.130 ns |         - |
| IncrementByAmount_EntryAndModifyState_NotExists | 12.719 ns |         - |
| IncrementByAmount_EntrySwitch_Exists            |  9.225 ns |         - |
| IncrementByAmount_EntrySwitch_NotExists         | 12.346 ns |         - |
```

Stateful Factory (`OrInsertWithKey` capturing vs. `TState` overload on `Dictionary<string, int>`):

```text
| Method                                   | Mean      | Allocated |
|----------------------------------------- |----------:|----------:|
| StatefulFactory_Traditional_Exists       |  4.113 ns |         - |
| StatefulFactory_Traditional_NotExists    | 11.946 ns |         - |
| StatefulFactory_EntryCapturing_Exists    | 17.186 ns |      80 B |
| StatefulFactory_EntryCapturing_NotExists | 30.216 ns |      80 B |
| StatefulFactory_EntryState_Exists        |  4.496 ns |         - |
| StatefulFactory_EntryState_NotExists     | 13.650 ns |         - |
```

Update Existing Only (`TryGetValue` + indexer vs. `AndModify` vs. `ValueRef` on `Dictionary<string, int>`):

```text
| Method                         | Mean      | Allocated |
|------------------------------- |----------:|----------:|
| Update_Traditional_Exists      |  9.197 ns |         - |
| Update_Traditional_NotExists   |  3.130 ns |         - |
| Update_Entry_Exists            | 10.875 ns |         - |
| Update_Entry_NotExists         |  3.485 ns |         - |
| Update_EntryValueRef_Exists    |  4.304 ns |         - |
| Update_EntryValueRef_NotExists |  3.373 ns |         - |
```

To run the benchmarks yourself:

```bash
cd src/DictionaryEntry.Benchmarks
dotnet run -c Release
```

*(Earlier full-suite v0.1.0 results on .NET 9 x64 are also archived in [benchmarks/results-0.1.0.md](./benchmarks/results-0.1.0.md).)*

### Performance Considerations with Lambdas and Closures

Methods that accept delegates (`AndModify`, `OrInsertWith`, `OrInsertWithKey`, and `Match`) are zero-allocation when passed a non-capturing lambda or `static` method group, because C# caches a singleton delegate instance.

When a lambda **captures local variables or `this`**, C# allocates a closure object on the heap—even on cache hits before the method is called (`24 B` outside loops on .NET 10/11 when JIT escape analysis stack-allocates the delegate, and up to `80–88 B` inside loops or on .NET 8/9 when the `64 B` delegate is also heap-allocated):

```csharp
// Captures 'amount' -> allocates a closure + delegate inside loops (24-88 B)
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
- **Do not mutate the dictionary externally while holding an `Entry` local (or inside `AndModify` callbacks):** Because `Entry<TKey, TValue>` caches a `CollectionsMarshal` value reference, mutating or resizing the same `Dictionary<TKey, TValue>` directly (outside the `Entry`'s own methods, including inside an `AndModify` callback) invalidates that `Entry` instance.

### `AlternateEntry` (`AlternateLookup` on .NET 9+)

Because `TAlternateKey` supports `ref struct` keys such as `ReadOnlySpan<char>`, `AlternateEntry<TKey, TValue, TAlternateKey>`, `AlternateOccupiedEntry<TKey, TValue, TAlternateKey>`, and `AlternateVacantEntry<TKey, TValue, TAlternateKey>` are all `ref struct`s:

- **Not a C# 15 `[Union]`:** C# 15 union case types must be convertible to `object` and cannot be `ref struct`s (`CS8121`), so `switch` / `is` type patterns on `AlternateOccupiedEntry` and `AlternateVacantEntry` are not supported. Use fluent methods, untyped property patterns (e.g. `if (lookup.Entry(span) is { IsOccupied: true } e)`), `TryGetOccupied`, `TryGetVacant`, or synchronous `Match` instead.
- **No `async` / `await` across `AlternateOccupiedEntry` or `AlternateVacantEntry`:** Unlike `OccupiedEntry` and `VacantEntry` (which are regular `struct`s), alternate case types hold a `ref struct` key and cannot cross `await` / `yield return` boundaries, be boxed, or be captured in `async` lambdas.
- **Requires .NET 9+:** `Dictionary<TKey, TValue>.AlternateLookup<TAlternateKey>` is only available on `net9.0` and later.

### Thread Safety

`Dictionary<TKey, TValue>` is not thread-safe. In concurrent scenarios, protect access with synchronization or use a concurrent collection.

## Potential Features

- Support for `ConcurrentDictionary<TKey, TValue>` and other dictionary-like collections

## License

DictionaryEntry is [MIT Licensed](https://github.com/kjhickman/DictionaryEntry/blob/main/LICENSE).
