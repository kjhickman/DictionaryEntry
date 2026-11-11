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
    public void DefaultEntry_AndModify_IsNoOp()
    {
        // Arrange
        Entry<string, int> entry = default;
        var invoked = false;

        // Act
        var result = entry.AndModify(v => { invoked = true; return v; });

        // Assert
        Assert.False(invoked);
        Assert.True(result is null);
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
}
