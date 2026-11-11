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

    [Fact]
    public async Task Match_WithAsyncBranches_UpdatesValueForOccupiedEntry()
    {
        // Arrange
        var dict = new Dictionary<string, int> { ["key"] = 10 };

        // Act
        var result = await dict.Entry("key").Match(
            async occupied => occupied.Insert(occupied.Value + await GetValueAsync()),
            vacant => Task.FromResult(vacant.Insert(0)));

        // Assert
        Assert.Equal(17, result);
        Assert.Equal(17, dict["key"]);
    }

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

    [Fact]
    public async Task UnionIsPattern_WithAwaitInBody_InsertsAndUpdatesAcrossAwait()
    {
        // Arrange
        var dict = new Dictionary<string, int> { ["existing"] = 35 };

        // Act
        if (dict.Entry("missing") is VacantEntry<string, int> vacant)
        {
            vacant.Insert(await GetValueAsync());
        }

        if (dict.Entry("existing") is OccupiedEntry<string, int> occupied)
        {
            var delta = await GetValueAsync();
            occupied.Insert(occupied.Value + delta);
        }

        // Assert
        Assert.Equal(7, dict["missing"]);
        Assert.Equal(42, dict["existing"]);
    }

    private static async Task<int> GetValueAsync()
    {
        await Task.Yield();
        return 7;
    }
}
