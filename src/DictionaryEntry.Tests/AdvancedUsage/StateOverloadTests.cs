using System.Runtime.CompilerServices;
using System.Text;

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
    public void ValueTupleState_PassesMultipleValuesAndChainsWithUnionSwitch()
    {
        var dict = new Dictionary<string, int> { ["existing"] = 10 };

        var updated = dict.Entry("existing")
            .AndModify((Multiplier: 3, Bonus: 5), static (v, s) => v * s.Multiplier + s.Bonus) switch
        {
            OccupiedEntry<string, int> occupied => occupied.Value,
            VacantEntry<string, int> => -1,
        };

        var initialized = dict.Entry("missing")
            .AndModify((Multiplier: 3, Bonus: 5), static (v, s) => v * s.Multiplier + s.Bonus)
            .OrInsertWithKey((Base: 10, Bonus: 2), static (k, s) => k.Length * s.Base + s.Bonus);

        Assert.Equal(35, updated);
        Assert.Equal(72, initialized);
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
    public void DefaultEntry_AllStateOverloads_ThrowInvalidOperationExceptionWithoutInvokingCallbacks()
    {
        var invoked = false;

        Assert.Throws<InvalidOperationException>(() =>
            default(Entry<string, int>).AndModify(1, (v, s) => { invoked = true; return v + s; }));
        Assert.Throws<InvalidOperationException>(() =>
            default(Entry<string, int>).OrInsertWith(1, s => { invoked = true; return s; }));
        Assert.Throws<InvalidOperationException>(() =>
            default(Entry<string, int>).OrInsertWithKey(1, (_, s) => { invoked = true; return s; }));
        Assert.Throws<InvalidOperationException>(() =>
            default(Entry<string, int>).Match(1, (_, _) => invoked = true, (_, _) => invoked = true));
        Assert.Throws<InvalidOperationException>(() =>
            default(Entry<string, int>).Match(
                1,
                (_, s) => { invoked = true; return s; },
                (_, s) => { invoked = true; return s; }));

        Assert.False(invoked);
    }

    [Fact]
    public void StateOverloads_WithStaticLambdas_AllocateZeroBytesAcrossAllOperations()
    {
        var dict = new Dictionary<string, int> { ["existing"] = 1 };

        // Warm up JIT and static delegate caches
        for (var i = 0; i < 200; i++)
        {
            _ = RunStatefulLoop(dict, i);
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        var sum = RunStatefulLoop(dict, 5);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.NotEqual(0L, sum);
        Assert.Equal(0, allocated);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static long RunStatefulLoop(Dictionary<string, int> d, int amount)
        {
            long acc = 0;
            for (var i = 0; i < 1_000; i++)
            {
                d.Entry("existing")
                    .AndModify(amount, static (v, amt) => v + amt)
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
    public void CapturingLambda_AllocatesClosure_WhileStateOverloadAllocatesZero()
    {
        var dict = new Dictionary<string, int> { ["existing"] = 10 };

        // Warm up both paths
        for (var i = 0; i < 200; i++)
        {
            _ = CapturingCacheHit(dict, "existing", i);
            _ = StatefulCacheHit(dict, "existing", i);
        }

        var beforeCapturing = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 100; i++)
        {
            _ = CapturingCacheHit(dict, "existing", i);
        }
        var capturingAllocated = GC.GetAllocatedBytesForCurrentThread() - beforeCapturing;

        var beforeStateful = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 100; i++)
        {
            _ = StatefulCacheHit(dict, "existing", i);
        }
        var statefulAllocated = GC.GetAllocatedBytesForCurrentThread() - beforeStateful;

        // Even on a 100% cache hit, the capturing lambda allocates its closure (24 B/call on .NET 10+, 88 B/call on .NET 8/9)
        Assert.True(capturingAllocated >= 2_400, $"Expected closure allocations, got {capturingAllocated} bytes.");
        Assert.Equal(0, statefulAllocated);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int CapturingCacheHit(Dictionary<string, int> d, string key, int multiplier) =>
            d.Entry(key).OrInsertWithKey(k => k.Length * multiplier);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int StatefulCacheHit(Dictionary<string, int> d, string key, int multiplier) =>
            d.Entry(key).OrInsertWithKey(multiplier, static (k, mult) => k.Length * mult);
    }

#if NET9_0_OR_GREATER
    [Fact]
    public void RefStructState_SpansAndCustomRefStructs_WorkAcrossAllStateOverloadsWithZeroAllocation()
    {
        var dict = new Dictionary<string, int> { ["alpha"] = 10 };
        ReadOnlySpan<char> charSpan = "hello-world".AsSpan(0, 5);
        ReadOnlySpan<byte> utf8Bytes = "42"u8;

        // 1. AndModify with ReadOnlySpan<char>
        dict.Entry("alpha").AndModify(charSpan, static (v, span) => v + span.Length);
        Assert.Equal(15, dict["alpha"]);

        // 2. OrInsertWith with ReadOnlySpan<byte>
        var parsed = dict.Entry("beta").OrInsertWith(utf8Bytes, static bytes => int.Parse(Encoding.UTF8.GetString(bytes)));
        Assert.Equal(42, parsed);

        // 3. OrInsertWithKey with custom ref struct
        var ctx = new ParseContext("99".AsSpan(), Multiplier: 2);
        var computed = dict.Entry("gamma").OrInsertWithKey(
            ctx,
            static (key, c) => key.Length + int.Parse(c.Digits) * c.Multiplier);
        Assert.Equal(5 + 198, computed);

        // 4. Match (Action) & Match (Func) with ReadOnlySpan<char>
        dict.Entry("alpha").Match(
            charSpan,
            static (occ, span) => occ.Insert(occ.Value + span.Length),
            static (vac, span) => vac.Insert(span.Length));
        Assert.Equal(20, dict["alpha"]);

        var matched = dict.Entry("alpha").Match(
            charSpan,
            static (occ, span) => occ.Value + span.Length,
            static (_, _) => 0);
        Assert.Equal(25, matched);

        // 5. Verify 0 bytes allocated when passing ReadOnlySpan<char> in a hot loop
        for (var i = 0; i < 200; i++)
        {
            _ = SpanStateHotLoop(dict, charSpan);
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        var total = SpanStateHotLoop(dict, charSpan);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.True(total > 0);
        Assert.Equal(0, allocated);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int SpanStateHotLoop(Dictionary<string, int> d, ReadOnlySpan<char> span)
        {
            var sum = 0;
            for (var i = 0; i < 500; i++)
            {
                d.Entry("alpha").AndModify(span, static (v, s) => v + s.Length);
                sum += d.Entry("alpha").OrInsertWith(span, static s => s.Length);
                sum += d.Entry("alpha").Match(
                    span,
                    static (occ, s) => occ.Value + s.Length,
                    static (_, s) => s.Length);
            }

            return sum;
        }
    }

    private readonly ref struct ParseContext(ReadOnlySpan<char> digits, int Multiplier)
    {
        public ReadOnlySpan<char> Digits { get; } = digits;
        public int Multiplier { get; } = Multiplier;
    }
#endif
}
