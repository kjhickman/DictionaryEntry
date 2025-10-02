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
    public async Task OrInsertWithAsync_WithVacantEntry_CallsFactoryAndInsertsValue()
    {
        // Arrange
        var dict = new Dictionary<string, int>();
        var factoryCalled = false;

        // Act
        var value = await dict.Entry("key").OrInsertWithAsync(async () => {
            factoryCalled = true;
            await Task.Delay(1); // Simulate async work
            return 42;
        });

        // Assert
        Assert.True(factoryCalled);
        Assert.Equal(42, dict["key"]);
        Assert.Equal(42, value);
    }

    [Fact]
    public async Task OrInsertWithAsync_WithOccupiedEntry_DoesNotCallFactory()
    {
        // Arrange
        var dict = new Dictionary<string, int> { ["key"] = 42 };
        var factoryCalled = false;

        // Act
        var value = await dict.Entry("key").OrInsertWithAsync(async () => {
            factoryCalled = true;
            await Task.Delay(1); // Simulate async work
            return 99;
        });

        // Assert
        Assert.False(factoryCalled);
        Assert.Equal(42, dict["key"]);
        Assert.Equal(42, value);
    }

    [Fact]
    public async Task OrInsertWithKeyAsync_UsesKeyInFactory()
    {
        // Arrange
        var dict = new Dictionary<string, int>();

        // Act
        var value = await dict.Entry("key").OrInsertWithKeyAsync(async k => {
            await Task.Delay(1); // Simulate async work
            return k.Length;
        });

        // Assert
        Assert.Equal(3, dict["key"]); // "key".Length == 3
        Assert.Equal(3, value);
    }

    [Fact]
    public async Task OrInsertWithKeyAsync_WithOccupiedEntry_DoesNotCallFactory()
    {
        // Arrange
        var dict = new Dictionary<string, int> { ["key"] = 42 };
        var factoryCalled = false;

        // Act
        var value = await dict.Entry("key").OrInsertWithKeyAsync(async k => {
            factoryCalled = true;
            await Task.Delay(1); // Simulate async work
            return k.Length;
        });

        // Assert
        Assert.False(factoryCalled);
        Assert.Equal(42, dict["key"]);
        Assert.Equal(42, value);
    }

    [Fact]
    public async Task OrInsertWithAsync_WithException_DoesNotInsertValue()
    {
        // Arrange
        var dict = new Dictionary<string, int>();
        var expectedException = new InvalidOperationException("Test exception");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await dict.Entry("key").OrInsertWithAsync(async () => {
                await Task.Delay(1);
                throw expectedException;
            })
        );

        Assert.Same(expectedException, exception);
        Assert.False(dict.ContainsKey("key"));
    }
}
