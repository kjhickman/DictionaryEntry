namespace DictionaryEntry.Tests.BasicUsage;

public class VacantEntryTests
{
    [Fact]
    public void Insert_AddsKeyValue()
    {
        // Arrange
        var dict = new Dictionary<string, int>();

        // Act
        var vacant = dict.Entry("key").ToVacant();
        var inserted = vacant.Insert(42);

        // Assert
        Assert.Equal(42, inserted);
        Assert.True(dict.ContainsKey("key"));
        Assert.Equal(42, dict["key"]);
    }

    [Fact]
    public void Key_ReturnsEntryKey()
    {
        // Arrange
        var dict = new Dictionary<string, int>();

        // Act
        var vacant = dict.Entry("key").ToVacant();

        // Assert
        Assert.Equal("key", vacant.Key);
    }

    [Fact]
    public void ToOccupiedEntry_InsertsValueAndReturnsOccupied()
    {
        // Arrange
        var dict = new Dictionary<string, int>();

        // Act
        var vacant = dict.Entry("key").ToVacant();
        var occupied = vacant.InsertEntry(42);

        // Assert
        Assert.True(dict.ContainsKey("key"));
        Assert.Equal(42, dict["key"]);
        Assert.Equal(42, occupied.Value);
    }

    [Fact]
    public void Insert_WhenKeyAlreadyPresent_OverwritesExistingValue()
    {
        // Arrange
        var dict = new Dictionary<string, int>();
        var vacant = dict.Entry("key").ToVacant();
        dict["key"] = 10;

        // Act
        var returned = vacant.Insert(99);

        // Assert
        Assert.Equal(99, returned);
        Assert.Equal(99, dict["key"]);
    }

    [Fact]
    public void InsertEntry_WhenKeyAlreadyPresent_OverwritesAndReturnsOccupiedEntry()
    {
        // Arrange
        var dict = new Dictionary<string, int>();
        var vacant = dict.Entry("key").ToVacant();
        vacant.Insert(10);

        // Act
        var occupied = vacant.InsertEntry(55);

        // Assert
        Assert.Equal("key", occupied.Key);
        Assert.Equal(55, occupied.Value);
        Assert.Equal(55, dict["key"]);
    }

    [Fact]
    public void InsertEntry_ReturnedOccupiedEntry_SupportsSubsequentUpdateAndRemove()
    {
        // Arrange
        var dict = new Dictionary<string, int>();

        // Act
        var occupied = dict.Entry("key").ToVacant().InsertEntry(42);
        occupied.Insert(100);
        var removed = occupied.Remove();

        // Assert
        Assert.Equal(42, occupied.Value);
        Assert.Equal(100, removed);
        Assert.Empty(dict);
    }
}
