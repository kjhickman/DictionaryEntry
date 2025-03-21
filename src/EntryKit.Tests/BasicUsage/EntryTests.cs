namespace EntryKit.Tests.BasicUsage;

public class EntryTests
{
    [Fact]
    public void Entry_WithExistingKey_IsOccupied()
    {
        // Arrange
        var dict = new Dictionary<string, int> { ["key"] = 42 };

        // Act
        var entry = dict.Entry("key");

        // Assert
        Assert.True(entry.IsOccupied());
        Assert.False(entry.IsVacant());
    }

    [Fact]
    public void Entry_WithNonExistingKey_IsVacant()
    {
        // Arrange
        var dict = new Dictionary<string, int>();

        // Act
        var entry = dict.Entry("key");

        // Assert
        Assert.False(entry.IsOccupied());
        Assert.True(entry.IsVacant());
    }

    [Fact]
    public void Key_ReturnsEntryKey()
    {
        // Arrange
        var dict = new Dictionary<string, int>();

        // Act
        var entry = dict.Entry("key");

        // Assert
        Assert.Equal("key", entry.Key());
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
        Assert.Equal(43, entry.ToOccupied().Value());
    }

    [Fact]
    public void AndModify_WhenNotExists_DoesNothing()
    {
        // Arrange
        var dict = new Dictionary<string, int>();

        // Act
        var entry = dict.Entry("key").AndModify(value => value + 1);

        // Assert
        Assert.True(entry.IsVacant());
    }

    [Fact]
    public void OrInsert_WhenNotExists_InsertsValue()
    {
        // Arrange
        var dict = new Dictionary<string, int>();

        // Act
        var value = dict.Entry("key").OrInsert(42);

        // Assert
        Assert.True(dict.ContainsKey("key"));
        Assert.Equal(42, dict["key"]);
        Assert.Equal(42, value);
    }

    [Fact]
    public void OrInsert_WhenExists_DoesNothing()
    {
        // Arrange
        var dict = new Dictionary<string, int>
        {
            ["key"] = 42
        };

        // Act
        var value = dict.Entry("key").OrInsert(43);

        // Assert
        Assert.True(dict.ContainsKey("key"));
        Assert.Equal(42, dict["key"]);
        Assert.Equal(42, value);
    }

    [Fact]
    public void OrInsertWith_InsertsValue()
    {
        // Arrange
        var dict = new Dictionary<string, int>();

        // Act
        var value = dict.Entry("key").OrInsertWith(() => 42);

        // Assert
        Assert.True(dict.ContainsKey("key"));
        Assert.Equal(42, dict["key"]);
        Assert.Equal(42, value);
    }

    [Fact]
    public void OrInsertWithKey_WhenNotExists_InsertsValue()
    {
        // Arrange
        var dict = new Dictionary<string, int>();

        // Act
        var value = dict.Entry("key").OrInsertWithKey(key => key.Length);

        // Assert
        Assert.True(dict.ContainsKey("key"));
        Assert.Equal(3, dict["key"]);
        Assert.Equal(3, value);
    }

    [Fact]
    public void OrInsertWithKey_WhenExists_DoesNothing()
    {
        // Arrange
        var dict = new Dictionary<string, int>
        {
            ["key"] = 42
        };

        // Act
        var value = dict.Entry("key").OrInsertWithKey(key => key.Length);

        // Assert
        Assert.True(dict.ContainsKey("key"));
        Assert.Equal(42, dict["key"]);
        Assert.Equal(42, value);
    }

    [Fact]
    public void OrDefault_WhenNotExists_InsertsDefaultValue()
    {
        // Arrange
        var dict = new Dictionary<string, int>();

        // Act
        var value = dict.Entry("key").OrDefault();

        // Assert
        Assert.True(dict.ContainsKey("key"));
        Assert.Equal(0, dict["key"]);
        Assert.Equal(0, value);
    }

    [Fact]
    public void OrDefault_WhenExists_DoesNothing()
    {
        // Arrange
        var dict = new Dictionary<string, int>
        {
            ["key"] = 42
        };

        // Act
        var value = dict.Entry("key").OrDefault();

        // Assert
        Assert.True(dict.ContainsKey("key"));
        Assert.Equal(42, dict["key"]);
        Assert.Equal(42, value);
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
        Assert.Equal(42, occupied.Value());
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
        Assert.Equal(43, occupied.Value());
    }

    [Fact]
    public void TryGetOccupied_WhenExists_ReturnsValue()
    {
        // Arrange
        var dict = new Dictionary<string, int>
        {
            ["key"] = 42
        };

        // Act
        var success = dict.Entry("key").TryGetOccupied(out var occupied);

        // Assert
        Assert.True(success);
        Assert.Equal(42, occupied.Value());
    }

    [Fact]
    public void TryGetOccupied_WhenNotExists_ReturnsFalse()
    {
        // Arrange
        var dict = new Dictionary<string, int>();

        // Act
        var success = dict.Entry("key").TryGetOccupied(out var occupied);

        // Assert
        Assert.False(success);
        Assert.Equal(default, occupied);
    }

    [Fact]
    public void TryGetVacant_WhenExists_ReturnsFalse()
    {
        // Arrange
        const string expectedKey = "key";
        var dict = new Dictionary<string, int>
        {
            [expectedKey] = 42
        };

        // Act
        var success = dict.Entry("key").TryGetVacant(out var vacant);

        // Assert
        Assert.False(success);
        Assert.Equal(default, vacant);
    }

    [Fact]
    public void TryGetVacant_WhenNotExists_ReturnsValue()
    {
        // Arrange
        const string expectedKey = "key";
        var dict = new Dictionary<string, int>();

        // Act
        var success = dict.Entry("key").TryGetVacant(out var vacant);

        // Assert
        Assert.True(success);
        Assert.Equal(expectedKey, vacant.Key());
    }
}
