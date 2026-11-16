namespace DictionaryEntry.Tests.BasicUsage;

public class EntryTests
{
    [Fact]
    public void Entry_WithExistingAndMissingKeys_ReportsOccupancyAndKey()
    {
        var dict = new Dictionary<string, int> { ["existing"] = 42 };

        var occupied = dict.Entry("existing");
        Assert.True(occupied.IsOccupied);
        Assert.False(occupied.IsVacant);
        Assert.Equal("existing", occupied.Key);

        var vacant = dict.Entry("missing");
        Assert.False(vacant.IsOccupied);
        Assert.True(vacant.IsVacant);
        Assert.Equal("missing", vacant.Key);
    }

    [Fact]
    public void ValueRef_WhenOccupied_ReturnsMutableReference()
    {
        // Arrange
        var dict = new Dictionary<string, int> { ["key"] = 42 };

        // Act
        if (dict.Entry("key") is { IsOccupied: true } entry)
        {
            entry.ValueRef += 8;
        }

        // Assert
        Assert.Equal(50, dict["key"]);
    }

    [Fact]
    public void ValueRef_WhenVacant_ThrowsKeyNotFoundException()
    {
        // Arrange
        var dict = new Dictionary<string, int>();

        // Act & Assert
        var ex = Assert.Throws<KeyNotFoundException>(() => _ = dict.Entry("missing").ValueRef);
        Assert.Contains("'missing'", ex.Message);
        Assert.Empty(dict);
    }

    [Fact]
    public void AndModify_WhenExists_UpdatesValue()
    {
        // Arrange
        var dict = new Dictionary<string, int> { ["key"] = 42 };

        // Act
        var entry = dict.Entry("key").AndModify(value => value + 1);

        // Assert
        Assert.Equal(43, dict["key"]);
        Assert.Equal(43, entry.ToOccupied().Value);
    }

    [Fact]
    public void AndModify_WhenNotExists_DoesNothingWithoutInvokingCallback()
    {
        // Arrange
        var dict = new Dictionary<string, int>();
        var invoked = false;

        // Act
        var entry = dict.Entry("key").AndModify(value =>
        {
            invoked = true;
            return value + 1;
        });

        // Assert
        Assert.True(entry.IsVacant);
        Assert.False(invoked);
        Assert.Empty(dict);
    }

    [Fact]
    public void AndModifyRef_WhenExists_MutatesValueInPlaceAndChains()
    {
        // Arrange
        var dict = new Dictionary<string, int> { ["key"] = 42 };

        // Act
        var value = dict.Entry("key").AndModify(static (ref v) => v += 8).OrInsert(0);

        // Assert
        Assert.Equal(50, value);
        Assert.Equal(50, dict["key"]);
    }

    [Fact]
    public void AndModifyRef_WhenNotExists_SkipsCallbackAndChainsIntoOrInsert()
    {
        // Arrange
        var dict = new Dictionary<string, int>();
        var invoked = false;

        // Act
        var value = dict.Entry("key")
            .AndModify((ref v) =>
            {
                invoked = true;
                v += 8;
            })
            .OrInsert(10);

        // Assert
        Assert.False(invoked);
        Assert.Equal(10, value);
        Assert.Equal(10, dict["key"]);
    }

    [Fact]
    public void OrInsert_WhenNotExists_InsertsValueAndReturnsMutableReference()
    {
        // Arrange
        var dict = new Dictionary<string, int>();

        // Act & Assert
        ref var valueRef = ref dict.Entry("key").OrInsert(42);
        Assert.Equal(42, valueRef);
        Assert.Equal(42, dict["key"]);

        valueRef = 100;
        Assert.Equal(100, dict["key"]);
    }

    [Fact]
    public void OrInsert_WhenExists_PreservesValueAndReturnsMutableReference()
    {
        // Arrange
        var dict = new Dictionary<string, int> { ["key"] = 42 };

        // Act & Assert
        ref var valueRef = ref dict.Entry("key").OrInsert(43);
        Assert.Equal(42, valueRef);
        Assert.Equal(42, dict["key"]);

        valueRef = 100;
        Assert.Equal(100, dict["key"]);
    }

    [Fact]
    public void OrInsertWithKey_WhenNotExists_InsertsValueAndReturnsMutableReference()
    {
        var dict = new Dictionary<string, int>();

        ref var value = ref dict.Entry("key").OrInsertWithKey(key => key.Length);

        Assert.Equal(3, dict["key"]);
        Assert.Equal(3, value);

        value *= 10;
        Assert.Equal(30, dict["key"]);
    }

    [Fact]
    public void OrInsertWithKey_WhenExists_DoesNotInvokeFactoryAndReturnsMutableReference()
    {
        var dict = new Dictionary<string, int> { ["key"] = 42 };
        var factoryCalled = false;

        ref var value = ref dict.Entry("key").OrInsertWithKey(key =>
        {
            factoryCalled = true;
            return key.Length;
        });

        Assert.False(factoryCalled);
        Assert.Equal(42, dict["key"]);
        Assert.Equal(42, value);

        value += 2;
        Assert.Equal(44, dict["key"]);
    }

    [Fact]
    public void OrDefault_WhenNotExists_InsertsDefaultAndReturnsMutableReference()
    {
        // Arrange
        var dict = new Dictionary<string, int>();

        // Act & Assert
        ref var valueRef = ref dict.Entry("key").OrDefault();
        Assert.Equal(0, valueRef);
        Assert.Equal(0, dict["key"]);

        valueRef = 7;
        Assert.Equal(7, dict["key"]);
    }

    [Fact]
    public void OrDefault_WhenExists_PreservesValueAndReturnsMutableReference()
    {
        // Arrange
        var dict = new Dictionary<string, int> { ["key"] = 42 };

        // Act & Assert
        ref var valueRef = ref dict.Entry("key").OrDefault();
        Assert.Equal(42, valueRef);
        Assert.Equal(42, dict["key"]);

        valueRef += 8;
        Assert.Equal(50, dict["key"]);
    }

    [Fact]
    public void InsertEntry_WhenNewKey_InsertsValue()
    {
        // Arrange
        var dict = new Dictionary<string, int>();

        // Act
        var occupied = dict.Entry("key").InsertEntry(42);

        // Assert
        Assert.True(dict.ContainsKey("key"));
        Assert.Equal(42, dict["key"]);
        Assert.Equal(42, occupied.Value);
    }

    [Fact]
    public void InsertEntry_WhenExists_InsertsValue()
    {
        // Arrange
        var dict = new Dictionary<string, int>
        {
            ["key"] = 42
        };

        // Act
        var occupied = dict.Entry("key").InsertEntry(43);

        // Assert
        Assert.True(dict.ContainsKey("key"));
        Assert.Equal(43, dict["key"]);
        Assert.Equal(43, occupied.Value);
    }

    [Fact]
    public void TryGetOccupied_AndTryGetVacant_ReturnExpectedCaseOrDefault()
    {
        var dict = new Dictionary<string, int> { ["existing"] = 42 };

        Assert.True(dict.Entry("existing").TryGetOccupied(out var occupied));
        Assert.Equal(42, occupied.Value);
        Assert.False(dict.Entry("existing").TryGetVacant(out var nonVacant));
        Assert.Equal(default, nonVacant);

        Assert.False(dict.Entry("missing").TryGetOccupied(out var nonOccupied));
        Assert.Equal(default, nonOccupied);
        Assert.True(dict.Entry("missing").TryGetVacant(out var vacant));
        Assert.Equal("missing", vacant.Key);
    }
}
