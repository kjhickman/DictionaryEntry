using System.Runtime.CompilerServices;
using System.Text;

namespace DictionaryEntry.Tests.AdvancedUsage;

public class AlternateEntryTests
{
    [Fact]
    public void Entry_WithExistingAndMissingSpanKeys_ReportsOccupancyWithoutAllocating()
    {
        var dict = new Dictionary<string, int> { ["existing"] = 42 };
        var lookup = dict.GetAlternateLookup<ReadOnlySpan<char>>();

        var occupied = lookup.Entry("existing".AsSpan());
        Assert.True(occupied.IsOccupied);
        Assert.False(occupied.IsVacant);
        Assert.True(occupied.Key.SequenceEqual("existing"));

        var vacant = lookup.Entry("missing".AsSpan());
        Assert.False(vacant.IsOccupied);
        Assert.True(vacant.IsVacant);
        Assert.True(vacant.Key.SequenceEqual("missing"));
        Assert.Single(dict);
    }

    [Fact]
    public void OrInsert_OrDefault_OrInsertWith_OrInsertWithKey_InsertOnlyWhenMissing()
    {
        var dict = new Dictionary<string, int>(StringComparer.Ordinal) { ["a"] = 10 };
        var lookup = dict.GetAlternateLookup<ReadOnlySpan<char>>();

        // Existing hits do not overwrite or invoke factories
        var factoryCalled = false;
        Assert.Equal(10, lookup.Entry("a".AsSpan()).OrInsert(99));
        Assert.Equal(10, lookup.Entry("a".AsSpan()).OrDefault());
        Assert.Equal(10, lookup.Entry("a".AsSpan()).OrInsertWith(() => { factoryCalled = true; return 99; }));
        Assert.Equal(10, lookup.Entry("a".AsSpan()).OrInsertWith(5, _ => { factoryCalled = true; return 99; }));
        Assert.Equal(10, lookup.Entry("a".AsSpan()).OrInsertWithKey(_ => { factoryCalled = true; return 99; }));
        Assert.Equal(10, lookup.Entry("a".AsSpan()).OrInsertWithKey(5, (_, _) => { factoryCalled = true; return 99; }));
        Assert.False(factoryCalled);

        // Vacant insertions materialize the string key once and return live refs
        ref var b = ref lookup.Entry("bb".AsSpan()).OrInsert(20);
        b += 2;
        Assert.Equal(22, dict["bb"]);

        ref var c = ref lookup.Entry("ccc".AsSpan()).OrDefault();
        Assert.Equal(0, c);
        c = 30;
        Assert.Equal(30, dict["ccc"]);

        ref var d = ref lookup.Entry("dddd".AsSpan()).OrInsertWith(static () => 40);
        d += 4;
        Assert.Equal(44, dict["dddd"]);

        ref var e = ref lookup.Entry("eeeee".AsSpan()).OrInsertWith(10, static s => s * 5);
        Assert.Equal(50, e);
        Assert.Equal(50, dict["eeeee"]);

        ref var f = ref lookup.Entry("ffffff".AsSpan()).OrInsertWithKey(static span => span.Length * 10);
        Assert.Equal(60, f);
        Assert.Equal(60, dict["ffffff"]);

        ref var g = ref lookup.Entry("ggggggg".AsSpan()).OrInsertWithKey(3, static (span, mult) => span.Length * mult);
        Assert.Equal(21, g);
        Assert.Equal(21, dict["ggggggg"]);
    }

    [Fact]
    public void AndModify_AllFourOverloads_UpdateOccupiedAndSkipVacant()
    {
        var dict = new Dictionary<string, PlayerStats>(StringComparer.Ordinal)
        {
            ["p1"] = new PlayerStats { Hits = 1, Damage = 10 }
        };
        var lookup = dict.GetAlternateLookup<ReadOnlySpan<char>>();
        var vacantInvoked = false;

        // 1. Func<TValue, TValue>
        lookup.Entry("p1".AsSpan())
            .AndModify(static s => new PlayerStats { Hits = s.Hits + 1, Damage = s.Damage + 5 });
        Assert.Equal(new PlayerStats { Hits = 2, Damage = 15 }, dict["p1"]);

        // 2. Func<TValue, TState, TValue> (with ref struct ReadOnlySpan<char> state)
        ReadOnlySpan<char> tag = "bonus";
        lookup.Entry("p1".AsSpan())
            .AndModify(tag, static (s, t) => new PlayerStats { Hits = s.Hits, Damage = s.Damage + t.Length });
        Assert.Equal(new PlayerStats { Hits = 2, Damage = 20 }, dict["p1"]);

        // 3. RefAction<TValue> (in-place struct mutation)
        lookup.Entry("p1".AsSpan())
            .AndModify(static (ref s) => s.Hits++);
        Assert.Equal(new PlayerStats { Hits = 3, Damage = 20 }, dict["p1"]);

        // 4. RefAction<TValue, TState> + OrInsert chaining
        lookup.Entry("p1".AsSpan())
            .AndModify(12, static (ref s, dmg) =>
            {
                s.Hits++;
                s.Damage += dmg;
            })
            .OrInsert(default);
        Assert.Equal(new PlayerStats { Hits = 4, Damage = 32 }, dict["p1"]);

        lookup.Entry("p2".AsSpan())
            .AndModify(12, (ref s, dmg) =>
            {
                vacantInvoked = true;
                s.Damage += dmg;
            })
            .OrInsert(new PlayerStats { Hits = 1, Damage = 12 });

        Assert.False(vacantInvoked);
        Assert.Equal(new PlayerStats { Hits = 1, Damage = 12 }, dict["p2"]);
    }

    [Fact]
    public void ValueRef_AndPropertyPatterns_MutateExistingAndSkipMissing()
    {
        var dict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["PlayerOne"] = 10
        };
        var lookup = dict.GetAlternateLookup<ReadOnlySpan<char>>();

        Span<char> stackKey = stackalloc char[9];
        "playerone".AsSpan().CopyTo(stackKey);

        if (lookup.Entry(stackKey) is { IsOccupied: true } existing)
        {
            existing.ValueRef += 15;
        }

        if (lookup.Entry("nobody".AsSpan()) is { IsOccupied: true } missing)
        {
            missing.ValueRef += 15;
        }

        Assert.Equal(25, dict["PlayerOne"]);
        Assert.Equal("PlayerOne", Assert.Single(dict.Keys));
    }

    [Fact]
    public void OccupiedAndVacantEntries_SupportInspectInsertDeconstructAndRemove()
    {
        var dict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["Alpha"] = 10
        };
        var lookup = dict.GetAlternateLookup<ReadOnlySpan<char>>();

        // TryGetOccupied + Deconstruct + Insert + RemoveEntry returning stored TKey casing
        Assert.True(lookup.Entry("alpha".AsSpan()).TryGetOccupied(out var occupied));
        var (spanKey, snapshotVal) = occupied;
        Assert.True(spanKey.SequenceEqual("alpha"));
        Assert.Equal(10, snapshotVal);

        Assert.Equal(25, occupied.Insert(25));
        Assert.Equal(10, occupied.Value); // snapshot unchanged
        Assert.Equal(25, dict["Alpha"]);

        var (actualKey, removedVal) = occupied.RemoveEntry();
        Assert.Equal("Alpha", actualKey);
        Assert.Equal(25, removedVal);
        Assert.Empty(dict);

        // TryGetVacant + InsertEntry + Remove
        Assert.True(lookup.Entry("Beta".AsSpan()).TryGetVacant(out var vacant));
        Assert.True(vacant.Key.SequenceEqual("Beta"));
        var newlyOccupied = vacant.InsertEntry(50);
        Assert.Equal(50, newlyOccupied.Value);
        Assert.Equal(50, dict["beta"]);
        Assert.Equal(50, newlyOccupied.Remove());
        Assert.Empty(dict);

        // InsertEntry on AlternateEntry (vacant and occupied)
        var occ1 = lookup.Entry("Gamma".AsSpan()).InsertEntry(1);
        Assert.Equal(1, occ1.Value);
        var occ2 = lookup.Entry("gamma".AsSpan()).InsertEntry(2);
        Assert.Equal(2, occ2.Value);
        Assert.Equal("Gamma", Assert.Single(dict.Keys));
        Assert.Equal(2, dict["Gamma"]);
    }

    [Fact]
    public void OccupiedInsert_WhenKeyRemovedExternally_ReinsertsViaFallback()
    {
        var dict = new Dictionary<string, int>(StringComparer.Ordinal) { ["key"] = 1 };
        var lookup = dict.GetAlternateLookup<ReadOnlySpan<char>>();

        var occupied = lookup.Entry("key".AsSpan()).ToOccupied();
        dict.Remove("key");

        Assert.Equal(99, occupied.Insert(99));
        Assert.Equal(99, dict["key"]);
    }

    [Fact]
    public void Match_AllFourOverloads_ExecuteExpectedBranch()
    {
        var dict = new Dictionary<string, int>(StringComparer.Ordinal) { ["hit"] = 10 };
        var lookup = dict.GetAlternateLookup<ReadOnlySpan<char>>();

        // Action Match
        lookup.Entry("hit".AsSpan()).Match(
            static occ => occ.Insert(occ.Value + 5),
            static vac => vac.Insert(1));
        lookup.Entry("miss1".AsSpan()).Match(
            static occ => occ.Insert(occ.Value + 5),
            static vac => vac.Insert(1));
        Assert.Equal(15, dict["hit"]);
        Assert.Equal(1, dict["miss1"]);

        // Stateful Action Match (with ReadOnlySpan<char> state)
        ReadOnlySpan<char> suffix = "xyz";
        lookup.Entry("hit".AsSpan()).Match(
            suffix,
            static (occ, s) => occ.Insert(occ.Value + s.Length),
            static (vac, s) => vac.Insert(s.Length));
        Assert.Equal(18, dict["hit"]);

        // Func Match
        var hitStr = lookup.Entry("hit".AsSpan()).Match(static occ => occ.Value * 2, static _ => -1);
        var missStr = lookup.Entry("absent".AsSpan()).Match(static occ => occ.Value * 2, static _ => -1);
        Assert.Equal(36, hitStr);
        Assert.Equal(-1, missStr);

        // Stateful Func Match
        var scaled = lookup.Entry("hit".AsSpan()).Match(
            10,
            static (occ, factor) => occ.Value * factor,
            static (vac, factor) => vac.Key.Length * factor);
        Assert.Equal(180, scaled);
    }

    [Fact]
    public void ErrorAndDefaultEntryGuards_ThrowExpectedExceptions()
    {
        // Test with both StringComparer.Ordinal (Comparer implements IAlternateEqualityComparer)
        // and default new Dictionary<string, int>() (Comparer unwraps to EqualityComparer<string>.Default)
        AssertErrorGuards(new Dictionary<string, int>(StringComparer.Ordinal), expectKeyInMessage: true);
        AssertErrorGuards(new Dictionary<string, int>(), expectKeyInMessage: false);

        static void AssertErrorGuards(Dictionary<string, int> dict, bool expectKeyInMessage)
        {
            dict["present"] = 1;
            var lookup = dict.GetAlternateLookup<ReadOnlySpan<char>>();

            var missingEx = Assert.Throws<KeyNotFoundException>(() => _ = lookup.Entry("missing".AsSpan()).ValueRef);
            var toOccEx = Assert.Throws<KeyNotFoundException>(() => lookup.Entry("missing".AsSpan()).ToOccupied());
            var toVacEx = Assert.Throws<InvalidOperationException>(() => lookup.Entry("present".AsSpan()).ToVacant());

            if (expectKeyInMessage)
            {
                Assert.Contains("missing", missingEx.Message);
                Assert.Contains("missing", toOccEx.Message);
                Assert.Contains("present", toVacEx.Message);
            }

            // Occupied Remove / RemoveEntry when key already removed
            var occupied = lookup.Entry("present".AsSpan()).ToOccupied();
            dict.Remove("present");
            KeyNotFoundException? removeEx = null;
            try { occupied.Remove(); } catch (KeyNotFoundException ex) { removeEx = ex; }
            Assert.NotNull(removeEx);
            if (expectKeyInMessage) Assert.Contains("present", removeEx!.Message);

            // Duplicate add through stale vacant local
            var staleVacant = lookup.Entry("dup".AsSpan());
            dict["dup"] = 7;
            ArgumentException? dupEx = null;
            try { staleVacant.OrInsert(8); } catch (ArgumentException ex) { dupEx = ex; }
            Assert.NotNull(dupEx);
            Assert.Equal(7, dict["dup"]);

            // Default AlternateEntry, AlternateOccupiedEntry, AlternateVacantEntry
            var invoked = false;
            AlternateEntry<string, int, ReadOnlySpan<char>> defaultEntry = default;
            Assert.False(defaultEntry.IsOccupied);
            Assert.False(defaultEntry.IsVacant);
            Assert.False(defaultEntry.TryGetOccupied(out _));
            Assert.False(defaultEntry.TryGetVacant(out _));

            Assert.Throws<InvalidOperationException>(() => _ = default(AlternateEntry<string, int, ReadOnlySpan<char>>).ValueRef);
            Assert.Throws<InvalidOperationException>(() => default(AlternateEntry<string, int, ReadOnlySpan<char>>).ToOccupied());
            Assert.Throws<InvalidOperationException>(() => default(AlternateEntry<string, int, ReadOnlySpan<char>>).ToVacant());
            Assert.Throws<InvalidOperationException>(() => default(AlternateEntry<string, int, ReadOnlySpan<char>>).OrInsert(1));
            Assert.Throws<InvalidOperationException>(() => default(AlternateEntry<string, int, ReadOnlySpan<char>>).OrDefault());
            Assert.Throws<InvalidOperationException>(() => default(AlternateEntry<string, int, ReadOnlySpan<char>>).InsertEntry(1));
            Assert.Throws<InvalidOperationException>(() =>
                default(AlternateEntry<string, int, ReadOnlySpan<char>>).OrInsertWith(() => { invoked = true; return 1; }));
            Assert.Throws<InvalidOperationException>(() =>
                default(AlternateEntry<string, int, ReadOnlySpan<char>>).OrInsertWith(1, s => { invoked = true; return s; }));
            Assert.Throws<InvalidOperationException>(() =>
                default(AlternateEntry<string, int, ReadOnlySpan<char>>).OrInsertWithKey(_ => { invoked = true; return 1; }));
            Assert.Throws<InvalidOperationException>(() =>
                default(AlternateEntry<string, int, ReadOnlySpan<char>>).OrInsertWithKey(1, (_, s) => { invoked = true; return s; }));
            Assert.Throws<InvalidOperationException>(() =>
                default(AlternateEntry<string, int, ReadOnlySpan<char>>).AndModify(v => { invoked = true; return v; }));
            Assert.Throws<InvalidOperationException>(() =>
                default(AlternateEntry<string, int, ReadOnlySpan<char>>).AndModify(1, (v, s) => { invoked = true; return v + s; }));
            Assert.Throws<InvalidOperationException>(() =>
                default(AlternateEntry<string, int, ReadOnlySpan<char>>).AndModify((ref v) => { invoked = true; v++; }));
            Assert.Throws<InvalidOperationException>(() =>
                default(AlternateEntry<string, int, ReadOnlySpan<char>>).AndModify(1, (ref v, s) => { invoked = true; v += s; }));
            Assert.Throws<InvalidOperationException>(() =>
                default(AlternateEntry<string, int, ReadOnlySpan<char>>).Match(_ => invoked = true, _ => invoked = true));
            Assert.Throws<InvalidOperationException>(() =>
                default(AlternateEntry<string, int, ReadOnlySpan<char>>).Match(1, (_, _) => invoked = true, (_, _) => invoked = true));
            Assert.Throws<InvalidOperationException>(() =>
                default(AlternateEntry<string, int, ReadOnlySpan<char>>).Match(_ => { invoked = true; return 1; }, _ => { invoked = true; return 2; }));
            Assert.Throws<InvalidOperationException>(() =>
                default(AlternateEntry<string, int, ReadOnlySpan<char>>).Match(1, (_, s) => { invoked = true; return s; }, (_, s) => { invoked = true; return s; }));

            Assert.Throws<InvalidOperationException>(() => default(AlternateOccupiedEntry<string, int, ReadOnlySpan<char>>).Insert(1));
            Assert.Throws<InvalidOperationException>(() => default(AlternateOccupiedEntry<string, int, ReadOnlySpan<char>>).Remove());
            Assert.Throws<InvalidOperationException>(() => default(AlternateOccupiedEntry<string, int, ReadOnlySpan<char>>).RemoveEntry());
            Assert.Throws<InvalidOperationException>(() => default(AlternateVacantEntry<string, int, ReadOnlySpan<char>>).Insert(1));
            Assert.Throws<InvalidOperationException>(() => default(AlternateVacantEntry<string, int, ReadOnlySpan<char>>).InsertEntry(1));
            Assert.False(invoked);
        }
    }

    [Fact]
    public void AlternateEntry_InReadOnlyParameter_WorksWithoutDefensiveCopies()
    {
        var dict = new Dictionary<string, int>(StringComparer.Ordinal) { ["key"] = 10 };
        var lookup = dict.GetAlternateLookup<ReadOnlySpan<char>>();

        var entry = lookup.Entry("key".AsSpan());
        Inspect(in entry);
        Assert.Equal(15, dict["key"]);

        static void Inspect(in AlternateEntry<string, int, ReadOnlySpan<char>> e)
        {
            Assert.True(e.IsOccupied);
            Assert.False(e.IsVacant);
            Assert.True(e.Key.SequenceEqual("key"));
            e.ValueRef += 2;
            e.AndModify(static (ref v) => v += 3);
        }
    }

    [Fact]
    public void SpanLookups_AllocateZeroBytesAcrossCacheHitsAndReadOnlyMisses()
    {
        var dict = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["alpha"] = 1,
            ["beta"] = 2,
        };
        const string buffer = "alpha,beta,gamma";

        for (var i = 0; i < 200; i++)
        {
            _ = HotLoop(dict, buffer.AsSpan());
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        var sum = HotLoop(dict, buffer.AsSpan());
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.True(sum > 0);
        Assert.Equal(0, allocated);
        Assert.Equal(2, dict.Count);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int HotLoop(Dictionary<string, int> d, ReadOnlySpan<char> csv)
        {
            var lookup = d.GetAlternateLookup<ReadOnlySpan<char>>();
            var hitA = csv[..5];      // "alpha"
            var hitB = csv.Slice(6, 4); // "beta"
            var miss = csv[11..];     // "gamma"

            var total = 0;
            for (var i = 0; i < 500; i++)
            {
                lookup.Entry(hitA).AndModify(1, static (ref v, dlt) => v += dlt).OrInsert(0);
                total += lookup.Entry(hitB).OrInsert(0);
                if (lookup.Entry(miss).TryGetOccupied(out var occ))
                {
                    total += occ.Value;
                }
            }

            return total;
        }
    }

    [Fact]
    public void CustomUtf8AlternateComparer_SupportsUtf8ByteSpanLookups()
    {
        var dict = new Dictionary<string, int>(Utf8StringComparer.Instance);
        var lookup = dict.GetAlternateLookup<ReadOnlySpan<byte>>();

        lookup.Entry("hello"u8).OrInsert(10);
        lookup.Entry("hello"u8).AndModify(static (ref v) => v += 32).OrInsert(0);

        Assert.Equal(42, dict["hello"]);
        Assert.Equal(42, lookup.Entry("hello"u8).ValueRef);
    }

    private struct PlayerStats
    {
        public int Hits;
        public int Damage;
    }

    private sealed class Utf8StringComparer : IEqualityComparer<string>, IAlternateEqualityComparer<ReadOnlySpan<byte>, string>
    {
        public static readonly Utf8StringComparer Instance = new();

        public bool Equals(string? x, string? y) => string.Equals(x, y, StringComparison.Ordinal);

        public int GetHashCode(string obj)
        {
            Span<byte> buf = stackalloc byte[Encoding.UTF8.GetMaxByteCount(obj.Length)];
            var written = Encoding.UTF8.GetBytes(obj, buf);
            return GetHashCode(buf[..written]);
        }

        public bool Equals(ReadOnlySpan<byte> alternate, string other)
        {
            Span<byte> buf = stackalloc byte[Encoding.UTF8.GetMaxByteCount(other.Length)];
            var written = Encoding.UTF8.GetBytes(other, buf);
            return alternate.SequenceEqual(buf[..written]);
        }

        public int GetHashCode(ReadOnlySpan<byte> alternate)
        {
            var hc = new HashCode();
            hc.AddBytes(alternate);
            return hc.ToHashCode();
        }

        public string Create(ReadOnlySpan<byte> alternate) => Encoding.UTF8.GetString(alternate);
    }
}
