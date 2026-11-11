namespace DictionaryEntry.Tests.AdvancedUsage;

public class FactoryMethodTests
{
    [Fact]
    public void OrInsertWith_WithVacantEntry_CallsFactoryAndInsertsValue()
    {
        // Arrange
        var dict = new Dictionary<string, int>();
        var factoryCalled = false;

        // Act
        ref var value = ref dict.Entry("key").OrInsertWith(() => {
            factoryCalled = true;
            return 42;
        });

        // Assert
        Assert.True(factoryCalled);
        Assert.Equal(42, dict["key"]);
        Assert.Equal(42, value);
    }

    [Fact]
    public void OrInsertWith_WithOccupiedEntry_DoesNotCallFactory()
    {
        // Arrange
        var dict = new Dictionary<string, int> { ["key"] = 42 };
        var factoryCalled = false;

        // Act
        ref var value = ref dict.Entry("key").OrInsertWith(() => {
            factoryCalled = true;
            return 99;
        });

        // Assert
        Assert.False(factoryCalled);
        Assert.Equal(42, dict["key"]);
        Assert.Equal(42, value);
    }

    [Fact]
    public void OrInsertWithKey_UsesKeyInFactory()
    {
        // Arrange
        var dict = new Dictionary<string, int>();

        // Act
        ref var value = ref dict.Entry("key").OrInsertWithKey(k => k.Length);

        // Assert
        Assert.Equal(3, dict["key"]); // "key".Length == 3
        Assert.Equal(3, value);
    }

    [Fact]
    public void OrInsertWith_ReturnsMutableReference_ForVacantAndOccupiedEntries()
    {
        // Arrange
        var dict = new Dictionary<string, int>();

        // Act: mutate through ref returned from vacant insertion
        ref var insertedRef = ref dict.Entry("key").OrInsertWith(() => 10);
        insertedRef += 5;

        // Assert
        Assert.Equal(15, dict["key"]);

        // Act: mutate through ref returned from existing entry
        ref var existingRef = ref dict.Entry("key").OrInsertWith(() => 999);
        existingRef += 10;

        // Assert
        Assert.Equal(25, dict["key"]);
    }

    [Fact]
    public void OrInsertWithKey_ReturnsMutableReference_ForVacantAndOccupiedEntries()
    {
        // Arrange
        var dict = new Dictionary<string, int>();

        // Act: mutate through ref returned from vacant insertion
        ref var insertedRef = ref dict.Entry("key").OrInsertWithKey(k => k.Length);
        insertedRef *= 10;

        // Assert
        Assert.Equal(30, dict["key"]);

        // Act: mutate through ref returned from existing entry
        ref var existingRef = ref dict.Entry("key").OrInsertWithKey(_ => 999);
        existingRef += 2;

        // Assert
        Assert.Equal(32, dict["key"]);
    }

    [Fact]
    public void OrInsertWith_WhenFactoryThrows_DoesNotInsertDefaultEntry()
    {
        // Arrange
        var dict = new Dictionary<string, int>();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            dict.Entry("key").OrInsertWith(() => throw new InvalidOperationException("factory failed")));
        Assert.Empty(dict);
        Assert.False(dict.ContainsKey("key"));
    }

    [Fact]
    public void OrInsertWithKey_WhenFactoryThrows_DoesNotInsertDefaultEntry()
    {
        // Arrange
        var dict = new Dictionary<string, int>();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            dict.Entry("key").OrInsertWithKey(_ => throw new InvalidOperationException("factory failed")));
        Assert.Empty(dict);
        Assert.False(dict.ContainsKey("key"));
    }
}
