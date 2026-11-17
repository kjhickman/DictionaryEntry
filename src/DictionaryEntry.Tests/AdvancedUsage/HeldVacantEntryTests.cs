namespace DictionaryEntry.Tests.AdvancedUsage;

/// <summary>
/// A vacant <see cref="Entry{TKey, TValue}"/> remembers the empty bucket found by its lookup and reuses it on insertion.
/// These tests hold a vacant entry across external mutations to verify the bucket is never reused once stale.
/// Each test tries many target keys so that some are guaranteed to land in empty buckets.
/// </summary>
public class HeldVacantEntryTests
{
    public enum Mutation
    {
        None,
        Clear,
        EnsureCapacity,
        TrimExcess,
        RemoveOther,
        RemoveOtherAndAdd,
        AddOther,
        GrowPastCapacity,
    }

    public static TheoryData<Mutation> Mutations => new(Enum.GetValues<Mutation>());

    private const int TargetCount = 32;

    [Theory]
    [MemberData(nameof(Mutations))]
    public void HeldVacantEntry_OrInsert_AfterExternalMutation_InsertsReachableEntry(Mutation mutation)
    {
        foreach (var capacity in new[] { 0, 32, 512 })
        {
            for (var t = 0; t < TargetCount; t++)
            {
                // Arrange
                var target = $"target{t}";
                var dict = CreateStringDictionary(capacity, populate: capacity > 0);
                var entry = dict.Entry(target);

                // Act
                Mutate(dict, mutation);
                entry.OrInsert(123);

                // Assert
                AssertIntact(dict, target, 123);
            }
        }
    }

    [Theory]
    [MemberData(nameof(Mutations))]
    public void VacantEntry_OrInsertWith_FactoryMutatesDictionary_InsertsReachableEntry(Mutation mutation)
    {
        for (var t = 0; t < TargetCount; t++)
        {
            // Arrange
            var target = $"target{t}";
            var dict = CreateStringDictionary(32, populate: true);

            // Act
            dict.Entry(target).OrInsertWith(() =>
            {
                Mutate(dict, mutation);
                return 7;
            });

            // Assert
            AssertIntact(dict, target, 7);
        }
    }

    [Theory]
    [MemberData(nameof(Mutations))]
    public void HeldAlternateVacantEntry_OrDefault_AfterExternalMutation_InsertsReachableEntry(Mutation mutation)
    {
        for (var t = 0; t < TargetCount; t++)
        {
            // Arrange
            var target = $"target{t}";
            var dict = CreateStringDictionary(32, populate: true);
            var lookup = dict.GetAlternateLookup<ReadOnlySpan<char>>();
            var entry = lookup.Entry(target.AsSpan());

            // Act
            Mutate(dict, mutation);
            entry.OrDefault() = 9;

            // Assert
            AssertIntact(dict, target, 9);
        }
    }

    [Theory]
    [MemberData(nameof(Mutations))]
    public void HeldValueTypeVacantEntry_InsertEntry_AfterExternalMutation_InsertsReachableEntry(Mutation mutation)
    {
        for (var target = 1000; target < 1000 + TargetCount; target++)
        {
            // Arrange
            var dict = new Dictionary<int, int>(32);
            for (var i = 0; i < 20; i++)
            {
                dict[i] = i;
            }

            var entry = dict.Entry(target);

            // Act
            Mutate(dict, mutation);
            entry.InsertEntry(55);

            // Assert
            Assert.Equal(55, dict[target]);
            foreach (var (key, value) in dict)
            {
                Assert.Equal(value, dict[key]);
            }
        }
    }

    private static void Mutate(Dictionary<int, int> dict, Mutation mutation)
    {
        switch (mutation)
        {
            case Mutation.Clear:
                dict.Clear();
                break;
            case Mutation.EnsureCapacity:
                dict.EnsureCapacity(500);
                break;
            case Mutation.TrimExcess:
                dict.TrimExcess();
                break;
            case Mutation.RemoveOther:
                dict.Remove(0);
                break;
            case Mutation.RemoveOtherAndAdd:
                dict.Remove(0);
                dict.Add(-1, -1);
                break;
            case Mutation.AddOther:
                dict.Add(-1, -1);
                break;
            case Mutation.GrowPastCapacity:
                for (var i = 0; i < 200; i++)
                {
                    dict[5000 + i] = i;
                }

                break;
        }
    }

    private static Dictionary<string, int> CreateStringDictionary(int capacity, bool populate)
    {
        var dict = new Dictionary<string, int>(capacity, StringComparer.Ordinal);
        if (populate)
        {
            for (var i = 0; i < 20; i++)
            {
                dict[$"k{i}"] = i;
            }
        }

        return dict;
    }

    private static void Mutate(Dictionary<string, int> dict, Mutation mutation)
    {
        switch (mutation)
        {
            case Mutation.Clear:
                dict.Clear();
                break;
            case Mutation.EnsureCapacity:
                dict.EnsureCapacity(dict.Count * 8 + 64);
                break;
            case Mutation.TrimExcess:
                dict.TrimExcess();
                break;
            case Mutation.RemoveOther:
                dict.Remove("k0");
                break;
            case Mutation.RemoveOtherAndAdd:
                dict.Remove("k0");
                dict.Add("other", -1);
                break;
            case Mutation.AddOther:
                dict.Add("other", -1);
                break;
            case Mutation.GrowPastCapacity:
                for (var i = 0; i < 200; i++)
                {
                    dict[$"grow{i}"] = i;
                }

                break;
        }
    }

    private static void AssertIntact(Dictionary<string, int> dict, string key, int expected)
    {
        Assert.Equal(expected, dict[key]);

        var seen = 0;
        foreach (var (k, v) in dict)
        {
            Assert.True(dict.TryGetValue(k, out var found));
            Assert.Equal(v, found);
            seen++;
        }

        Assert.Equal(dict.Count, seen);
    }
}
