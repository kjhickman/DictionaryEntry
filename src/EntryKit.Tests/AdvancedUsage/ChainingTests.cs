namespace EntryKit.Tests.AdvancedUsage;

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
}
