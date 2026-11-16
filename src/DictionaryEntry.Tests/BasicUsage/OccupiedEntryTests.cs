namespace DictionaryEntry.Tests.BasicUsage;

public class OccupiedEntryTests
{
    [Fact]
    public void Key_Value_AndDeconstruct_ReturnOccupiedSnapshot()
    {
        var dict = new Dictionary<string, int> { ["key"] = 42 };
        var occupied = dict.Entry("key").ToOccupied();

        Assert.Equal("key", occupied.Key);
        Assert.Equal(42, occupied.Value);

        var (key, value) = occupied;
        Assert.Equal("key", key);
        Assert.Equal(42, value);
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
    public void RemoveEntry_RemovesKeyAndReturnsNamedTuple()
    {
        // Arrange
        var dict = new Dictionary<string, int> { ["key"] = 42 };

        // Act
        var occupied = dict.Entry("key").ToOccupied();
        var removed = occupied.RemoveEntry();

        // Assert
        Assert.Equal("key", removed.Key);
        Assert.Equal(42, removed.Value);
        Assert.False(dict.ContainsKey("key"));
    }

    [Fact]
    public void RemoveEntry_WithCaseInsensitiveComparer_ReturnsLookupKeyAndRemovedValue()
    {
        var dict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["Alpha"] = 42,
            ["Beta"] = 99,
        };

        // Fast path (known slot location)
        var fastRemoved = dict.Entry("alpha").ToOccupied().RemoveEntry();
        Assert.Equal("alpha", fastRemoved.Key);
        Assert.Equal(42, fastRemoved.Value);
        Assert.False(dict.ContainsKey("Alpha"));

        // Stale fallback path (after structural resize)
        var staleOccupied = dict.Entry("BETA").ToOccupied();
        dict.EnsureCapacity(100);
        var (fallbackKey, fallbackValue) = staleOccupied.RemoveEntry();
        Assert.Equal("BETA", fallbackKey);
        Assert.Equal(99, fallbackValue);
        Assert.Empty(dict);
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
