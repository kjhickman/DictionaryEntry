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
            occupied => Task.FromResult(occupied.Value()),
            async vacant => vacant.Insert(await GetValueAsync()));

        // Assert
        Assert.Equal(7, result);
        Assert.Equal(7, dict["key"]);
    }

    private static async Task<int> GetValueAsync()
    {
        await Task.Yield();
        return 7;
    }
}
