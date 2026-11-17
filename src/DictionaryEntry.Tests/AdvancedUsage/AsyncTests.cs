namespace DictionaryEntry.Tests.AdvancedUsage;

public class AsyncTests
{
    [Fact]
    public async Task UnionSwitchStatement_WithAwaitInBothArms_MutatesAcrossAwait()
    {
        // Arrange
        var dict = new Dictionary<string, int>
        {
            ["existing"] = 10,
            ["removable"] = 99,
        };

        // Act
        await ApplyAsync(dict, "existing", removeIfOccupied: false);
        await ApplyAsync(dict, "removable", removeIfOccupied: true);
        await ApplyAsync(dict, "missing", removeIfOccupied: false);

        // Assert
        Assert.Equal(17, dict["existing"]);
        Assert.False(dict.ContainsKey("removable"));
        Assert.Equal(7, dict["missing"]);

        static async Task ApplyAsync(Dictionary<string, int> d, string key, bool removeIfOccupied)
        {
            switch (d.Entry(key))
            {
                case OccupiedEntry<string, int> occupied:
                    var delta = await GetValueAsync();
                    if (removeIfOccupied)
                    {
                        occupied.Remove();
                    }
                    else
                    {
                        occupied.Insert(occupied.Value + delta);
                    }
                    break;
                case VacantEntry<string, int> vacant:
                    vacant.Insert(await GetValueAsync());
                    break;
            }
        }
    }

    private static async Task<int> GetValueAsync()
    {
        await Task.Yield();
        return 7;
    }
}
