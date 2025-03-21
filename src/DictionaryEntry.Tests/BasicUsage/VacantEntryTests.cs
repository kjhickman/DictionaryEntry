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
        vacant.Insert(42);

        // Assert
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
        Assert.Equal("key", vacant.Key());
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
        Assert.Equal(42, occupied.Value());
    }
}
