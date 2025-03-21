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
}
