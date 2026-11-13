namespace DictionaryEntry.Tests.AdvancedUsage;

public class DictionaryInternalsTests
{
    [Fact]
    public void FreeListReuse_AndResize_MaintainExactParityWithStandardDictionary()
    {
        var actual = new Dictionary<string, string?>(capacity: 4);
        var expected = new Dictionary<string, string?>(capacity: 4);
        var lookup = actual.GetAlternateLookup<ReadOnlySpan<char>>();

        // 1. Insert across multiple resizes via OrInsert, VacantEntry.Insert, and AlternateLookup
        for (var i = 0; i < 80; i++)
        {
            var key = $"item_{i}";
            var val = i % 5 == 0 ? null : $"val_{i}";

            switch (i % 3)
            {
                case 0:
                    actual.Entry(key).OrInsert(val);
                    break;
                case 1:
                    actual.Entry(key).ToVacant().Insert(val);
                    break;
                default:
                    lookup.Entry(key.AsSpan()).OrInsert(val);
                    break;
            }

            expected[key] = val;
        }

        AssertDictionariesEqual(expected, actual);

        // 2. Remove every 2nd item to populate _freeList / _freeCount
        for (var i = 0; i < 80; i += 2)
        {
            var key = $"item_{i}";
            if (i % 4 == 0)
            {
                actual.Entry(key).ToOccupied().Remove();
            }
            else
            {
                actual.Remove(key);
            }

            expected.Remove(key);
        }

        AssertDictionariesEqual(expected, actual);

        // 3. Insert new keys to consume _freeList slots and then grow again
        for (var i = 80; i < 150; i++)
        {
            var key = $"item_{i}";
            var val = $"new_{i}";

            if ((i & 1) == 0)
            {
                actual.Entry(key).OrInsert(val);
            }
            else
            {
                lookup.Entry(key.AsSpan()).ToVacant().Insert(val);
            }

            expected[key] = val;
        }

        AssertDictionariesEqual(expected, actual);
    }

    [Fact]
    public void OccupiedInsert_SurvivesCompactingGcAndHandlesReferenceTypeWriteBarriers()
    {
        var dict = new Dictionary<string, string> { ["target"] = "initial" };
        var occupied = dict.Entry("target").ToOccupied();

        // Allocate pinned/unpinned objects and force a compacting Gen2 GC to encourage heap movement
        var anchor = new List<byte[]>();
        for (var i = 0; i < 200; i++)
        {
            anchor.Add(new byte[1024]);
        }
        anchor.RemoveRange(0, 100);
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);

        occupied.Insert(new string('x', 32));
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);

        Assert.Equal(new string('x', 32), dict["target"]);
        Assert.Equal("initial", occupied.Value);
        GC.KeepAlive(anchor);
    }

    [Fact]
    public void OccupiedInsert_FallsBackSafelyAcrossAllExternalStructuralMutations()
    {
        // 1. External Remove of ANOTHER key (freeCount changes, version unchanged)
        var d1 = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 };
        var occ1 = d1.Entry("a").ToOccupied();
        d1.Remove("b");
        occ1.Insert(10);
        Assert.Equal(10, d1["a"]);
        Assert.False(d1.ContainsKey("b"));

        // 2. External Remove of target key + Add of another key (Count unchanged, version changes, slot reused!)
        var d2 = new Dictionary<string, int>(capacity: 4) { ["a"] = 1, ["b"] = 2 };
        var occ2 = d2.Entry("a").ToOccupied();
        d2.Remove("a");
        d2.Add("c", 3); // reuses 'a's slot on _freeList!
        occ2.Insert(99);
        Assert.Equal(99, d2["a"]);
        Assert.Equal(3, d2["c"]); // 'c' must NOT have been overwritten by occ2's old _byteOffset!

        // 3. External Clear() when freeCount was 0 (version unchanged, Count becomes 0)
        var d3 = new Dictionary<string, int> { ["a"] = 1 };
        var occ3 = d3.Entry("a").ToOccupied();
        d3.Clear();
        occ3.Insert(42);
        Assert.Equal(42, d3["a"]);
        Assert.Single(d3);

        // 4. External EnsureCapacity / Resize (Count unchanged, version changes, _entries replaced)
        var d4 = new Dictionary<string, int>(capacity: 3) { ["a"] = 1 };
        var occ4 = d4.Entry("a").ToOccupied();
        d4.EnsureCapacity(200);
        occ4.Insert(77);
        Assert.Equal(77, d4["a"]);

        // 5. External TrimExcess (Count unchanged, version changes, _entries replaced)
        var d5 = new Dictionary<string, int>(capacity: 200) { ["a"] = 1 };
        var occ5 = d5.Entry("a").ToOccupied();
        d5.TrimExcess();
        occ5.Insert(88);
        Assert.Equal(88, d5["a"]);

        // 6. AlternateOccupiedEntry with Remove + Add slot reuse
        var d6 = new Dictionary<string, int>(StringComparer.Ordinal) { ["a"] = 1, ["b"] = 2 };
        var lookup = d6.GetAlternateLookup<ReadOnlySpan<char>>();
        var altOcc = lookup.Entry("a".AsSpan()).ToOccupied();
        d6.Remove("a");
        d6.Add("c", 3);
        altOcc.Insert(55);
        Assert.Equal(55, d6["a"]);
        Assert.Equal(3, d6["c"]);
    }

    [Fact]
    public void ReferenceTypeCollisions_Exceeding100Threshold_DelegateCleanlyAndPreserveAllEntries()
    {
        var dict = new Dictionary<RefCollidingKey, int>();
        for (var i = 0; i < 140; i++)
        {
            dict.Entry(new RefCollidingKey(i)).OrInsert(i * 10);
        }

        Assert.Equal(140, dict.Count);
        for (var i = 0; i < 140; i++)
        {
            var k = new RefCollidingKey(i);
            Assert.Equal(i * 10, dict[k]);
            Assert.True(dict.Entry(k) is OccupiedEntry<RefCollidingKey, int> { Value: var v } && v == i * 10);
        }
    }

    [Fact]
    public void StringAndAlternateCollisions_Exceeding100Threshold_DelegateCleanlyAndPreserveAllEntries()
    {
        var dict = new Dictionary<string, int>(CollidingSpanStringComparer.Instance);
        var lookup = dict.GetAlternateLookup<ReadOnlySpan<char>>();

        for (var i = 0; i < 140; i++)
        {
            var key = $"colliding_{i}";
            if ((i & 1) == 0)
            {
                dict.Entry(key).OrInsert(i * 3);
            }
            else
            {
                lookup.Entry(key.AsSpan()).OrInsert(i * 3);
            }
        }

        Assert.Equal(140, dict.Count);
        for (var i = 0; i < 140; i++)
        {
            var key = $"colliding_{i}";
            Assert.Equal(i * 3, dict[key]);
            Assert.Equal(i * 3, lookup.Entry(key.AsSpan()).ToOccupied().Value);
        }
    }

    [Fact]
    public void DiverseStructAndReferenceLayouts_PreserveExactFieldOffsetsAndGcWriteBarriers()
    {
        var smallKeyWideVal = new Dictionary<byte, Int128>();
        smallKeyWideVal.Entry((byte)7).OrInsert(new Int128(0x1122334455667788UL, 0x99AABBCCDDEEFF00UL));
        smallKeyWideVal.Entry((byte)7).ToOccupied().Insert(new Int128(0xCAFEBABEUL, 0xDEADBEEFUL));
        Assert.Equal(new Int128(0xCAFEBABEUL, 0xDEADBEEFUL), smallKeyWideVal[7]);

        var wideKeyMixedVal = new Dictionary<Guid, MixedLayoutValue>();
        var id = Guid.NewGuid();
        wideKeyMixedVal.Entry(id).OrInsert(new MixedLayoutValue(1, new string('a', 24), 100L));
        var occ = wideKeyMixedVal.Entry(id).ToOccupied();

        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        occ.Insert(new MixedLayoutValue(2, new string('b', 24), 200L));
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);

        Assert.Equal(new MixedLayoutValue(2, new string('b', 24), 200L), wideKeyMixedVal[id]);
    }

    [Fact]
    public void ValueTypeKey_WithCustomComparer_RespectsComparerForLookupAndInsert()
    {
        // Keys equal modulo 10
        var dict = new Dictionary<int, int>(Mod10Comparer.Instance);

        dict.Entry(3).OrInsert(100);
        Assert.True(dict.Entry(13).IsOccupied);
        dict.Entry(23).AndModify(static v => v + 50).OrInsert(0);

        Assert.Single(dict);
        Assert.Equal(150, dict[3]);
        Assert.Equal(150, dict[33]);
    }

    [Fact]
    public void VacantInsertions_IncrementVersionAndInvalidateActiveEnumerator()
    {
        var dict = new Dictionary<string, int> { ["a"] = 1 };
        using var enumerator = dict.GetEnumerator();
        Assert.True(enumerator.MoveNext());

        dict.Entry("b").OrInsert(2);

        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
    }

    private static void AssertDictionariesEqual<TKey, TValue>(
        Dictionary<TKey, TValue> expected,
        Dictionary<TKey, TValue> actual)
        where TKey : notnull
    {
        Assert.Equal(expected.Count, actual.Count);
        foreach (var (k, v) in expected)
        {
            Assert.True(actual.TryGetValue(k, out var actualVal));
            Assert.Equal(v, actualVal);
        }

        // Also verify actual's own enumerator yields exact Count items
        var enumeratedCount = 0;
        foreach (var (k, v) in actual)
        {
            Assert.Equal(expected[k], v);
            enumeratedCount++;
        }
        Assert.Equal(expected.Count, enumeratedCount);
    }

    private sealed record RefCollidingKey(int Id)
    {
        public override int GetHashCode() => 42;
    }

    private readonly record struct MixedLayoutValue(byte Tag, string Label, long Timestamp);

    private sealed class CollidingSpanStringComparer
        : IEqualityComparer<string>, IAlternateEqualityComparer<ReadOnlySpan<char>, string>
    {
        public static readonly CollidingSpanStringComparer Instance = new();
        public bool Equals(string? x, string? y) => string.Equals(x, y, StringComparison.Ordinal);
        public int GetHashCode(string obj) => 42;
        public bool Equals(ReadOnlySpan<char> alternate, string other) => alternate.SequenceEqual(other.AsSpan());
        public int GetHashCode(ReadOnlySpan<char> alternate) => 42;
        public string Create(ReadOnlySpan<char> alternate) => alternate.ToString();
    }

    private sealed class Mod10Comparer : IEqualityComparer<int>
    {
        public static readonly Mod10Comparer Instance = new();
        public bool Equals(int x, int y) => Math.Abs(x % 10) == Math.Abs(y % 10);
        public int GetHashCode(int obj) => Math.Abs(obj % 10);
    }
}
