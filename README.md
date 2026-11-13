# DictionaryEntry

`DictionaryEntry` brings Rust's [`entry()`](https://doc.rust-lang.org/std/collections/struct.HashMap.html#method.entry) API to C# `Dictionary<TKey, TValue>`, replacing multi-line `TryGetValue` + indexer boilerplate with expressive, **single-lookup** operations.

Because every operation hashes and probes the dictionary only once—and avoids rehashing on inserts and updates—`DictionaryEntry` matches hand-written `Dictionary` code on cache hits and runs **25% to ~2x faster** on misses and updates, with **zero allocations**, NativeAOT compatibility, `ReadOnlySpan<char>` lookups, and zero-boxing **C# 15 `[Union]`** pattern matching.

```csharp
// Increment or initialize in 1 lookup (instead of TryGetValue + dict[key] = ...)
scores.Entry(player).OrDefault() += points;

// Lazily initialize a collection and mutate it in place
groups.Entry(category).OrInsertWith(static () => []).Add(item);

// Zero-allocation ReadOnlySpan<char> lookup on Dictionary<string, int>
counts.Entry(line.AsSpan(0, 5)).AndModify(static c => c + 1).OrInsert(1);

// Exhaustive, zero-boxing C# 15 union switch
string label = users.Entry(userId) switch
{
    OccupiedEntry<Guid, User> occupied => occupied.Value.Name,
    VacantEntry<Guid, User> => "Guest",
};
```

## Table of Contents

- [Installation](#installation)
- [Common Use Cases](#common-use-cases)
  - [Insert-or-Update (Counters and Accumulators)](#insert-or-update-counters-and-accumulators)
  - [Get-or-Initialize (Caching and Grouping)](#get-or-initialize-caching-and-grouping)
  - [In-Place Struct Mutation](#in-place-struct-mutation)
  - [Zero-Allocation Span Lookups](#zero-allocation-span-lookups)
- [Benchmarks](#benchmarks)
- [Advanced Features](#advanced-features)
  - [C# 15 Union Pattern Matching](#c-15-union-pattern-matching)
  - [Pattern Matching Without C# 15](#pattern-matching-without-c-15)
  - [AlternateLookup and Custom Span Keys](#alternatelookup-and-custom-span-keys)
  - [API Quick Reference](#api-quick-reference)
- [Performance Considerations](#performance-considerations)
- [Limitations and Caveats](#limitations-and-caveats)
- [License](#license)

## Installation

```bash
dotnet add package DictionaryEntry
```

Targets `net9.0`, `net10.0`, and `net11.0` (`IsAotCompatible = true`).

## Common Use Cases

### Insert-or-Update (Counters and Accumulators)

Standard C# upserts hash and probe the dictionary twice—once in `TryGetValue` and again in the indexer setter:

```csharp
// Traditional C# (2 lookups, 9 lines)
if (scores.TryGetValue(player, out var score))
{
    scores[player] = score + points;
}
else
{
    scores[player] = points;
}

// DictionaryEntry: live ref return (1 lookup, 0 allocations)
scores.Entry(player).OrDefault() += points;

// DictionaryEntry: distinct update vs. initial value (1 lookup, 0 allocations)
scores.Entry(player)
    .AndModify(points, static (score, pts) => score + pts)
    .OrInsert(initialScore);
```

### Get-or-Initialize (Caching and Grouping)

`OrInsert`, `OrInsertWith`, and `OrInsertWithKey` return a live `ref TValue` into the dictionary, evaluating factory delegates only when the key is absent:

```csharp
// Simple default value
int port = config.Entry("port").OrInsert(8080);

// Lazy factory (only invoked when missing) + immediate mutation
buckets.Entry(routeKey)
    .OrInsertWith(static () => new List<Request>())
    .Add(request);

// Key-aware factory with state (0 closure allocations)
int scaled = cache.Entry(key)
    .OrInsertWithKey(multiplier, static (k, mult) => Compute(k, mult));
```

### In-Place Struct Mutation

Mutating a value-type entry in a standard `Dictionary<TKey, TStruct>` copies the struct out, modifies the copy, and hashes the key a second time to write it back. `DictionaryEntry` mutates the slot directly in place with zero struct copies:

```csharp
// Upsert + in-place struct field mutation via RefAction
playerStats.Entry(playerId)
    .AndModify(damage, static (ref stats, dmg) =>
    {
        stats.HitsLanded++;
        stats.TotalDamage += dmg;
    })
    .OrInsert(new PlayerStats { HitsLanded = 1, TotalDamage = damage });

// Update in place ONLY if the key already exists
if (playerStats.Entry(playerId) is { IsOccupied: true } entry)
{
    ref var stats = ref entry.ValueRef;
    stats.HitsLanded++;
    stats.TotalDamage += damage;
}
```

### Zero-Allocation Span Lookups

For `Dictionary<string, TValue>`, pass a `ReadOnlySpan<char>` (or `Span<char>` / `char[]`) directly to `.Entry(span)`. Cache hits allocate **0 bytes**, and cache misses materialize the `string` key only once when inserting:

```csharp
var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

ReadOnlySpan<char> line = "Content-Length: 512";
ReadOnlySpan<char> name = line[..line.IndexOf(':')];

headers.Entry(name).OrDefault()++;
```

## Benchmarks

Benchmarked with BenchmarkDotNet v0.15.8 on `.NET 11.0` (Apple M1 Pro Arm64, RyuJIT). Comparing idiomatic hand-written `TryGetValue` + indexer code against `DictionaryEntry`:

| Scenario | Key / Value | Hand-Written | `DictionaryEntry` | vs. Hand-Written | Allocated |
|---|---|---:|---:|---:|---:|
| **Get-or-Add (Hit)** | `int, int` | `1.84 ns` | **`1.88 ns`** | ~1.0x (parity) | `0 B` |
| **Get-or-Add (Miss)** | `int, int` | `5.26 ns` | **`3.78 ns`** | **1.39x faster** | `0 B` |
| **Get-or-Default (Hit)** | `string, string?` | `4.25 ns` | **`4.37 ns`** | ~1.0x (parity) | `0 B` |
| **Get-or-Default (Miss)** | `string, string?` | `11.04 ns` | **`7.36 ns`** | **1.50x faster** | `0 B` |
| **Lazy Factory (Miss)** | `string, List<int>` | `11.89 ns` | **`8.83 ns`** | **1.35x faster** | `0 B`* |
| **Update Existing (Hit)** | `string, int` | `9.20 ns` | **`4.30 ns`** | **2.14x faster** | `0 B` |
| **Increment / Upsert (Hit)** | `string, int` | `10.03 ns` | **`4.61 ns`** | **2.18x faster** | `0 B` |
| **Union `switch` Upsert (Hit)** | `string, int` | `10.03 ns` | **`5.24 ns`** | **1.92x faster** | `0 B` |
| **Union `switch` Upsert (Miss)** | `string, int` | `11.70 ns` | **`8.79 ns`** | **1.33x faster** | `0 B` |
| **Span Lookup (Hit)** | `ReadOnlySpan<char>` | `12.40 ns` (`ToString`) | **`4.50 ns`** | **2.75x faster** | **`0 B`** vs `32 B` |

*\*Excludes the `List<int>` intentionally created by the caller's factory.*

Why is `DictionaryEntry` faster on misses and mutations?
- **Single key hash & bucket probe:** `TryGetValue` followed by `dict[key] = value` hashes the key and walks the bucket chain twice. `Entry` caches the slot reference on hits and the computed hash code on misses.
- **O(1) `OccupiedEntry.Insert`:** `OccupiedEntry` records the entry's byte offset and a structural version stamp, writing updated values directly to the slot without re-probing.
- **Zero-boxing structs:** `Entry<TKey, TValue>` is a stack-only `ref struct` implementing C# 15's non-boxing `TryGetValue` union pattern rather than `object`-backed storage.

For the full benchmark suite and instructions to run them locally, see [**src/DictionaryEntry.Benchmarks/README.md**](https://github.com/kjhickman/DictionaryEntry/blob/main/src/DictionaryEntry.Benchmarks/README.md).

## Advanced Features

### C# 15 Union Pattern Matching

`Entry<TKey, TValue>` is decorated with `[Union]` over `OccupiedEntry<TKey, TValue>` and `VacantEntry<TKey, TValue>` (and implements `System.Runtime.CompilerServices.IUnion` on .NET 11+), enabling compiler-verified exhaustive `switch` expressions, `switch` statements, and `is` patterns without boxing:

```csharp
// Exhaustive switch expression (no '_' discard arm needed)
string tier = scores.Entry(player) switch
{
    OccupiedEntry<string, int> { Value: >= 1000 } => "Gold",
    OccupiedEntry<string, int>(_, >= 100) => "Silver", // positional Deconstruct(out key, out value)
    OccupiedEntry<string, int> => "Bronze",
    VacantEntry<string, int> => "Unranked",
};

// 'is' pattern matching (Rust 'if let' style)
if (users.Entry(userId) is OccupiedEntry<Guid, User> occupied)
{
    Console.WriteLine($"Found {occupied.Key}: {occupied.Value.Name}");
}

// Switch statement with local variables (no delegates or closures)
switch (users.Entry(userId))
{
    case OccupiedEntry<Guid, User> existing:
        existing.Insert(existing.Value with { LastSeen = now });
        break;
    case VacantEntry<Guid, User> vacant:
        vacant.Insert(CreateNewUser(vacant.Key, now));
        break;
}
```

### Pattern Matching Without C# 15

On C# 13 / 14—or when you prefer generic type inference over spelling out `<TKey, TValue>` in case patterns—use `Match`, `TryGetOccupied`, and `TryGetVacant`:

```csharp
string status = users.Entry(userId).Match(
    occupied => $"Found: {occupied.Value.Name}",
    vacant => $"Created: {vacant.Insert(CreateNewUser(vacant.Key)).Name}"
);

if (users.Entry(userId).TryGetOccupied(out var occupied))
{
    // Remove and return the value in a single flow
    User removed = occupied.Remove();
}
```

### AlternateLookup and Custom Span Keys

While `dict.Entry(ReadOnlySpan<char>)` is convenient for one-off lookups on `Dictionary<string, TValue>`, tight loops and custom alternate key types (such as UTF-8 `ReadOnlySpan<byte>` via `IAlternateEqualityComparer<ReadOnlySpan<byte>, string>`) can hoist `GetAlternateLookup<TAlternateKey>()` once outside the loop:

```csharp
var counts = new Dictionary<string, int>(StringComparer.Ordinal);
var lookup = counts.GetAlternateLookup<ReadOnlySpan<char>>();

ReadOnlySpan<char> csv = "apple,banana,apple,cherry";
foreach (var range in csv.Split(','))
{
    lookup.Entry(csv[range]).AndModify(static c => c + 1).OrInsert(1);
}

// Remove via alternate key and recover both the stored string key and value
if (lookup.Entry("banana".AsSpan()).TryGetOccupied(out var banana))
{
    (string storedKey, int finalCount) = banana.RemoveEntry();
}
```

### API Quick Reference

| Type | Kind | Key Members |
|---|---|---|
| `Entry<TKey, TValue>` | `ref struct` (`[Union]`) | `Key`, `IsOccupied`, `IsVacant`, `ValueRef`, `OrInsert(v)`, `OrDefault()`, `OrInsertWith(...)`, `OrInsertWithKey(...)`, `AndModify(...)`, `InsertEntry(v)`, `ToOccupied()`, `ToVacant()`, `TryGetOccupied(out ...)`, `TryGetVacant(out ...)`, `Match(...)` |
| `OccupiedEntry<TKey, TValue>` | `readonly struct` | `Key`, `Value` *(snapshot)*, `Deconstruct(out key, out value)`, `Insert(newValue)`, `Remove()`, `RemoveEntry()` |
| `VacantEntry<TKey, TValue>` | `readonly struct` | `Key`, `Insert(value)`, `InsertEntry(value)` |
| `AlternateEntry<TKey, TValue, TAlt>` | `ref struct` | Same fluent surface as `Entry<TKey, TValue>`, keyed by `TAlt` (`allows ref struct`) |
| `AlternateOccupiedEntry<...>` | `readonly ref struct` | `Key`, `Value` *(snapshot)*, `Deconstruct`, `Insert(newValue)`, `Remove()`, `RemoveEntry()` *(returns `(TKey, TValue)`)* |
| `AlternateVacantEntry<...>` | `readonly ref struct` | `Key`, `Insert(value)`, `InsertEntry(value)` |

## Performance Considerations

Methods taking delegates (`AndModify`, `OrInsertWith`, `OrInsertWithKey`, and `Match`) are **zero-allocation** when passed a `static` lambda or non-capturing lambda, because C# caches a singleton delegate instance.

If a lambda **captures local variables or `this`**, C# allocates a closure object (`24–88 B`) even on cache hits before the method runs:

```csharp
// Avoid on hot paths: captures 'amount', allocating a closure on every call
scores.Entry(key).AndModify(c => c + amount).OrInsert(amount);

// Zero-allocation Option 1: TState overload + static lambda (also supports ref struct / Span state)
scores.Entry(key).AndModify(amount, static (c, amt) => c + amt).OrInsert(amount);

// Zero-allocation Option 2: ref return
scores.Entry(key).OrDefault() += amount;

// Zero-allocation Option 3: C# 15 union switch
switch (scores.Entry(key))
{
    case OccupiedEntry<string, int> occupied:
        occupied.Insert(occupied.Value + amount);
        break;
    case VacantEntry<string, int> vacant:
        vacant.Insert(amount);
        break;
}
```

## Limitations and Caveats

### `ref struct` and `async` / `await`

`Entry<TKey, TValue>` holds a managed `ref TValue` into the dictionary, so `Entry` itself cannot be boxed, stored in heap fields, or held across `await` boundaries.

However, **`OccupiedEntry<TKey, TValue>` and `VacantEntry<TKey, TValue>` are regular `readonly struct`s** (not `ref struct`s). Unwrapping `Entry` in a C# 15 `switch` before hitting `await` works seamlessly:

```csharp
// Works on C# 15: Entry is unwrapped into OccupiedEntry / VacantEntry before 'await'
var user = cache.Entry(userId) switch
{
    OccupiedEntry<Guid, User> occupied => occupied.Value,
    VacantEntry<Guid, User> vacant => vacant.Insert(await db.LoadUserAsync(vacant.Key)),
};

// Works on C# 13 / 14: return a Task from Match and await it
var user = await cache.Entry(userId).Match(
    occupied => Task.FromResult(occupied.Value),
    async vacant => vacant.Insert(await db.LoadUserAsync(vacant.Key))
);
```

*(Note: `AlternateOccupiedEntry` and `AlternateVacantEntry` hold `TAlternateKey`—such as `ReadOnlySpan<char>`—so all three `Alternate*` types are `ref struct`s, cannot cross `await` boundaries, and cannot participate in C# 15 `[Union]` type patterns (`CS8121`).)*

### C# 15 Union Nuances

- **Compiler version:** Native union `switch` / `is` patterns require compiling with C# 15 (`.NET 11` SDK; projects targeting `net9.0` or `net10.0` on the `.NET 11` SDK can opt in with `<LangVersion>15.0</LangVersion>` or `<LangVersion>latest</LangVersion>`). All fluent methods, `Match`, and `TryGet*` work on earlier C# versions.
- **Explicit type arguments in case patterns:** C# 15 unions are type unions rather than nominal enum variants, so `<TKey, TValue>` must be written in `OccupiedEntry<TKey, TValue>` / `VacantEntry<TKey, TValue>` type patterns (or aliased with `using`).
- **Live `Entry` vs. snapshot cases:** `OccupiedEntry.Value` is a snapshot captured when the entry was obtained and is not updated by `Insert`. Converting an `OccupiedEntry` or `VacantEntry` back to `Entry<TKey, TValue>` performs a fresh lookup against the dictionary's current state.

### External Mutation, Runtime Internals, and Thread Safety

- **Do not mutate the dictionary externally while holding an `Entry`:** Because `Entry` and `AlternateEntry` cache an interior value reference into the dictionary's storage, mutating or resizing the same `Dictionary<TKey, TValue>` outside the entry's own methods (including inside an `AndModify` callback) invalidates that entry instance.
- **Runtime internals:** `DictionaryEntry` uses `[UnsafeAccessor]` to avoid rehashing keys on vacant insertions and to perform O(1) slot writes on `OccupiedEntry.Insert`. It is verified on `.NET 9`, `.NET 10`, and `.NET 11`; newer `.NET` releases may require an updated package version if `Dictionary<TKey, TValue>`'s internal layout changes.
- **Thread safety:** `Dictionary<TKey, TValue>` is not thread-safe. Synchronize concurrent access externally or use a concurrent collection.

## License

DictionaryEntry is [MIT Licensed](https://github.com/kjhickman/DictionaryEntry/blob/main/LICENSE).
