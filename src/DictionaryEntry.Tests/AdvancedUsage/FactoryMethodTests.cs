namespace DictionaryEntry.Tests.AdvancedUsage;

public class FactoryMethodTests
{
    [Fact]
    public void OrInsertWith_WithVacantEntry_CallsFactoryInsertsValueAndReturnsMutableReference()
    {
        var dict = new Dictionary<string, int>();
        var factoryCalled = false;

        ref var value = ref dict.Entry("key").OrInsertWith(() => {
            factoryCalled = true;
            return 42;
        });

        Assert.True(factoryCalled);
        Assert.Equal(42, dict["key"]);
        Assert.Equal(42, value);

        value += 5;
        Assert.Equal(47, dict["key"]);
    }

    [Fact]
    public void OrInsertWith_WithOccupiedEntry_DoesNotCallFactoryAndReturnsMutableReference()
    {
        var dict = new Dictionary<string, int> { ["key"] = 42 };
        var factoryCalled = false;

        ref var value = ref dict.Entry("key").OrInsertWith(() => {
            factoryCalled = true;
            return 99;
        });

        Assert.False(factoryCalled);
        Assert.Equal(42, dict["key"]);
        Assert.Equal(42, value);

        value += 10;
        Assert.Equal(52, dict["key"]);
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
