using System.Runtime.CompilerServices;

namespace DictionaryEntry.Tests.AdvancedUsage;

public class UnionTests
{
    [Fact]
    public void SwitchExpression_IsExhaustive_HandlesOccupiedAndVacant()
    {
        var dict = new Dictionary<string, int> { ["existing"] = 21 };

        // No '_' or 'null' arm needed: compiler verifies exhaustiveness over OccupiedEntry and VacantEntry
        var existingResult = dict.Entry("existing") switch
        {
            OccupiedEntry<string, int> occupied => occupied.Value * 2,
            VacantEntry<string, int> vacant => vacant.Insert(10),
        };

        var missingResult = dict.Entry("missing") switch
        {
            OccupiedEntry<string, int> occupied => occupied.Value * 2,
            VacantEntry<string, int> vacant => vacant.Insert(10),
        };

        Assert.Equal(42, existingResult);
        Assert.Equal(10, missingResult);
        Assert.Equal(10, dict["missing"]);
    }

    [Fact]
    public void IsPattern_MatchesOccupiedAndVacantCases()
    {
        var dict = new Dictionary<string, int> { ["existing"] = 42 };

        Assert.True(dict.Entry("existing") is OccupiedEntry<string, int> occupied && occupied.Value == 42);
        Assert.False(dict.Entry("existing") is VacantEntry<string, int>);

        Assert.True(dict.Entry("missing") is VacantEntry<string, int> vacant && vacant.Key == "missing");
        Assert.False(dict.Entry("missing") is OccupiedEntry<string, int>);
    }

    [Fact]
    public void ImplicitUnionConversion_ConvertsCaseTypesToEntry()
    {
        var dict = new Dictionary<string, int> { ["existing"] = 42 };

        var occupied = dict.Entry("existing").ToOccupied();
        Entry<string, int> fromOccupied = occupied;
        Assert.True(fromOccupied.IsOccupied);
        Assert.Equal("existing", fromOccupied.Key);

        var vacant = dict.Entry("missing").ToVacant();
        Entry<string, int> fromVacant = vacant;
        Assert.True(fromVacant.IsVacant);
        Assert.Equal("missing", fromVacant.Key);
    }

    [Fact]
    public void ImplicitUnionConversion_FromRemovedOccupiedEntry_IsVacant()
    {
        // Entry is a live view; converting a case re-reads the dictionary rather than trusting the snapshot
        var dict = new Dictionary<string, int> { ["a"] = 42, ["b"] = 99 };

        var occupiedA = dict.Entry("a").ToOccupied();
        dict.Remove("a");
        Entry<string, int> entryA = occupiedA;
        Assert.True(entryA is VacantEntry<string, int> { Key: "a" });

        var occupiedB = dict.Entry("b").ToOccupied();
        occupiedB.Remove();
        Entry<string, int> entryB = occupiedB;
        Assert.True(entryB is VacantEntry<string, int> { Key: "b" });
    }

    [Fact]
    public void ImplicitUnionConversion_FromStaleOccupiedEntry_CarriesCurrentValue()
    {
        var dict = new Dictionary<string, int> { ["key"] = 1 };
        var occupied = dict.Entry("key").ToOccupied();
        dict["key"] = 99;

        Entry<string, int> entry = occupied;

        Assert.True(entry is OccupiedEntry<string, int> { Value: 99 });
        Assert.Equal(1, occupied.Value);
    }

    [Fact]
    public void ImplicitUnionConversion_FromFilledVacantEntry_IsOccupied()
    {
        var dict = new Dictionary<string, int>();
        var vacant = dict.Entry("key").ToVacant();
        vacant.Insert(7);

        Entry<string, int> entry = vacant;

        Assert.True(entry is OccupiedEntry<string, int> { Value: 7 });
    }

    [Fact]
    public void ImplicitUnionConversion_ReturnedFromMethod_ProducesUsableEntry()
    {
        var dict = new Dictionary<string, int>();

        ref var value = ref Lookup(dict, "key").OrInsert(3);
        value++;

        Assert.Equal(4, dict["key"]);

        static Entry<string, int> Lookup(Dictionary<string, int> d, string key) => d.Entry(key).ToVacant();
    }

    [Fact]
    public void ImplicitUnionConversion_FromDefaultCases_IsNullUnion()
    {
        Entry<string, int> fromOccupied = default(OccupiedEntry<string, int>);
        Entry<string, int> fromVacant = default(VacantEntry<string, int>);

        Assert.True(fromOccupied is null);
        Assert.True(fromVacant is null);
    }

    [Fact]
    public void OccupiedWithNullValue_IsOccupiedNotNullUnion()
    {
        var dict = new Dictionary<string, string?> { ["key"] = null };

        var result = dict.Entry("key") switch
        {
            OccupiedEntry<string, string?> { Value: null } => "occupied-null",
            OccupiedEntry<string, string?> => "occupied",
            VacantEntry<string, string?> => "vacant",
        };

        Assert.Equal("occupied-null", result);
        Assert.False(dict.Entry("key") is null);
    }

    [Fact]
    public void DefaultEntry_IsNeitherOccupiedNorVacantAndMatchesOnlyNull()
    {
        Entry<string, int> defaultEntry = default;

        Assert.False(defaultEntry.IsOccupied);
        Assert.False(defaultEntry.IsVacant);
        Assert.True(defaultEntry is null);
        Assert.False(defaultEntry is OccupiedEntry<string, int>);
        Assert.False(defaultEntry is VacantEntry<string, int>);
        Assert.False(defaultEntry.TryGetOccupied(out _));
        Assert.False(defaultEntry.TryGetVacant(out _));
    }

    [Fact]
    public void UnionPatternMatching_DoesNotBox()
    {
        var dict = new Dictionary<string, int> { ["existing"] = 42 };

        var total = AllocationAssert.ZeroAllocations(dict, static d => MatchLoop(d));

        Assert.Equal(42_000, total);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static int MatchLoop(Dictionary<string, int> d)
        {
            var sum = 0;
            for (var i = 0; i < 1_000; i++)
            {
                sum += d.Entry("existing") switch
                {
                    OccupiedEntry<string, int> o => o.Value,
                    VacantEntry<string, int> => 0,
                };
                sum += d.Entry("missing") switch
                {
                    OccupiedEntry<string, int> o => o.Value,
                    VacantEntry<string, int> => 0,
                };
            }

            return sum;
        }
    }

    [Fact]
    public void UnionValue_ViaObjectPatternAndInterface_BoxesExpectedCaseOrNull()
    {
        var dict = new Dictionary<string, int> { ["existing"] = 42 };
        Entry<string, int> defaultEntry = default;

        // C# 15 'is object' on a custom union lowers through HasValue + IUnionMembers.Value
        Assert.True(dict.Entry("existing") is object boxedOccupied
                    && boxedOccupied is OccupiedEntry<string, int> { Key: "existing", Value: 42 });
        Assert.True(dict.Entry("missing") is object boxedVacant
                    && boxedVacant is VacantEntry<string, int> { Key: "missing" });
        Assert.False(defaultEntry is object);

        // Occupied entry whose TValue is null must still box OccupiedEntry rather than returning null
        var nullableDict = new Dictionary<string, string?> { ["nullValue"] = null };
        Assert.True(nullableDict.Entry("nullValue") is object boxedNullValueOccupied
                    && boxedNullValueOccupied is OccupiedEntry<string, string?> { Key: "nullValue", Value: null });

        // Also verify direct IUnionMembers.Value access (including the defaultEntry -> null branch)
        Assert.IsType<OccupiedEntry<string, int>>(
            GetUnionValue<string, int, Entry<string, int>>(dict.Entry("existing")));
        Assert.IsType<VacantEntry<string, int>>(
            GetUnionValue<string, int, Entry<string, int>>(dict.Entry("missing")));
        Assert.Null(GetUnionValue<string, int, Entry<string, int>>(defaultEntry));
        Assert.IsType<OccupiedEntry<string, string?>>(
            GetUnionValue<string, string?, Entry<string, string?>>(nullableDict.Entry("nullValue")));

        static object? GetUnionValue<TKey, TValue, TUnion>(TUnion union)
            where TKey : notnull
            where TUnion : Entry<TKey, TValue>.IUnionMembers, allows ref struct
            => union.Value;
    }

    [Fact]
    public void LocalEntry_TransitionsFromVacantToOccupied_AcrossAllInsertionMethods()
    {
        var dict = new Dictionary<string, int>();

        var viaFactory = dict.Entry("a");
        Assert.True(viaFactory is VacantEntry<string, int>);
        viaFactory.OrInsertWith(() => 10);
        Assert.True(viaFactory is OccupiedEntry<string, int> { Key: "a", Value: 10 });

        var viaKeyFactory = dict.Entry("bb");
        Assert.True(viaKeyFactory is VacantEntry<string, int>);
        viaKeyFactory.OrInsertWithKey(k => k.Length);
        Assert.True(viaKeyFactory is OccupiedEntry<string, int> { Key: "bb", Value: 2 });

        var viaDefault = dict.Entry("c");
        Assert.True(viaDefault is VacantEntry<string, int>);
        viaDefault.OrDefault() = 7;
        Assert.True(viaDefault is OccupiedEntry<string, int> { Key: "c", Value: 7 });

        var viaInsertEntry = dict.Entry("d");
        Assert.True(viaInsertEntry is VacantEntry<string, int>);
        viaInsertEntry.InsertEntry(99);
        Assert.True(viaInsertEntry is OccupiedEntry<string, int> { Key: "d", Value: 99 });
    }

#if NET11_0_OR_GREATER
    [Fact]
    public void Entry_SatisfiesStandardIUnionGenericConstraint()
    {
        var dict = new Dictionary<string, int> { ["existing"] = 42 };
        Entry<string, int> defaultEntry = default;

        var occupiedBoxed = ExtractUnionValue(dict.Entry("existing"));
        Assert.True(occupiedBoxed is OccupiedEntry<string, int> { Key: "existing", Value: 42 });

        var vacantBoxed = ExtractUnionValue(dict.Entry("missing"));
        Assert.True(vacantBoxed is VacantEntry<string, int> { Key: "missing" });

        Assert.Null(ExtractUnionValue(defaultEntry));

        var nullableDict = new Dictionary<string, string?> { ["nullValue"] = null };
        var nullValueOccupied = ExtractUnionValue(nullableDict.Entry("nullValue"));
        Assert.True(nullValueOccupied is OccupiedEntry<string, string?> { Key: "nullValue", Value: null });

        static object? ExtractUnionValue<TUnion>(TUnion union)
            where TUnion : IUnion, allows ref struct
            => union.Value;
    }
#endif
}
