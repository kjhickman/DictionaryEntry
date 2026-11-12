// ReSharper disable PreferConcreteValueOverDefault
namespace DictionaryEntry.Tests.AdvancedUsage;

public class EdgeCaseTests
{
    [Fact]
    public void ToOccupied_OnVacantEntry_ThrowsKeyNotFoundException()
    {
        // Arrange
        var dict = new Dictionary<string, int>();

        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() => dict.Entry("key").ToOccupied());
    }

    [Fact]
    public void ToVacant_OnOccupiedEntry_ThrowsException()
    {
        // Arrange
        var dict = new Dictionary<string, int> { ["key"] = 42 };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => dict.Entry("key").ToVacant());
    }

    [Fact]
    public void OrDefault_WithVacantEntry_InsertsDefaultValue()
    {
        // Arrange
        var dict = new Dictionary<string, int>();

        // Act
        ref var value = ref dict.Entry("key").OrDefault();

        // Assert
        Assert.Equal(default, dict["key"]);
        Assert.Equal(default, value);

        // Test with reference type
        var dictStr = new Dictionary<string, string>();
        ref var strValue = ref dictStr.Entry("key").OrDefault();

        // Assert
        Assert.Null(dictStr["key"]);
        Assert.Null(strValue);
    }

    [Fact]
    public void DefaultEntry_IsNeitherOccupiedNorVacant()
    {
        // Arrange
        Entry<string, int> entry = default;

        // Assert
        Assert.False(entry.IsOccupied);
        Assert.False(entry.IsVacant);
    }

    [Fact]
    public void DefaultEntry_ToOccupied_ThrowsInvalidOperationException()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => default(Entry<string, int>).ToOccupied());
        Assert.Contains("not associated with a dictionary", ex.Message);
    }

    [Fact]
    public void DefaultEntry_ValueRef_ThrowsInvalidOperationException()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => _ = default(Entry<string, int>).ValueRef);
        Assert.Contains("not associated with a dictionary", ex.Message);
    }

    [Fact]
    public void DefaultEntry_ToVacant_ThrowsInvalidOperationException()
    {
        // Act & Assert (ref structs can't be captured by lambdas, so the default is created inside)
        var ex = Assert.Throws<InvalidOperationException>(() => default(Entry<string, int>).ToVacant());
        Assert.Contains("not associated with a dictionary", ex.Message);
    }

    [Fact]
    public void DefaultEntry_MatchAction_ThrowsWithoutInvokingEitherBranch()
    {
        // Arrange
        var invoked = false;

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            default(Entry<string, int>).Match(_ => invoked = true, _ => invoked = true));
        Assert.False(invoked);
    }

    [Fact]
    public void DefaultEntry_MatchFunc_ThrowsWithoutInvokingEitherBranch()
    {
        // Arrange
        var invoked = false;

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            default(Entry<string, int>).Match(_ => { invoked = true; return 1; }, _ => { invoked = true; return 2; }));
        Assert.False(invoked);
    }

    [Fact]
    public void DefaultEntry_AndModify_ThrowsWithoutInvokingCallback()
    {
        // Arrange
        var invoked = false;

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            default(Entry<string, int>).AndModify(v => { invoked = true; return v; }));
        Assert.Contains("not associated with a dictionary", ex.Message);
        Assert.False(invoked);
    }

    [Fact]
    public void DefaultEntry_InsertionMethods_ThrowInvalidOperationExceptionWithoutInvokingFactories()
    {
        var factoryInvoked = false;

        Assert.Throws<InvalidOperationException>(() => default(Entry<string, int>).OrInsert(1));
        Assert.Throws<InvalidOperationException>(() => default(Entry<string, int>).OrDefault());
        Assert.Throws<InvalidOperationException>(() => default(Entry<string, int>).InsertEntry(1));

        Assert.Throws<InvalidOperationException>(() =>
            default(Entry<string, int>).OrInsertWith(() => { factoryInvoked = true; return 1; }));
        Assert.Throws<InvalidOperationException>(() =>
            default(Entry<string, int>).OrInsertWithKey(_ => { factoryInvoked = true; return 1; }));

        Assert.False(factoryInvoked);
    }

    [Fact]
    public void DefaultOccupiedAndVacantEntries_MutatingMethods_ThrowInvalidOperationException()
    {
        OccupiedEntry<string, int> defaultOccupied = default;
        VacantEntry<string, int> defaultVacant = default;

        Assert.Throws<InvalidOperationException>(() => defaultOccupied.Insert(1));
        Assert.Throws<InvalidOperationException>(() => defaultOccupied.Remove());
        Assert.Throws<InvalidOperationException>(() => defaultOccupied.RemoveEntry());

        Assert.Throws<InvalidOperationException>(() => defaultVacant.Insert(1));
        Assert.Throws<InvalidOperationException>(() => defaultVacant.InsertEntry(1));
    }

    [Fact]
    public void AndModify_WhenUpdateFuncResizesDictionary_ChainedRefPointsAtLiveStorage()
    {
        // Arrange
        var dict = new Dictionary<string, int> { ["key"] = 1 };

        // Act: the callback forces several resizes, invalidating the ref captured by Entry()
        ref var valueRef = ref dict.Entry("key")
            .AndModify(v =>
            {
                for (var i = 0; i < 100; i++)
                {
                    dict[$"filler{i}"] = i;
                }

                return v + 1;
            })
            .OrInsert(0);

        // Assert: the ref observes the modified value and writes through it are not lost
        Assert.Equal(2, valueRef);
        valueRef = 500;
        Assert.Equal(500, dict["key"]);
    }

    [Fact]
    public void AndModify_WhenUpdateFuncResizesDictionary_ChainedAndModifySeesUpdatedValue()
    {
        // Arrange
        var dict = new Dictionary<string, int> { ["key"] = 1 };

        // Act
        dict.Entry("key")
            .AndModify(v =>
            {
                for (var i = 0; i < 100; i++)
                {
                    dict[$"filler{i}"] = i;
                }

                return v + 1;
            })
            .AndModify(v => v * 10);

        // Assert
        Assert.Equal(20, dict["key"]);
    }

    [Fact]
    public void AndModify_WhenUpdateFuncRemovesKey_ReinsertsWithNewValue()
    {
        // Arrange
        var dict = new Dictionary<string, int> { ["key"] = 1 };

        // Act
        var entry = dict.Entry("key").AndModify(v =>
        {
            dict.Remove("key");
            return v + 1;
        });

        // Assert
        Assert.Equal(2, dict["key"]);
        Assert.True(entry is OccupiedEntry<string, int> { Value: 2 });
    }

    [Fact]
    public void AndModify_ChainedTwice_AppliesBothUpdates()
    {
        // Arrange
        var dict = new Dictionary<string, int> { ["key"] = 1 };

        // Act
        dict.Entry("key").AndModify(v => v + 1).AndModify(v => v * 10);

        // Assert
        Assert.Equal(20, dict["key"]);
    }

    [Fact]
    public void LocalEntry_TracksStateAcrossDirectCalls()
    {
        // Arrange
        var dict = new Dictionary<string, int>();
        var entry = dict.Entry("key");

        // Act: calls made directly on the local mutate it, so the second OrInsert sees the first
        entry.OrInsert(1);
        ref var value = ref entry.OrInsert(2);

        // Assert
        Assert.Equal(1, value);
        Assert.Equal(1, dict["key"]);
        Assert.True(entry.IsOccupied);
        Assert.True(entry is OccupiedEntry<string, int> { Value: 1 });
    }

    [Fact]
    public void LocalEntry_IsNotUpdatedByCallsOnChainedCopy()
    {
        // Arrange
        var dict = new Dictionary<string, int>();
        var entry = dict.Entry("key");

        // Act: AndModify returns a copy, so OrInsert transitions the copy, not the local
        entry.AndModify(v => v + 1).OrInsert(1);

        // Assert: the local still believes the key is vacant...
        Assert.True(entry.IsVacant);
        Assert.Equal(1, dict["key"]);

        // ...so inserting through it again collides with the key the copy added.
        // (try/catch rather than Assert.Throws because ref struct locals can't be captured by lambdas)
        Exception? thrown = null;
        try
        {
            entry.OrInsert(2);
        }
        catch (Exception ex)
        {
            thrown = ex;
        }

        Assert.IsType<ArgumentException>(thrown);
        Assert.Equal(1, dict["key"]);
    }

    [Fact]
    public void Entry_WithNullKey_ThrowsArgumentNullException()
    {
        // Arrange
        var dict = new Dictionary<string, int>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => dict.Entry(null!));
    }

    [Fact]
    public void AndModify_WhenUpdateFuncThrows_PreservesExistingValue()
    {
        // Arrange
        var dict = new Dictionary<string, int> { ["key"] = 42 };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            dict.Entry("key").AndModify(_ => throw new InvalidOperationException("update failed")));
        Assert.Equal(42, dict["key"]);
    }

    [Fact]
    public void CustomComparer_CaseInsensitiveKeys_RespectComparerAcrossOperations()
    {
        // Arrange
        var dict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["PlayerOne"] = 10
        };

        // Act & Assert: lookup with different casing finds the existing entry
        Assert.True(dict.Entry("playerone").IsOccupied);
        Assert.False(dict.Entry("playerone").IsVacant);

        var matched = dict.Entry("PLAYERONE") switch
        {
            OccupiedEntry<string, int> o => o.Value,
            VacantEntry<string, int> => -1,
        };
        Assert.Equal(10, matched);

        // AndModify and OrInsert update/read the existing slot without altering the stored key casing
        dict.Entry("playerone").AndModify(v => v + 5).OrInsert(0);
        Assert.Equal(15, dict["PlayerOne"]);
        Assert.Equal("PlayerOne", Assert.Single(dict.Keys));

        // OccupiedEntry.Insert, implicit union conversion, and Remove respect the comparer
        var occupied = dict.Entry("PLAYERONE").ToOccupied();
        occupied.Insert(99);
        Entry<string, int> convertedOccupied = occupied;
        Assert.True(convertedOccupied is OccupiedEntry<string, int> { Value: 99 });
        Assert.Equal("PlayerOne", Assert.Single(dict.Keys));

        Assert.Equal(99, occupied.Remove());
        Assert.Empty(dict);

        // VacantEntry insertion also uses the dictionary's comparer
        var vacant = dict.Entry("NewKey").ToVacant();
        vacant.Insert(7);
        Assert.True(dict.Entry("newkey") is OccupiedEntry<string, int> { Value: 7 });
    }

    [Fact]
    public void ValueTypeKeys_GuidAndRecordStruct_SupportFluentAndUnionOperations()
    {
        var id = Guid.NewGuid();
        var byGuid = new Dictionary<Guid, string>();

        byGuid.Entry(id).OrInsert("alice");
        var name = byGuid.Entry(id) switch
        {
            OccupiedEntry<Guid, string> occupied => occupied.Value,
            VacantEntry<Guid, string> => "none",
        };
        Assert.Equal("alice", name);

        var coordDict = new Dictionary<GridCoord, int>();
        var origin = new GridCoord(0, 0);
        coordDict.Entry(origin).OrDefault() = 5;
        coordDict.Entry(origin).AndModify(v => v + 10);
        Assert.Equal(15, coordDict[origin]);
    }

    [Fact]
    public void MutableStructValue_RefReturns_MutateFieldsInPlaceWithoutCopy()
    {
        var dict = new Dictionary<int, PlayerStats>();

        ref var p1 = ref dict.Entry(1).OrInsert(new PlayerStats { Score = 10, Visits = 1 });
        p1.Score += 25;
        p1.Visits++;
        Assert.Equal(new PlayerStats { Score = 35, Visits = 2 }, dict[1]);

        ref var p2 = ref dict.Entry(2).OrDefault();
        p2.Score = 5;
        p2.Visits = 1;
        Assert.Equal(new PlayerStats { Score = 5, Visits = 1 }, dict[2]);

        ref var p3 = ref dict.Entry(3).OrInsertWith(() => new PlayerStats { Score = 1, Visits = 1 });
        p3.Visits += 4;
        Assert.Equal(new PlayerStats { Score = 1, Visits = 5 }, dict[3]);

        ref var p4 = ref dict.Entry(4).OrInsertWithKey(id => new PlayerStats { Score = id * 10, Visits = 1 });
        p4.Score += 2;
        Assert.Equal(new PlayerStats { Score = 42, Visits = 1 }, dict[4]);
    }

    [Fact]
    public void ValueRef_UpdateOnlyInPlaceStructMutation_UpdatesExistingAndSkipsMissing()
    {
        var dict = new Dictionary<int, PlayerStats>
        {
            [1] = new PlayerStats { Score = 10, Visits = 1 }
        };

        // Existing key: mutates struct fields directly in dictionary storage
        if (dict.Entry(1) is { IsOccupied: true } existing)
        {
            ref var stats = ref existing.ValueRef;
            stats.Score += 25;
            stats.Visits++;
        }

        // Missing key: skipped without inserting a default entry
        if (dict.Entry(99) is { IsOccupied: true } missing)
        {
            missing.ValueRef.Visits++;
        }

        Assert.Equal(new PlayerStats { Score = 35, Visits = 2 }, dict[1]);
        Assert.False(dict.ContainsKey(99));
        Assert.Single(dict);
    }

    [Fact]
    public void ValueRef_AfterTransitionOrResize_PointsAtLiveStorage()
    {
        var dict = new Dictionary<string, int>();

        var entry = dict.Entry("a");
        entry.InsertEntry(10);
        entry.ValueRef += 5;
        Assert.Equal(15, dict["a"]);

        var resized = dict.Entry("a").AndModify(v =>
        {
            for (var i = 0; i < 100; i++)
            {
                dict[$"filler{i}"] = i;
            }

            return v + 5;
        });
        resized.ValueRef *= 2;
        Assert.Equal(40, dict["a"]);
    }

    [Fact]
    public void HashCollisions_MultipleKeysInSameBucket_IsolateEntriesAndRemovals()
    {
        var dict = new Dictionary<CollidingKey, int>();
        var k1 = new CollidingKey(1);
        var k2 = new CollidingKey(2);
        var k3 = new CollidingKey(3);
        var k4 = new CollidingKey(4);

        dict.Entry(k1).OrInsert(10);
        dict.Entry(k2).OrInsert(20);
        dict.Entry(k3).OrInsert(30);

        // Remove middle element in the collision chain, then probe/modify past it
        Assert.Equal(20, dict.Entry(k2).ToOccupied().Remove());
        Assert.True(dict.Entry(k2) is VacantEntry<CollidingKey, int>);

        dict.Entry(k3).AndModify(v => v + 5);
        dict.Entry(k4).OrInsert(40);

        Assert.Equal(10, dict[k1]);
        Assert.False(dict.ContainsKey(k2));
        Assert.Equal(35, dict[k3]);
        Assert.Equal(40, dict[k4]);
    }

    [Fact]
    public void OrInsertVariants_WhenInsertionTriggersResize_ReturnedRefPointsAtLiveStorage()
    {
        AssertRefSurvivesResize((dict, key) => ref dict.Entry(key).OrInsert(1));
        AssertRefSurvivesResize((dict, key) => ref dict.Entry(key).OrInsertWith(() => 1));
        AssertRefSurvivesResize((dict, key) => ref dict.Entry(key).OrInsertWithKey(_ => 1));
        AssertRefSurvivesResize((dict, key) =>
        {
            ref var slot = ref dict.Entry(key).OrDefault();
            slot = 1;
            return ref slot;
        });

        static void AssertRefSurvivesResize(RefInserter inserter)
        {
            var dict = new Dictionary<int, int>(capacity: 3);
            for (var i = 0; i < 64; i++)
            {
                ref var valueRef = ref inserter(dict, i);
                valueRef = i * 10;
            }

            for (var i = 0; i < 64; i++)
            {
                Assert.Equal(i * 10, dict[i]);
            }
        }
    }

    private delegate ref int RefInserter(Dictionary<int, int> dict, int key);

    private readonly record struct GridCoord(int X, int Y);

    private struct PlayerStats
    {
        public int Score;
        public int Visits;
    }

    private readonly struct CollidingKey(int id) : IEquatable<CollidingKey>
    {
        public int Id { get; } = id;
        public bool Equals(CollidingKey other) => Id == other.Id;
        public override bool Equals(object? obj) => obj is CollidingKey other && Equals(other);
        public override int GetHashCode() => 0;
    }
}
