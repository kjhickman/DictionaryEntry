using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DictionaryEntry;

/// <summary>
/// Provides direct, zero-overhead access to <see cref="Dictionary{TKey, TValue}"/> internals via
/// <see cref="UnsafeAccessorAttribute"/> so vacant insertions reuse the already-computed hash code
/// and <see cref="OccupiedEntry{TKey, TValue}"/> / <see cref="AlternateOccupiedEntry{TKey, TValue, TAlternateKey}"/>
/// update or remove the entry slot in O(1) when the dictionary has not been structurally modified.
/// </summary>
internal static class DictionaryAccess<TKey, TValue> where TKey : notnull
{
    internal struct RawEntry
    {
        public uint hashCode;
        public int next;
        public TKey key;
        public TValue value;
    }

    private sealed class RawDictionary
    {
#pragma warning disable CS0649 // Populated by Dictionary<TKey, TValue>; accessed via Unsafe.As
        public int[]? _buckets;
        public RawEntry[]? _entries;
#pragma warning restore CS0649
    }

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_buckets")]
    public static extern ref int[]? GetBuckets(Dictionary<TKey, TValue> dictionary);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_comparer")]
    public static extern ref IEqualityComparer<TKey>? GetComparer(Dictionary<TKey, TValue> dictionary);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_count")]
    public static extern ref int GetCount(Dictionary<TKey, TValue> dictionary);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_freeList")]
    public static extern ref int GetFreeList(Dictionary<TKey, TValue> dictionary);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_freeCount")]
    public static extern ref int GetFreeCount(Dictionary<TKey, TValue> dictionary);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_version")]
    public static extern ref int GetVersion(Dictionary<TKey, TValue> dictionary);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "GetBucket")]
    public static extern ref int GetBucket(Dictionary<TKey, TValue> dictionary, uint hashCode);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "Resize")]
    public static extern void Resize(Dictionary<TKey, TValue> dictionary);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static RawEntry[]? GetEntries(Dictionary<TKey, TValue> dictionary) =>
        Unsafe.As<RawDictionary>(dictionary)._entries;

    /// <summary>
    /// Packs <c>(_version &lt;&lt; 32) | (_count - _freeCount)</c> for an occupied dictionary.
    /// Because insertions/resizes/trims increment <c>_version</c>, <c>Remove</c> decreases <c>_count - _freeCount</c>,
    /// and <c>Clear</c> sets <c>_count - _freeCount</c> from <c>&gt;= 1</c> to <c>0</c>, any structural change
    /// to an occupied dictionary changes this 64-bit stamp.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong GetOccupiedStamp(Dictionary<TKey, TValue> dictionary) =>
        ((ulong)(uint)GetVersion(dictionary) << 32) | (uint)(GetCount(dictionary) - GetFreeCount(dictionary));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteOccupiedValue(Dictionary<TKey, TValue> dictionary, ulong location, TValue newValue) =>
        Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(GetEntries(dictionary)!), (nint)(uint)location).value = newValue;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TValue RemoveOccupied(Dictionary<TKey, TValue> dictionary, ulong location)
    {
        int index = (int)location;
        int prevOrBucket = (int)(location >> 32);
        ref RawEntry firstEntry = ref MemoryMarshal.GetArrayDataReference(GetEntries(dictionary)!);
        ref RawEntry target = ref Unsafe.Add(ref firstEntry, (nint)(uint)index);

        int next = target.next;
        TValue value = target.value;

        if (prevOrBucket < 0)
        {
            if (typeof(TKey).IsValueType)
            {
                GetBucket(dictionary, target.hashCode) = next + 1;
            }
            else
            {
                ref byte firstBucket = ref Unsafe.As<int, byte>(ref MemoryMarshal.GetArrayDataReference(GetBuckets(dictionary)!));
                Unsafe.As<byte, int>(ref Unsafe.AddByteOffset(ref firstBucket, (nint)(uint)~prevOrBucket)) = next + 1;
            }
        }
        else
        {
            Unsafe.Add(ref firstEntry, (nint)(uint)prevOrBucket).next = next;
        }

        target.next = -3 - GetFreeList(dictionary);

        if (RuntimeHelpers.IsReferenceOrContainsReferences<TKey>())
        {
            target.key = default!;
        }

        if (RuntimeHelpers.IsReferenceOrContainsReferences<TValue>())
        {
            target.value = default!;
        }

        GetFreeList(dictionary) = index;
        GetFreeCount(dictionary)++;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TValue RemoveOccupied(Dictionary<TKey, TValue> dictionary, ulong location, out TKey actualKey)
    {
        int index = (int)location;
        int prevOrBucket = (int)(location >> 32);
        ref RawEntry firstEntry = ref MemoryMarshal.GetArrayDataReference(GetEntries(dictionary)!);
        ref RawEntry target = ref Unsafe.Add(ref firstEntry, (nint)(uint)index);

        int next = target.next;
        actualKey = target.key;
        TValue value = target.value;

        if (prevOrBucket < 0)
        {
            if (typeof(TKey).IsValueType)
            {
                GetBucket(dictionary, target.hashCode) = next + 1;
            }
            else
            {
                ref byte firstBucket = ref Unsafe.As<int, byte>(ref MemoryMarshal.GetArrayDataReference(GetBuckets(dictionary)!));
                Unsafe.As<byte, int>(ref Unsafe.AddByteOffset(ref firstBucket, (nint)(uint)~prevOrBucket)) = next + 1;
            }
        }
        else
        {
            Unsafe.Add(ref firstEntry, (nint)(uint)prevOrBucket).next = next;
        }

        target.next = -3 - GetFreeList(dictionary);

        if (RuntimeHelpers.IsReferenceOrContainsReferences<TKey>())
        {
            target.key = default!;
        }

        if (RuntimeHelpers.IsReferenceOrContainsReferences<TValue>())
        {
            target.value = default!;
        }

        GetFreeList(dictionary) = index;
        GetFreeCount(dictionary)++;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OccupiedEntry<TKey, TValue> CreateOccupied(
        Dictionary<TKey, TValue> dictionary,
        TKey key,
        TValue value,
        ulong location) =>
        new(dictionary, key, value, location, GetOccupiedStamp(dictionary));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static OccupiedEntry<TKey, TValue> CreateOccupiedUnstamped(
        Dictionary<TKey, TValue> dictionary,
        TKey key,
        TValue value) =>
        new(dictionary, key, value, ulong.MaxValue, 0);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AlternateOccupiedEntry<TKey, TValue, TAlternateKey> CreateAlternateOccupied<TAlternateKey>(
        Dictionary<TKey, TValue>.AlternateLookup<TAlternateKey> lookup,
        TAlternateKey key,
        TValue value,
        ulong location)
        where TAlternateKey : notnull, allows ref struct
    {
        var dictionary = lookup.Dictionary;
        return new AlternateOccupiedEntry<TKey, TValue, TAlternateKey>(
            lookup,
            key,
            value,
            location,
            GetOccupiedStamp(dictionary));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AlternateOccupiedEntry<TKey, TValue, TAlternateKey> CreateAlternateOccupiedUnstamped<TAlternateKey>(
        Dictionary<TKey, TValue>.AlternateLookup<TAlternateKey> lookup,
        TAlternateKey key,
        TValue value)
        where TAlternateKey : notnull, allows ref struct =>
        new(lookup, key, value, ulong.MaxValue, 0);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref TValue FindValue(
        Dictionary<TKey, TValue> dictionary,
        TKey key,
        out ulong hashAndVersion)
    {
        if (key is null)
        {
            ThrowKeyNull();
        }

        if (GetBuckets(dictionary) is null)
        {
            hashAndVersion = 0;
            return ref Unsafe.NullRef<TValue>();
        }

        uint hashCode;
        IEqualityComparer<TKey>? comparer = GetComparer(dictionary);
        if (typeof(TKey).IsValueType && comparer is null)
        {
            hashCode = (uint)EqualityComparer<TKey>.Default.GetHashCode(key);
            int i = GetBucket(dictionary, hashCode) - 1;
            RawEntry[] entries = GetEntries(dictionary)!;
            int last = -1;
            uint collisionCount = 0;
            while ((uint)i < (uint)entries.Length)
            {
                ref RawEntry entry = ref entries[i];
                if (entry.hashCode == hashCode && EqualityComparer<TKey>.Default.Equals(entry.key, key))
                {
                    hashAndVersion = ((ulong)(uint)last << 32) | (uint)i;
                    return ref entry.value;
                }

                last = i;
                i = entry.next;
                collisionCount++;
                if (collisionCount > (uint)entries.Length)
                {
                    ThrowConcurrentOperations();
                }
            }
        }
        else
        {
            hashCode = (uint)comparer!.GetHashCode(key);
            ref int bucket = ref GetBucket(dictionary, hashCode);
            int i = bucket - 1;
            RawEntry[] entries = GetEntries(dictionary)!;
            if ((uint)i < (uint)entries.Length)
            {
                int prevOrBucket = typeof(TKey).IsValueType
                    ? -1
                    : ~(int)Unsafe.ByteOffset(
                        ref Unsafe.As<int, byte>(ref MemoryMarshal.GetArrayDataReference(GetBuckets(dictionary)!)),
                        ref Unsafe.As<int, byte>(ref bucket));
                uint collisionCount = 0;
                do
                {
                    ref RawEntry entry = ref entries[i];
                    if (entry.hashCode == hashCode && comparer.Equals(entry.key, key))
                    {
                        hashAndVersion = ((ulong)(uint)prevOrBucket << 32) | (uint)i;
                        return ref entry.value;
                    }

                    prevOrBucket = i;
                    i = entry.next;
                    collisionCount++;
                    if (collisionCount > (uint)entries.Length)
                    {
                        ThrowConcurrentOperations();
                    }
                } while ((uint)i < (uint)entries.Length);

                if (typeof(TKey) == typeof(string) && collisionCount > 100)
                {
                    // Force AddVacant onto the BCL path so NonRandomizedStringEqualityComparer can upgrade to randomized hashing.
                    hashAndVersion = ((ulong)~(uint)GetVersion(dictionary) << 32) | hashCode;
                    return ref Unsafe.NullRef<TValue>();
                }
            }
        }

        hashAndVersion = ((ulong)(uint)GetVersion(dictionary) << 32) | hashCode;
        return ref Unsafe.NullRef<TValue>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref TValue FindAlternateValue<TAlternateKey>(
        Dictionary<TKey, TValue>.AlternateLookup<TAlternateKey> lookup,
        TAlternateKey key,
        out ulong hashAndVersion)
        where TAlternateKey : notnull, allows ref struct
    {
        Dictionary<TKey, TValue> dictionary = lookup.Dictionary;
        if (GetBuckets(dictionary) is null)
        {
            hashAndVersion = 0;
            return ref Unsafe.NullRef<TValue>();
        }

        var comparer = Unsafe.As<IAlternateEqualityComparer<TAlternateKey, TKey>>(GetComparer(dictionary)!);
        uint hashCode = (uint)comparer.GetHashCode(key);
        ref int bucket = ref GetBucket(dictionary, hashCode);
        int i = bucket - 1;
        RawEntry[] entries = GetEntries(dictionary)!;
        if ((uint)i < (uint)entries.Length)
        {
            int prevOrBucket = typeof(TKey).IsValueType
                ? -1
                : ~(int)Unsafe.ByteOffset(
                    ref Unsafe.As<int, byte>(ref MemoryMarshal.GetArrayDataReference(GetBuckets(dictionary)!)),
                    ref Unsafe.As<int, byte>(ref bucket));
            uint collisionCount = 0;
            do
            {
                ref RawEntry entry = ref entries[i];
                if (entry.hashCode == hashCode && comparer.Equals(key, entry.key))
                {
                    hashAndVersion = ((ulong)(uint)prevOrBucket << 32) | (uint)i;
                    return ref entry.value;
                }

                prevOrBucket = i;
                i = entry.next;
                collisionCount++;
                if (collisionCount > (uint)entries.Length)
                {
                    ThrowConcurrentOperations();
                }
            } while ((uint)i < (uint)entries.Length);

            if (typeof(TKey) == typeof(string) && collisionCount > 100)
            {
                hashAndVersion = ((ulong)~(uint)GetVersion(dictionary) << 32) | hashCode;
                return ref Unsafe.NullRef<TValue>();
            }
        }

        hashAndVersion = ((ulong)(uint)GetVersion(dictionary) << 32) | hashCode;
        return ref Unsafe.NullRef<TValue>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref TValue AddVacant(
        Dictionary<TKey, TValue> dictionary,
        TKey key,
        ulong hashAndVersion,
        bool overwriteIfStale = false)
    {
        int snapshotVersion = (int)(hashAndVersion >> 32);
        if (GetBuckets(dictionary) is null || GetVersion(dictionary) != snapshotVersion)
        {
            return ref AddStaleOrUninitialized(dictionary, key, overwriteIfStale);
        }

        return ref InsertWithKnownHash(dictionary, key, (uint)hashAndVersion);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref TValue AddAlternateVacant<TAlternateKey>(
        Dictionary<TKey, TValue>.AlternateLookup<TAlternateKey> lookup,
        TAlternateKey key,
        ulong hashAndVersion,
        bool overwriteIfStale = false)
        where TAlternateKey : notnull, allows ref struct
    {
        Dictionary<TKey, TValue> dictionary = lookup.Dictionary;
        int snapshotVersion = (int)(hashAndVersion >> 32);
        if (GetBuckets(dictionary) is null || GetVersion(dictionary) != snapshotVersion)
        {
            return ref AddAlternateStaleOrUninitialized(lookup, key, overwriteIfStale);
        }

        var comparer = Unsafe.As<IAlternateEqualityComparer<TAlternateKey, TKey>>(GetComparer(dictionary)!);
        TKey actualKey = comparer.Create(key);
        if (actualKey is null)
        {
            ThrowKeyNull();
        }

        return ref InsertWithKnownHash(dictionary, actualKey, (uint)hashAndVersion);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ref TValue InsertWithKnownHash(
        Dictionary<TKey, TValue> dictionary,
        TKey key,
        uint hashCode)
    {
        RawEntry[] entries = GetEntries(dictionary)!;
        int count = GetCount(dictionary);

        // Fast path: no free-list entries and spare capacity (collision threshold was already verified during FindValue).
        if (GetFreeCount(dictionary) == 0 && (uint)count < (uint)entries.Length)
        {
            ref int bucket = ref GetBucket(dictionary, hashCode);
            GetCount(dictionary) = count + 1;
            ref RawEntry entry = ref entries[count];
            entry.hashCode = hashCode;
            entry.next = bucket - 1;
            entry.key = key;
            entry.value = default!;
            bucket = count + 1;
            GetVersion(dictionary)++;
            return ref entry.value;
        }

        return ref InsertWithKnownHashSlow(dictionary, key, hashCode);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ref TValue InsertWithKnownHashSlow(
        Dictionary<TKey, TValue> dictionary,
        TKey key,
        uint hashCode)
    {
        RawEntry[] entries = GetEntries(dictionary)!;
        int index;
        ref int freeCount = ref GetFreeCount(dictionary);
        if (freeCount > 0)
        {
            index = GetFreeList(dictionary);
            GetFreeList(dictionary) = -3 - entries[index].next;
            freeCount--;
        }
        else
        {
            int count = GetCount(dictionary);
            if (count == entries.Length)
            {
                Resize(dictionary);
                entries = GetEntries(dictionary)!;
            }

            index = count;
            GetCount(dictionary) = count + 1;
        }

        ref int bucket = ref GetBucket(dictionary, hashCode);
        ref RawEntry entry = ref entries[index];
        entry.hashCode = hashCode;
        entry.next = bucket - 1;
        entry.key = key;
        entry.value = default!;
        bucket = index + 1;
        GetVersion(dictionary)++;

        return ref entry.value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ref TValue AddStaleOrUninitialized(
        Dictionary<TKey, TValue> dictionary,
        TKey key,
        bool overwriteIfStale)
    {
        ref var slot = ref CollectionsMarshal.GetValueRefOrAddDefault(dictionary, key, out var existed);
        if (existed && !overwriteIfStale)
        {
            ThrowKeyAlreadyAdded(key);
        }

        return ref slot!;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ref TValue AddAlternateStaleOrUninitialized<TAlternateKey>(
        Dictionary<TKey, TValue>.AlternateLookup<TAlternateKey> lookup,
        TAlternateKey key,
        bool overwriteIfStale)
        where TAlternateKey : notnull, allows ref struct
    {
        ref var slot = ref CollectionsMarshal.GetValueRefOrAddDefault(lookup, key, out var existed);
        if (existed && !overwriteIfStale)
        {
            ThrowAlternateKeyAlreadyAdded(lookup.Dictionary, key);
        }

        return ref slot!;
    }

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowKeyNull() => throw new ArgumentNullException("key");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowConcurrentOperations() =>
        throw new InvalidOperationException("Operations that change non-concurrent collections must have exclusive access.");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowKeyAlreadyAdded(TKey key) =>
        throw new ArgumentException($"An item with the same key has already been added. Key: {key}");

    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowAlternateKeyAlreadyAdded<TAlternateKey>(
        Dictionary<TKey, TValue> dictionary,
        TAlternateKey key)
        where TAlternateKey : notnull, allows ref struct
    {
        if (dictionary.Comparer is IAlternateEqualityComparer<TAlternateKey, TKey> altComparer)
        {
            throw new ArgumentException($"An item with the same key has already been added. Key: {altComparer.Create(key)}");
        }

        throw new ArgumentException("An item with the same key has already been added.");
    }
}
