namespace DictionaryEntry.Tests.AdvancedUsage;

public class ChainingTests
{
    [Fact]
    public void AndModifyOrInsert_UpdatesValue()
    {
        var scores = new Dictionary<string, int>();
        scores.Entry("Player1").AndModify(score => score + 5).OrInsert(10);

        Assert.Equal(10, scores["Player1"]);

        scores.Entry("Player1").AndModify(score => score + 5).OrInsert(10);
        Assert.Equal(15, scores["Player1"]);
    }

    [Fact]
    public void AndModifyOrInsertWith_UpdatesValue()
    {
        var scores = new Dictionary<string, int>();
        scores.Entry("Player1").AndModify(score => score + 5).OrInsertWith(() => 10);

        Assert.Equal(10, scores["Player1"]);

        scores.Entry("Player1").AndModify(score => score + 5).OrInsertWith(() => 10);
        Assert.Equal(15, scores["Player1"]);
    }

    [Fact]
    public void AndModifyOrInsertWithKey_UpdatesOrInitializesFromKey()
    {
        var scores = new Dictionary<string, int>();

        scores.Entry("Player1").AndModify(score => score + 5).OrInsertWithKey(key => key.Length);
        Assert.Equal(7, scores["Player1"]);

        scores.Entry("Player1").AndModify(score => score + 5).OrInsertWithKey(key => key.Length);
        Assert.Equal(12, scores["Player1"]);
    }

    [Fact]
    public void AndModifyOrDefault_UpdatesOrInitializesDefaultAndAllowsRefMutation()
    {
        var scores = new Dictionary<string, int>();

        ref var initial = ref scores.Entry("Player1").AndModify(score => score + 5).OrDefault();
        Assert.Equal(0, initial);
        initial = 20;
        Assert.Equal(20, scores["Player1"]);

        ref var updated = ref scores.Entry("Player1").AndModify(score => score + 5).OrDefault();
        Assert.Equal(25, updated);
        updated += 5;
        Assert.Equal(30, scores["Player1"]);
    }

    [Fact]
    public void AndModifyInsertEntry_OverwritesAndReturnsOccupiedEntry()
    {
        var scores = new Dictionary<string, int> { ["Player1"] = 10 };

        var occupied = scores.Entry("Player1").AndModify(score => score + 5).InsertEntry(100);
        Assert.Equal(100, occupied.Value);
        Assert.Equal(100, scores["Player1"]);
    }

    [Fact]
    public void AndModifyMatch_PassesUpdatedValueToOccupiedBranch()
    {
        var scores = new Dictionary<string, int> { ["Player1"] = 10 };

        var existingResult = scores.Entry("Player1")
            .AndModify(score => score + 5)
            .Match(occupied => occupied.Value, _ => -1);

        var missingResult = scores.Entry("Player2")
            .AndModify(score => score + 5)
            .Match(occupied => occupied.Value, vacant => vacant.Insert(1));

        Assert.Equal(15, existingResult);
        Assert.Equal(1, missingResult);
        Assert.Equal(1, scores["Player2"]);
    }

    [Fact]
    public void AndModifyTryGetOccupied_ReturnsUpdatedValue()
    {
        var scores = new Dictionary<string, int> { ["Player1"] = 10 };

        var found = scores.Entry("Player1").AndModify(score => score * 3).TryGetOccupied(out var occupied);

        Assert.True(found);
        Assert.Equal(30, occupied.Value);
    }
}
