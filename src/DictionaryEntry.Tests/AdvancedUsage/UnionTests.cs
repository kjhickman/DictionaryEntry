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
    public void PropertyPatterns_MatchOccupiedValueAndEntryProperties()
    {
        var dict = new Dictionary<string, int>
        {
            ["high"] = 200,
            ["low"] = 5,
        };

        Assert.Equal("Large", Classify("high"));
        Assert.Equal("Small", Classify("low"));
        Assert.Equal("Missing", Classify("absent"));

        // Untyped property pattern applies to Entry itself
        Assert.True(dict.Entry("high") is { Key: "high", IsOccupied: true });
        Assert.True(dict.Entry("absent") is { Key: "absent", IsVacant: true });

        string Classify(string key) => dict.Entry(key) switch
        {
            OccupiedEntry<string, int> { Value: >= 100 } => "Large",
            OccupiedEntry<string, int> => "Small",
            VacantEntry<string, int> => "Missing",
        };
    }

    [Fact]
    public void PositionalDeconstructPattern_MatchesOccupiedEntry()
    {
        var dict = new Dictionary<string, int> { ["player1"] = 75 };

        var formatted = dict.Entry("player1") switch
        {
            OccupiedEntry<string, int>(var key, >= 50 and var score) => $"{key}:{score}",
            OccupiedEntry<string, int>(var key, _) => $"{key}:low",
            VacantEntry<string, int> { Key: var key } => $"{key}:none",
        };

        Assert.Equal("player1:75", formatted);
    }

    [Fact]
    public void SwitchStatement_MutatesDictionaryWithLocalState()
    {
        var dict = new Dictionary<string, int> { ["a"] = 10 };
        var bonus = 5;

        ApplyScore(dict, "a", bonus);
        ApplyScore(dict, "b", bonus);

        Assert.Equal(15, dict["a"]);
        Assert.Equal(5, dict["b"]);

        static void ApplyScore(Dictionary<string, int> scores, string key, int delta)
        {
            switch (scores.Entry(key))
            {
                case OccupiedEntry<string, int> occupied:
                    occupied.Insert(occupied.Value + delta);
                    break;
                case VacantEntry<string, int> vacant:
                    vacant.Insert(delta);
                    break;
            }
        }
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
    public void DefaultEntry_IsNullUnionAndDoesNotMatchCases()
    {
        Entry<string, int> defaultEntry = default;

        Assert.True(defaultEntry is null);
        Assert.False(defaultEntry is not null);
        Assert.False(defaultEntry is OccupiedEntry<string, int>);
        Assert.False(defaultEntry is VacantEntry<string, int>);
        Assert.False(defaultEntry.TryGetOccupied(out _));
        Assert.False(defaultEntry.TryGetVacant(out _));
    }

    [Fact]
    public void UnionPatternMatching_DoesNotBox()
    {
        var dict = new Dictionary<string, int> { ["existing"] = 42 };

        // Warm up JIT
        for (var i = 0; i < 200; i++)
        {
            _ = MatchLoop(dict);
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        var total = MatchLoop(dict);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(42_000, total);
        Assert.Equal(0, allocated);

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
}
