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
    public void DefaultEntry_AllOperations_ThrowInvalidOperationExceptionWithoutInvokingCallbacks()
    {
        var invoked = false;
        static Entry<string, int> Default() => default;

        Assert.Contains("not associated with a dictionary",
            Assert.Throws<InvalidOperationException>(() => Default().ToOccupied()).Message);
        Assert.Contains("not associated with a dictionary",
            Assert.Throws<InvalidOperationException>(() => _ = Default().ValueRef).Message);
        Assert.Contains("not associated with a dictionary",
            Assert.Throws<InvalidOperationException>(() => Default().ToVacant()).Message);
        Assert.Contains("not associated with a dictionary",
            Assert.Throws<InvalidOperationException>(() => Default().AndModify(v => { invoked = true; return v; })).Message);

        Assert.Throws<InvalidOperationException>(() => Default().AndModify(1, (v, s) => { invoked = true; return v + s; }));
        Assert.Throws<InvalidOperationException>(() => Default().AndModify((ref v) => { invoked = true; v++; }));
        Assert.Throws<InvalidOperationException>(() => Default().AndModify(1, (ref v, s) => { invoked = true; v += s; }));
        Assert.Throws<InvalidOperationException>(() => Default().Match(_ => invoked = true, _ => invoked = true));
        Assert.Throws<InvalidOperationException>(() => Default().Match(1, (_, _) => invoked = true, (_, _) => invoked = true));
        Assert.Throws<InvalidOperationException>(() => Default().Match(_ => { invoked = true; return 1; }, _ => { invoked = true; return 2; }));
        Assert.Throws<InvalidOperationException>(() => Default().Match(1, (_, s) => { invoked = true; return s; }, (_, s) => { invoked = true; return s; }));
        Assert.Throws<InvalidOperationException>(() => Default().OrInsert(1));
        Assert.Throws<InvalidOperationException>(() => Default().OrDefault());
        Assert.Throws<InvalidOperationException>(() => Default().InsertEntry(1));
        Assert.Throws<InvalidOperationException>(() => Default().OrInsertWith(() => { invoked = true; return 1; }));
        Assert.Throws<InvalidOperationException>(() => Default().OrInsertWith(1, s => { invoked = true; return s; }));
        Assert.Throws<InvalidOperationException>(() => Default().OrInsertWithKey(_ => { invoked = true; return 1; }));
        Assert.Throws<InvalidOperationException>(() => Default().OrInsertWithKey(1, (_, s) => { invoked = true; return s; }));

        Assert.Throws<InvalidOperationException>(() => default(OccupiedEntry<string, int>).Insert(1));
        Assert.Throws<InvalidOperationException>(() => default(OccupiedEntry<string, int>).Remove());
        Assert.Throws<InvalidOperationException>(() => default(OccupiedEntry<string, int>).RemoveEntry());
        Assert.Throws<InvalidOperationException>(() => default(VacantEntry<string, int>).Insert(1));
        Assert.Throws<InvalidOperationException>(() => default(VacantEntry<string, int>).InsertEntry(1));
        Assert.False(invoked);
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
    public void ValueRef_AfterVacantTransition_PointsAtLiveStorage()
    {
        var dict = new Dictionary<string, int>();

        var entry = dict.Entry("a");
        entry.InsertEntry(10);
        entry.ValueRef += 5;
        Assert.Equal(15, dict["a"]);

        var viaFactory = dict.Entry("b");
        viaFactory.OrInsertWith(() =>
        {
            for (var i = 0; i < 100; i++)
            {
                dict[$"filler{i}"] = i;
            }

            return 20;
        });
        viaFactory.ValueRef *= 2;
        Assert.Equal(40, dict["b"]);
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
}
