using System.Globalization;
using System.Runtime.CompilerServices;

namespace DictionaryEntry.Tests.AdvancedUsage;

public class StateOverloadTests
{
    [Fact]
    public void AndModify_WithState_UpdatesOccupiedAndSkipsVacant()
    {
        var dict = new Dictionary<string, int> { ["existing"] = 10 };
        var invokedOnVacant = false;

        dict.Entry("existing").AndModify(5, static (v, delta) => v + delta);
        var vacant = dict.Entry("missing").AndModify(5, (v, delta) =>
        {
            invokedOnVacant = true;
            return v + delta;
        });

        Assert.Equal(15, dict["existing"]);
        Assert.False(invokedOnVacant);
        Assert.True(vacant.IsVacant);
        Assert.False(dict.ContainsKey("missing"));
    }

    [Fact]
    public void AndModifyRef_WithState_MutatesStructFieldsInPlaceAndSkipsVacant()
    {
        var dict = new Dictionary<string, PlayerStats>
        {
            ["existing"] = new PlayerStats { Hits = 2, Damage = 15 }
        };
        var invokedOnVacant = false;

        dict.Entry("existing")
            .AndModify(10, static (ref stats, dmg) =>
            {
                stats.Hits++;
                stats.Damage += dmg;
            })
            .OrDefault();

        dict.Entry("missing")
            .AndModify(10, (ref stats, dmg) =>
            {
                invokedOnVacant = true;
                stats.Damage += dmg;
            })
            .OrInsert(new PlayerStats { Hits = 1, Damage = 10 });

        Assert.Equal(new PlayerStats { Hits = 3, Damage = 25 }, dict["existing"]);
        Assert.False(invokedOnVacant);
        Assert.Equal(new PlayerStats { Hits = 1, Damage = 10 }, dict["missing"]);
    }

    [Fact]
    public void OrInsertWith_WithState_InsertsUsingStateOrReturnsExistingWithoutCallingFactory()
    {
        var dict = new Dictionary<string, int> { ["existing"] = 42 };
        var factoryCalledForExisting = false;

        ref var existingRef = ref dict.Entry("existing").OrInsertWith(99, state =>
        {
            factoryCalledForExisting = true;
            return state;
        });
        Assert.False(factoryCalledForExisting);
        Assert.Equal(42, existingRef);
        existingRef += 8;
        Assert.Equal(50, dict["existing"]);

        ref var insertedRef = ref dict.Entry("missing").OrInsertWith(20, static state => state * 3);
        Assert.Equal(60, insertedRef);
        insertedRef += 5;
        Assert.Equal(65, dict["missing"]);
    }

    [Fact]
    public void OrInsertWithKey_WithState_PassesKeyAndStateOrReturnsExistingWithoutCallingFactory()
    {
        var dict = new Dictionary<string, string> { ["existing"] = "already-here" };
        var factoryCalledForExisting = false;

        ref var existingRef = ref dict.Entry("existing").OrInsertWithKey("prefix", (k, pfx) =>
        {
            factoryCalledForExisting = true;
            return $"{pfx}:{k}";
        });
        Assert.False(factoryCalledForExisting);
        Assert.Equal("already-here", existingRef);

        ref var insertedRef = ref dict.Entry("item").OrInsertWithKey("pfx", static (k, pfx) => $"{pfx}:{k}");
        Assert.Equal("pfx:item", insertedRef);
        insertedRef = "updated";
        Assert.Equal("updated", dict["item"]);
    }

    [Fact]
    public void MatchAction_WithState_PassesStateToExecutedBranchOnly()
    {
        var dict = new Dictionary<string, int> { ["a"] = 10 };

        dict.Entry("a").Match(
            7,
            static (occupied, delta) => occupied.Insert(occupied.Value + delta),
            static (vacant, delta) => vacant.Insert(delta));

        dict.Entry("b").Match(
            7,
            static (occupied, delta) => occupied.Insert(occupied.Value + delta),
            static (vacant, delta) => vacant.Insert(delta));

        Assert.Equal(17, dict["a"]);
        Assert.Equal(7, dict["b"]);
    }

    [Fact]
    public void MatchFunc_WithState_PassesStateAndReturnsResult()
    {
        var dict = new Dictionary<string, int> { ["player1"] = 120 };

        var existingTier = dict.Entry("player1").Match(
            100,
            static (occupied, minScore) => occupied.Value >= minScore ? "Pass" : "Fail",
            static (_, _) => "Unranked");

        var missingTier = dict.Entry("player2").Match(
            100,
            static (occupied, minScore) => occupied.Value >= minScore ? "Pass" : "Fail",
            static (_, _) => "Unranked");

        Assert.Equal("Pass", existingTier);
        Assert.Equal("Unranked", missingTier);
    }

    [Fact]
    public void OrInsertWithStateVariants_WhenFactoryThrows_DoNotInsertDefaultEntry()
    {
        var dict = new Dictionary<string, int>();

        Assert.Throws<InvalidOperationException>(() =>
            dict.Entry("a").OrInsertWith(42, static _ => throw new InvalidOperationException("boom")));
        Assert.Throws<InvalidOperationException>(() =>
            dict.Entry("b").OrInsertWithKey(42, static (_, _) => throw new InvalidOperationException("boom")));

        Assert.Empty(dict);
    }

    [Fact]
    public void StateOverloads_WithStaticLambdas_AllocateZeroBytesAcrossAllOperations()
    {
        var dict = new Dictionary<string, int> { ["existing"] = 1 };

        var sum = AllocationAssert.ZeroAllocations(dict, 5, static (d, amt) => RunStatefulLoop(d, amt));

        Assert.NotEqual(0L, sum);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long RunStatefulLoop(Dictionary<string, int> d, int amount)
        {
            long acc = 0;
            for (var i = 0; i < 1_000; i++)
            {
                d.Entry("existing")
                    .AndModify(amount, static (v, amt) => v + amt)
                    .OrInsert(amount);

                d.Entry("existing")
                    .AndModify(amount, static (ref v, amt) => v += amt)
                    .OrInsert(amount);

                acc += d.Entry("existing")
                    .OrInsertWith(amount, static amt => amt);

                acc += d.Entry("existing")
                    .OrInsertWithKey(amount, static (k, amt) => k.Length + amt);

                d.Entry("existing").Match(
                    amount,
                    static (occ, amt) => occ.Insert(occ.Value + amt),
                    static (vac, amt) => vac.Insert(amt));

                acc += d.Entry("existing").Match(
                    amount,
                    static (occ, amt) => occ.Value + amt,
                    static (_, amt) => amt);

                acc += d.Entry("missing").Match(
                    amount,
                    static (occ, amt) => occ.Value + amt,
                    static (_, amt) => amt);
            }

            return acc;
        }
    }

    [Fact]
    public void RefStructState_SpansAndCustomRefStructs_WorkAcrossAllStateOverloads()
    {
        var dict = new Dictionary<string, int> { ["alpha"] = 10 };
        ReadOnlySpan<char> charSpan = "hello-world".AsSpan(0, 5);
        ReadOnlySpan<byte> utf8Bytes = "42"u8;

        // 1. AndModify (Func and RefAction) with ReadOnlySpan<char>
        dict.Entry("alpha").AndModify(charSpan, static (v, span) => v + span.Length);
        Assert.Equal(15, dict["alpha"]);
        dict.Entry("alpha").AndModify(charSpan, static (ref v, span) => v += span.Length);
        Assert.Equal(20, dict["alpha"]);

        // 2. OrInsertWith with ReadOnlySpan<byte>
        var parsed = dict.Entry("beta").OrInsertWith(utf8Bytes, static bytes => int.Parse(bytes, CultureInfo.InvariantCulture));
        Assert.Equal(42, parsed);

        // 3. OrInsertWithKey with custom ref struct
        var ctx = new ParseContext("99".AsSpan(), Multiplier: 2);
        var computed = dict.Entry("gamma").OrInsertWithKey(
            ctx,
            static (key, c) => key.Length + int.Parse(c.Digits, CultureInfo.InvariantCulture) * c.Multiplier);
        Assert.Equal(5 + 198, computed);

        // 4. Match (Action) & Match (Func) with ReadOnlySpan<char>
        dict.Entry("alpha").Match(
            charSpan,
            static (occ, span) => occ.Insert(occ.Value + span.Length),
            static (vac, span) => vac.Insert(span.Length));
        Assert.Equal(25, dict["alpha"]);

        var matched = dict.Entry("alpha").Match(
            charSpan,
            static (occ, span) => occ.Value + span.Length,
            static (_, _) => 0);
        Assert.Equal(30, matched);
    }

    private readonly ref struct ParseContext(ReadOnlySpan<char> digits, int Multiplier)
    {
        public ReadOnlySpan<char> Digits { get; } = digits;
        public int Multiplier { get; } = Multiplier;
    }
}
