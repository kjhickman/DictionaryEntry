namespace DictionaryEntry.Tests.TestHelpers;

internal static class DictionaryAssert
{
    public static void Equal<TKey, TValue>(Dictionary<TKey, TValue> expected, Dictionary<TKey, TValue> actual)
        where TKey : notnull
    {
        Assert.Equal(expected.Count, actual.Count);

        foreach (var (key, expectedValue) in expected)
        {
            Assert.True(actual.TryGetValue(key, out var actualValue));
            Assert.Equal(expectedValue, actualValue);
        }

        var enumeratedCount = 0;
        foreach (var (key, actualValue) in actual)
        {
            Assert.True(expected.TryGetValue(key, out var expectedValue));
            Assert.Equal(expectedValue, actualValue);
            enumeratedCount++;
        }

        Assert.Equal(expected.Count, enumeratedCount);
    }
}

internal record struct PlayerStats(int Hits, int Damage);

internal readonly record struct CollidingKey(int Id)
{
    public override int GetHashCode() => 0;
}

internal sealed class LowEntropySpanStringComparer(int mask)
    : IEqualityComparer<string>, IAlternateEqualityComparer<ReadOnlySpan<char>, string>
{
    public static readonly LowEntropySpanStringComparer Colliding = new(0);
    public static readonly LowEntropySpanStringComparer LowEntropy = new(3);

    public bool Equals(string? x, string? y) => string.Equals(x, y, StringComparison.Ordinal);
    public int GetHashCode(string obj) => obj.Length & mask;
    public bool Equals(ReadOnlySpan<char> alternate, string other) => alternate.SequenceEqual(other.AsSpan());
    public int GetHashCode(ReadOnlySpan<char> alternate) => alternate.Length & mask;
    public string Create(ReadOnlySpan<char> alternate) => alternate.ToString();
}
