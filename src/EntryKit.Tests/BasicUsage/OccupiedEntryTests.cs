namespace EntryKit.Tests.BasicUsage;

public class OccupiedEntryTests
{
    [Fact]
    public void Key_ReturnsCurrentKey()
    {
        // Arrange
        var dict = new Dictionary<string, int> { ["key"] = 42 };

        // Act
        var occupied = dict.Entry("key").ToOccupied();

        // Assert
        Assert.Equal("key", occupied.Key());
    }

    [Fact]
    public void Value_ReturnsCurrentValue()
    {
        // Arrange
        var dict = new Dictionary<string, int> { ["key"] = 42 };

        // Act
        var occupied = dict.Entry("key").ToOccupied();

        // Assert
        Assert.Equal(42, occupied.Value());
    }

    [Fact]
    public void Insert_UpdatesValue()
    {
        // Arrange
        var dict = new Dictionary<string, int> { ["key"] = 42 };

        // Act
        var occupied = dict.Entry("key").ToOccupied();
        occupied.Insert(99);

        // Assert
        Assert.Equal(99, dict["key"]);
    }

    [Fact]
    public void Remove_RemovesKeyAndReturnsValue()
    {
        // Arrange
        var dict = new Dictionary<string, int> { ["key"] = 42 };

        // Act
        var occupied = dict.Entry("key").ToOccupied();
        var value = occupied.Remove();

        // Assert
        Assert.Equal(42, value);
        Assert.False(dict.ContainsKey("key"));
    }

    [Fact]
    public void RemoveEntry_RemovesKeyAndReturnsTuple()
    {
        // Arrange
        var dict = new Dictionary<string, int> { ["key"] = 42 };

        // Act
        var occupied = dict.Entry("key").ToOccupied();
        var (key, value) = occupied.RemoveEntry();

        // Assert
        Assert.Equal("key", key);
        Assert.Equal(42, value);
        Assert.False(dict.ContainsKey("key"));
    }
}
