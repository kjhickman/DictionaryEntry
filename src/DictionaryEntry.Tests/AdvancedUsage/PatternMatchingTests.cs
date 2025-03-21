namespace DictionaryEntry.Tests.AdvancedUsage;

public class PatternMatchingTests
{
    [Fact]
    public void Match_WithOccupiedEntry_CallsOccupiedAction()
    {
        // Arrange
        var dict = new Dictionary<string, int> { ["key"] = 42 };
        var occupiedCalled = false;
        var vacantCalled = false;

        // Act
        dict.Entry("key").Match(
            occupied => { occupiedCalled = true; },
            vacant => { vacantCalled = true; });

        // Assert
        Assert.True(occupiedCalled);
        Assert.False(vacantCalled);
    }

    [Fact]
    public void Match_WithVacantEntry_CallsVacantAction()
    {
        // Arrange
        var dict = new Dictionary<string, int>();
        var occupiedCalled = false;
        var vacantCalled = false;

        // Act
        dict.Entry("key").Match(
            occupied => { occupiedCalled = true; },
            vacant => { vacantCalled = true; });

        // Assert
        Assert.False(occupiedCalled);
        Assert.True(vacantCalled);
    }

    [Fact]
    public void Match_WithFuncReturnsCorrectValue()
    {
        // Arrange
        var dict = new Dictionary<string, int> { ["key"] = 42 };

        // Act
        var result = dict.Entry("key").Match(
            occupied => occupied.Value() * 2,
            vacant => 0);

        // Assert
        Assert.Equal(84, result);

        // Test with vacant entry
        dict.Clear();
        result = dict.Entry("key").Match(
            occupied => occupied.Value() * 2,
            vacant => 99);

        // Assert
        Assert.Equal(99, result);
    }
}
