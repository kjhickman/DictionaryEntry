namespace DictionaryEntry.Tests.BasicUsage;

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
        Assert.Equal("key", occupied.Key);
    }

    [Fact]
    public void Value_ReturnsCurrentValue()
    {
        // Arrange
        var dict = new Dictionary<string, int> { ["key"] = 42 };

        // Act
        var occupied = dict.Entry("key").ToOccupied();

        // Assert
        Assert.Equal(42, occupied.Value);
    }

    [Fact]
    public void Deconstruct_ReturnsKeyAndValue()
    {
        // Arrange
        var dict = new Dictionary<string, int> { ["key"] = 42 };

        // Act
        var (key, value) = dict.Entry("key").ToOccupied();

        // Assert
        Assert.Equal("key", key);
        Assert.Equal(42, value);
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

    [Fact]
    public void Insert_ReturnsNewValueAndDoesNotChangeSnapshotValue()
    {
        // Arrange
        var dict = new Dictionary<string, int> { ["key"] = 42 };
        var occupied = dict.Entry("key").ToOccupied();

        // Act
        var returned = occupied.Insert(99);

        // Assert
        Assert.Equal(99, returned);
        Assert.Equal(99, dict["key"]);
        Assert.Equal(42, occupied.Value);
    }

    [Fact]
    public void Remove_AfterInsert_ReturnsValueActuallyRemoved()
    {
        // Arrange
        var dict = new Dictionary<string, int> { ["key"] = 1 };
        var occupied = dict.Entry("key").ToOccupied();
        occupied.Insert(5);

        // Act
        var removed = occupied.Remove();

        // Assert
        Assert.Equal(5, removed);
        Assert.Empty(dict);
    }

    [Fact]
    public void RemoveEntry_AfterInsert_ReturnsValueActuallyRemoved()
    {
        // Arrange
        var dict = new Dictionary<string, int> { ["key"] = 1 };
        var occupied = dict.Entry("key").ToOccupied();
        occupied.Insert(5);

        // Act
        var (key, value) = occupied.RemoveEntry();

        // Assert
        Assert.Equal("key", key);
        Assert.Equal(5, value);
        Assert.Empty(dict);
    }

    [Fact]
    public void Remove_AfterExternalUpdate_ReturnsCurrentValue()
    {
        // Arrange
        var dict = new Dictionary<string, int> { ["key"] = 1 };
        var occupied = dict.Entry("key").ToOccupied();
        dict["key"] = 99;

        // Act & Assert
        Assert.Equal(99, occupied.Remove());
    }

    [Fact]
    public void RemoveEntry_AfterExternalUpdate_ReturnsCurrentValue()
    {
        // Arrange
        var dict = new Dictionary<string, int> { ["key"] = 1 };
        var occupied = dict.Entry("key").ToOccupied();
        dict["key"] = 99;

        // Act & Assert
        Assert.Equal(("key", 99), occupied.RemoveEntry());
    }

    [Fact]
    public void Remove_CalledTwice_ThrowsKeyNotFoundException()
    {
        // Arrange
        var dict = new Dictionary<string, int> { ["key"] = 42 };
        var occupied = dict.Entry("key").ToOccupied();
        occupied.Remove();

        // Act & Assert
        var ex = Assert.Throws<KeyNotFoundException>(() => occupied.Remove());
        Assert.Contains("'key'", ex.Message);
    }

    [Fact]
    public void RemoveEntry_WhenKeyRemovedExternally_ThrowsKeyNotFoundException()
    {
        // Arrange
        var dict = new Dictionary<string, int> { ["key"] = 42 };
        var occupied = dict.Entry("key").ToOccupied();
        dict.Remove("key");

        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() => occupied.RemoveEntry());
        Assert.Empty(dict);
    }

    [Fact]
    public void Remove_WithNullValue_ReturnsNull()
    {
        // Arrange
        var dict = new Dictionary<string, string?> { ["key"] = null };
        var occupied = dict.Entry("key").ToOccupied();

        // Act
        var removed = occupied.Remove();

        // Assert
        Assert.Null(removed);
        Assert.Empty(dict);
    }

    [Fact]
    public void Remove_WhenKeyRemovedExternally_ThrowsKeyNotFoundException()
    {
        // Arrange
        var dict = new Dictionary<string, int> { ["key"] = 42 };
        var occupied = dict.Entry("key").ToOccupied();
        dict.Remove("key");

        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() => occupied.Remove());
        Assert.Empty(dict);
    }

    [Fact]
    public void RemoveEntry_CalledTwice_ThrowsKeyNotFoundException()
    {
        // Arrange
        var dict = new Dictionary<string, int> { ["key"] = 42 };
        var occupied = dict.Entry("key").ToOccupied();
        occupied.RemoveEntry();

        // Act & Assert
        var ex = Assert.Throws<KeyNotFoundException>(() => occupied.RemoveEntry());
        Assert.Contains("'key'", ex.Message);
    }

    [Fact]
    public void RemoveEntry_WithNullValue_ReturnsNullInTuple()
    {
        // Arrange
        var dict = new Dictionary<string, string?> { ["key"] = null };
        var occupied = dict.Entry("key").ToOccupied();

        // Act
        var (key, value) = occupied.RemoveEntry();

        // Assert
        Assert.Equal("key", key);
        Assert.Null(value);
        Assert.Empty(dict);
    }

    [Fact]
    public void Insert_WhenKeyRemovedExternally_ReinsertsValue()
    {
        // Arrange
        var dict = new Dictionary<string, int> { ["key"] = 42 };
        var occupied = dict.Entry("key").ToOccupied();
        dict.Remove("key");

        // Act
        var inserted = occupied.Insert(99);

        // Assert
        Assert.Equal(99, inserted);
        Assert.Equal(99, dict["key"]);
        Assert.Equal(42, occupied.Value);
    }
}
