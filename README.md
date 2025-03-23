# DictionaryEntry

Dictionary manipulation with a fluent, expressive syntax. Inspired by Rust's "Entry" API.

## Overview

DictionaryEntry is a lightweight library that brings an entry API pattern to C# dictionaries, allowing for more ergonomic dictionary operations. Instead of repeatedly looking up keys and checking if they exist, you get a unified entry object that represents either an existing entry (occupied) or a non-existing entry (vacant).

## Features

- Eliminate repetitive dictionary key lookups
- Fluent API for dictionary manipulation
- Efficient reference access to dictionary values
- Pattern matching for occupied/vacant entries
- Clean, expressive syntax for common operations
- Performance nearly matching traditional Dictionary operations in most cases (see [benchmarks](#benchmarks))

## Installation

```
dotnet add package DictionaryEntry
```

## Examples

The library provides three main types:
- `Entry<TKey, TValue>`: The initial entry point that can be either occupied or vacant
- `OccupiedEntry<TKey, TValue>`: Represents an entry for an existing key
- `VacantEntry<TKey, TValue>`: Represents an entry for a non-existing key

The core functionality revolves around the `.Entry(key)` extension method that lets you work with dictionary entries in a more fluent way.

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

// Using DictionaryEntry
scores.Entry(player).AndModify(score => score + points).OrInsert(points);
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

### Pattern Matching

```csharp
// Traditional approach
if (users.TryGetValue(userId, out var user))
{
    // Handle existing user
    ProcessExistingUser(user);
}
else
{
    // Handle new user
    var newUser = CreateNewUser(userId);
    users[userId] = newUser;
}

// Using DictionaryEntry
users.Entry(userId).Match(
    occupied => ProcessExistingUser(occupied.Value()),
    vacant => vacant.Insert(CreateNewUser(userId))
);
```

### Pattern Matching with Return Values

```csharp
string status = users.Entry(userId).Match(
    occupied => $"User found: {occupied.Value().Name}",
    vacant => "User not found"
);
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

The library is designed with performance in mind. Performance is usually very close to using Dictionary the traditional way. Full benchmark results comparing traditional dictionary operations with DictionaryEntry operations are available in the benchmarks project.

### Key benchmarks

Here are a few of the more common scenarios:

```
BenchmarkDotNet v0.14.0, Windows 11 (10.0.26100.3476)
AMD Ryzen 7 7800X3D, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.100-preview.1.25120.13
  [Host]     : .NET 9.0.3 (9.0.325.11113), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-LEFTJB : .NET 9.0.3 (9.0.325.11113), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

InvocationCount=10000000  
```
Get-or-Add
```
| Method                                   | Mean      | Error     | StdDev    | Median    | Allocated |
| GetOrAdd_Traditional_Exists              |  2.514 ns | 0.0242 ns | 0.0189 ns |  2.507 ns |         - |
| GetOrAdd_Traditional_NotExists           |  2.866 ns | 0.0334 ns | 0.0260 ns |  2.864 ns |         - |
| GetOrAdd_Entry_Exists                    |  2.793 ns | 0.0222 ns | 0.0173 ns |  2.791 ns |         - |
| GetOrAdd_Entry_NotExists                 |  2.552 ns | 0.0337 ns | 0.0281 ns |  2.543 ns |         - |
```

Get-or-Default
```
| Method                                   | Mean      | Error     | StdDev    | Median    | Allocated |
| DefaultValue_Traditional_Exists          |  6.229 ns | 0.0248 ns | 0.0207 ns |  6.230 ns |         - |
| DefaultValue_Traditional_NotExists       |  5.738 ns | 0.0328 ns | 0.0290 ns |  5.740 ns |         - |
| DefaultValue_Entry_Exists                |  6.179 ns | 0.0233 ns | 0.0182 ns |  6.180 ns |         - |
| DefaultValue_Entry_NotExists             |  5.815 ns | 0.0516 ns | 0.0483 ns |  5.803 ns |         - |
```

Increment counter
```
| Method                                   | Mean      | Error     | StdDev    | Median    | Gen0   | Allocated |
| IncrementCounter_Traditional_Exists      | 13.083 ns | 0.0494 ns | 0.0549 ns | 13.063 ns |      - |         - |
| IncrementCounter_Traditional_NotExists   | 12.340 ns | 0.1864 ns | 0.2551 ns | 12.244 ns |      - |         - |
| IncrementCounter_Entry_Exists            | 15.437 ns | 0.0683 ns | 0.0571 ns | 15.420 ns |      - |         - |
| IncrementCounter_Entry_NotExists         | 15.053 ns | 0.0508 ns | 0.0424 ns | 15.039 ns |      - |         - |
| IncrementByAmount_Traditional_Exists     | 13.169 ns | 0.0606 ns | 0.0506 ns | 13.159 ns |      - |         - |
| IncrementByAmount_Traditional_NotExists  | 12.519 ns | 0.2666 ns | 0.3823 ns | 12.295 ns |      - |         - |
| IncrementByAmount_Entry_Exists           | 17.795 ns | 0.0663 ns | 0.0620 ns | 17.796 ns | 0.0017 |      88 B |
| IncrementByAmount_Entry_NotExists        | 17.074 ns | 0.2999 ns | 0.2805 ns | 16.958 ns | 0.0017 |      88 B |
```

To run the benchmarks yourself:

```bash
cd src/DictionaryEntry.Benchmarks
dotnet run -c Release
```

For detailed benchmark results and analysis, see the [Benchmark Results](./benchmarks/results-0.1.0.md) document.

### Performance Considerations with Lambdas

When using methods that accept lambda expressions like `AndModify` or `OrInsertWith`, be aware that capturing local variables in these lambdas can cause heap allocations. This is important to understand in performance-critical scenarios:

```csharp
// This method allocates a closure on the heap because it captures the 'amount' parameter
private void IncrementByAmountEntry(string key, int amount)
{
    _dictionary.Entry(key).AndModify(count => count + amount).OrInsert(amount);
}

// This method doesn't allocate because it uses a simple non-capturing lambda
private void IncrementByOneEntry(string key)
{
    _dictionary.Entry(key).AndModify(count => count + 1).OrInsert(1);
}
```

## Potential Features and Plans

### Potential Features
- Support for `ConcurrentDictionary<TKey, TValue>` and other dictionary-like collections
- Specialized overloads to optimize common scenarios such as incrementing a value

### Future plans
In the future when C# introduces [typed union structs](https://github.com/dotnet/csharplang/blob/main/proposals/TypeUnions.md#specialized---union-structs) I may create a new version to take full advantage of that.

In rust, I like the `if let` syntax where you can do this:
```rust
if let Entry::Occupied(entry) = map.entry(key) {
    println!("Found: {} -> {}", key, entry.get());
}
```

and I'd like to be able to do similar in C# like this:
```csharp
if (dict.Entry(key) is OccupiedEntry occupied)
{
    Console.WriteLine($"Found: {occupied.Key()} -> {occupied.Value()}");
}
```

But as far as I know, it's not currently possible without using a class + inheritance, which would result in too many allocations for this use-case.

For now, `TryGetOccupied` gets pretty close:
```csharp
if (dict.Entry(key).TryGetOccupied(out var occupied))
{
    Console.WriteLine($"Found: {occupied.Key()} -> {occupied.Value()}");
}
```

## License

DictionaryEntry is [MIT Licensed](https://github.com/kjhickman/DictionaryEntry/blob/main/LICENSE).