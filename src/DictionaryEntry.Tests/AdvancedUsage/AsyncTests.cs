namespace DictionaryEntry.Tests.AdvancedUsage;

public class AsyncTests
{
    [Fact]
    public async Task Match_WithAsyncBranches_InsertsValueForVacantEntry()
    {
        // Arrange
        var dict = new Dictionary<string, int>();

        // Act
        var result = await dict.Entry("key").Match(
            occupied => Task.FromResult(occupied.Value),
            async vacant => vacant.Insert(await GetValueAsync()));

        // Assert
        Assert.Equal(7, result);
        Assert.Equal(7, dict["key"]);
    }

    [Fact]
    public async Task UnionSwitchExpression_WithAsyncBranch_InsertsValueForVacantEntry()
    {
        // Arrange
        var dict = new Dictionary<string, int>();

        // Act
        var result = dict.Entry("key") switch
        {
            OccupiedEntry<string, int> occupied => occupied.Value,
            VacantEntry<string, int> vacant => vacant.Insert(await GetValueAsync()),
        };

        // Assert
        Assert.Equal(7, result);
        Assert.Equal(7, dict["key"]);
    }

    [Fact]
    public async Task UnionSwitchExpression_WithAsyncBranch_ReturnsOccupiedValueWithoutAwait()
    {
        // Arrange
        var dict = new Dictionary<string, int> { ["key"] = 42 };

        // Act
        var result = dict.Entry("key") switch
        {
            OccupiedEntry<string, int> occupied => occupied.Value,
            VacantEntry<string, int> vacant => vacant.Insert(await GetValueAsync()),
        };

        // Assert
        Assert.Equal(42, result);
        Assert.Equal(42, dict["key"]);
    }

    private static async Task<int> GetValueAsync()
    {
        await Task.Yield();
        return 7;
    }
}
