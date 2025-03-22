// ReSharper disable PreferConcreteValueOverDefault
namespace DictionaryEntry.Tests.AdvancedUsage;

public class EdgeCaseTests
{
    [Fact]
    public void ToOccupied_OnVacantEntry_ThrowsKeyNotFoundException()
    {
        // Arrange
        var dict = new Dictionary<string, int>();

        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() => dict.Entry("key").ToOccupied());
    }

    [Fact]
    public void ToVacant_OnOccupiedEntry_ThrowsException()
    {
        // Arrange
        var dict = new Dictionary<string, int> { ["key"] = 42 };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => dict.Entry("key").ToVacant());
    }

    [Fact]
    public void OrDefault_WithVacantEntry_InsertsDefaultValue()
    {
        // Arrange
        var dict = new Dictionary<string, int>();

        // Act
        ref var value = ref dict.Entry("key").OrDefault();

        // Assert
        Assert.Equal(default, dict["key"]);
        Assert.Equal(default, value);

        // Test with reference type
        var dictStr = new Dictionary<string, string>();
        ref var strValue = ref dictStr.Entry("key").OrDefault();

        // Assert
        Assert.Null(dictStr["key"]);
        Assert.Null(strValue);
    }
}
