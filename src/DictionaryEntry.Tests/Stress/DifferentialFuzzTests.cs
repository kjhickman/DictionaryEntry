using System.Reflection;

namespace DictionaryEntry.Tests.Stress;

public class DifferentialFuzzTests
{
    private static int GetSeedCount(int defaultSeeds) =>
        int.TryParse(Environment.GetEnvironmentVariable("DICTIONARYENTRY_FUZZ_SEEDS"), out var parsed) && parsed > 0
            ? parsed
            : defaultSeeds;

    [Fact]
    public void RandomizedStringAndSpanStateMachine_MatchesOracleAndPreservesInternalInvariants()
    {
        var seedCount = GetSeedCount(defaultSeeds: 400);
        const int stepsPerSeed = 250;

        for (var seed = 1; seed <= seedCount; seed++)
        {
            RunStringFuzzSeed(seed, stepsPerSeed);
        }
    }

    [Fact]
    public void RandomizedValueTypeAndMixedStructStateMachine_MatchesOracleAndPreservesInternalInvariants()
    {
        var seedCount = GetSeedCount(defaultSeeds: 200);
        const int stepsPerSeed = 250;

        for (var seed = 1; seed <= seedCount; seed++)
        {
            RunValueTypeStructFuzzSeed(seed, stepsPerSeed);
        }
    }

    private static void RunStringFuzzSeed(int seed, int steps)
    {
        var rng = new Random(seed * 7919 + 17);
        var initialCapacity = (seed % 5) switch
        {
            0 => 0,
            1 => 1,
            2 => 3,
            3 => 7,
            _ => 16,
        };

        var comparerMode = seed % 4;
        IEqualityComparer<string>? comparer = comparerMode switch
        {
            0 => null, // default EqualityComparer<string>.Default (NonRandomizedStringEqualityComparer)
            1 => StringComparer.Ordinal,
            2 => StringComparer.OrdinalIgnoreCase,
            _ => LowEntropySpanStringComparer.Instance, // heavy bucket collisions + AlternateLookup
        };

        var sut = initialCapacity == 0 && comparer is null
            ? new Dictionary<string, int>()
            : new Dictionary<string, int>(initialCapacity, comparer);
        var oracle = initialCapacity == 0 && comparer is null
            ? new Dictionary<string, int>()
            : new Dictionary<string, int>(initialCapacity, comparer);

        var lookup = sut.GetAlternateLookup<ReadOnlySpan<char>>();
        var keyUniverseSize = comparerMode == 3 ? 24 : 18;

        var savedOccupied = new List<OccupiedEntry<string, int>>(capacity: 16);
        var savedVacant = new List<VacantEntry<string, int>>(capacity: 16);

        for (var step = 0; step < steps; step++)
        {
            var keyId = rng.Next(keyUniverseSize);
            // Vary casing on OrdinalIgnoreCase seeds to test key preservation vs lookup key casing
            var key = comparerMode == 2 && (rng.Next(2) == 0)
                ? $"KEY_{keyId}"
                : $"key_{keyId}";
            var paddedSpanSource = $"prefix:{key}:suffix";
            var val = rng.Next(-500, 500);
            var delta = rng.Next(-25, 26);

            var op = rng.Next(22);
            try
            {
                switch (op)
                {
                    case 0: // OrInsert
                    {
                        ref var sutSlot = ref sut.Entry(key).OrInsert(val);
                        if (!oracle.TryGetValue(key, out var expected))
                        {
                            expected = val;
                            oracle[key] = expected;
                        }
                        Assert.Equal(expected, sutSlot);
                        sutSlot += delta;
                        oracle[key] = expected + delta;
                        break;
                    }

                    case 1: // OrDefault + ref mutation
                    {
                        ref var sutSlot = ref sut.Entry(key).OrDefault();
                        if (!oracle.TryGetValue(key, out var expected))
                        {
                            expected = 0;
                            oracle[key] = 0;
                        }
                        Assert.Equal(expected, sutSlot);
                        sutSlot += delta;
                        oracle[key] = expected + delta;
                        break;
                    }

                    case 2: // OrInsertWith (stateful)
                    {
                        var actual = sut.Entry(key).OrInsertWith(val, static s => s + 1);
                        if (!oracle.TryGetValue(key, out var expected))
                        {
                            expected = val + 1;
                            oracle[key] = expected;
                        }
                        Assert.Equal(expected, actual);
                        break;
                    }

                    case 3: // OrInsertWithKey (stateful)
                    {
                        var actual = sut.Entry(key).OrInsertWithKey(val, static (k, s) => k.Length + s);
                        if (!oracle.TryGetValue(key, out var expected))
                        {
                            expected = key.Length + val;
                            oracle[key] = expected;
                        }
                        Assert.Equal(expected, actual);
                        break;
                    }

                    case 4: // AndModify(Func) + OrInsert
                    {
                        var actual = sut.Entry(key).AndModify(v => v + delta).OrInsert(val);
                        int expected;
                        if (oracle.TryGetValue(key, out var cur))
                        {
                            expected = cur + delta;
                            oracle[key] = expected;
                        }
                        else
                        {
                            expected = val;
                            oracle[key] = expected;
                        }
                        Assert.Equal(expected, actual);
                        break;
                    }

                    case 5: // AndModify(state, Func) + OrInsert
                    {
                        var actual = sut.Entry(key).AndModify(delta, static (v, d) => v + d).OrInsert(val);
                        int expected;
                        if (oracle.TryGetValue(key, out var cur))
                        {
                            expected = cur + delta;
                            oracle[key] = expected;
                        }
                        else
                        {
                            expected = val;
                            oracle[key] = expected;
                        }
                        Assert.Equal(expected, actual);
                        break;
                    }

                    case 6: // AndModify(state, RefAction) only (no OrInsert)
                    {
                        sut.Entry(key).AndModify(delta, static (ref v, d) => v += d);
                        if (oracle.TryGetValue(key, out var cur))
                        {
                            oracle[key] = cur + delta;
                        }
                        break;
                    }

                    case 7: // InsertEntry + save OccupiedEntry snapshot
                    {
                        var occ = sut.Entry(key).InsertEntry(val);
                        oracle[key] = val;
                        Assert.Equal(val, occ.Value);
                        if (savedOccupied.Count < 16)
                        {
                            savedOccupied.Add(occ);
                        }
                        break;
                    }

                    case 8: // C# 15 union switch (Insert or Remove)
                    {
                        switch (sut.Entry(key))
                        {
                            case OccupiedEntry<string, int> occ:
                                if ((step & 1) == 0)
                                {
                                    occ.Insert(occ.Value + delta);
                                    oracle[key] += delta;
                                }
                                else
                                {
                                    var removed = occ.Remove();
                                    Assert.True(oracle.Remove(key, out var expectedRemoved));
                                    Assert.Equal(expectedRemoved, removed);
                                }
                                break;

                            case VacantEntry<string, int> vac:
                                vac.Insert(val);
                                oracle[key] = val;
                                break;
                        }
                        break;
                    }

                    case 9: // Capture OccupiedEntry / VacantEntry for delayed stale execution
                    {
                        if (sut.Entry(key).TryGetOccupied(out var occ))
                        {
                            if (savedOccupied.Count < 16) savedOccupied.Add(occ);
                        }
                        else if (sut.Entry(key).TryGetVacant(out var vac))
                        {
                            if (savedVacant.Count < 16) savedVacant.Add(vac);
                        }
                        break;
                    }

                    case 10: // Fire a previously saved (potentially stale!) OccupiedEntry.Insert
                    {
                        if (savedOccupied.Count > 0)
                        {
                            var idx = rng.Next(savedOccupied.Count);
                            var occ = savedOccupied[idx];
                            if ((step & 1) == 0) savedOccupied.RemoveAt(idx);

                            occ.Insert(val);
                            oracle[occ.Key] = val;
                        }
                        break;
                    }

                    case 11: // Fire a previously saved (potentially stale!) OccupiedEntry.Remove / RemoveEntry
                    {
                        if (savedOccupied.Count > 0)
                        {
                            var idx = rng.Next(savedOccupied.Count);
                            var occ = savedOccupied[idx];
                            savedOccupied.RemoveAt(idx);

                            if (oracle.Remove(occ.Key, out var expectedVal))
                            {
                                var actualVal = (step & 1) == 0 ? occ.Remove() : occ.RemoveEntry().Item2;
                                Assert.Equal(expectedVal, actualVal);
                            }
                            else
                            {
                                Assert.Throws<KeyNotFoundException>(() => _ = occ.Remove());
                            }
                        }
                        break;
                    }

                    case 12: // Fire a previously saved (potentially stale!) VacantEntry.Insert / InsertEntry
                    {
                        if (savedVacant.Count > 0)
                        {
                            var idx = rng.Next(savedVacant.Count);
                            var vac = savedVacant[idx];
                            savedVacant.RemoveAt(idx);

                            if ((step & 1) == 0)
                            {
                                vac.Insert(val);
                            }
                            else
                            {
                                var occ = vac.InsertEntry(val);
                                if (savedOccupied.Count < 16) savedOccupied.Add(occ);
                            }
                            oracle[vac.Key] = val;
                        }
                        break;
                    }

                    case 13: // Implicit union conversion from potentially stale OccupiedEntry / VacantEntry
                    {
                        if (savedOccupied.Count > 0)
                        {
                            Entry<string, int> live = savedOccupied[^1];
                            Assert.Equal(oracle.ContainsKey(savedOccupied[^1].Key), live.IsOccupied);
                        }
                        if (savedVacant.Count > 0)
                        {
                            Entry<string, int> live = savedVacant[^1];
                            Assert.Equal(oracle.ContainsKey(savedVacant[^1].Key), live.IsOccupied);
                        }
                        break;
                    }

                    case 14: // Direct ReadOnlySpan<char> extension: OrInsert / AndModify
                    {
                        var span = paddedSpanSource.AsSpan(7, key.Length);
                        var actual = sut.Entry(span).AndModify(delta, static (ref v, d) => v += d).OrInsert(val);
                        int expected;
                        if (oracle.TryGetValue(key, out var cur))
                        {
                            expected = cur + delta;
                            oracle[key] = expected;
                        }
                        else
                        {
                            expected = val;
                            oracle[key] = expected;
                        }
                        Assert.Equal(expected, actual);
                        break;
                    }

                    case 15: // Cached AlternateLookup<ReadOnlySpan<char>>: Occupied.Insert / Vacant.InsertEntry
                    {
                        var span = paddedSpanSource.AsSpan(7, key.Length);
                        if (lookup.Entry(span).TryGetOccupied(out var altOcc))
                        {
                            altOcc.Insert(altOcc.Value + delta);
                            oracle[key] += delta;
                        }
                        else if (lookup.Entry(span).TryGetVacant(out var altVac))
                        {
                            var newOcc = altVac.InsertEntry(val);
                            newOcc.Insert(val + 1);
                            oracle[key] = val + 1;
                        }
                        break;
                    }

                    case 16: // AlternateLookup RemoveEntry
                    {
                        var span = paddedSpanSource.AsSpan(7, key.Length);
                        if (lookup.Entry(span).TryGetOccupied(out var altOcc))
                        {
                            var (_, removedVal) = altOcc.RemoveEntry();
                            Assert.True(oracle.Remove(key, out var expectedVal));
                            Assert.Equal(expectedVal, removedVal);
                        }
                        else
                        {
                            Assert.False(oracle.ContainsKey(key));
                        }
                        break;
                    }

                    case 17: // Match stateful
                    {
                        var res = sut.Entry(key).Match(
                            (delta, val),
                            static (occ, st) => occ.Insert(occ.Value + st.delta),
                            static (vac, st) => vac.Insert(st.val));
                        var expected = oracle.TryGetValue(key, out var cur)
                            ? (oracle[key] = cur + delta)
                            : (oracle[key] = val);
                        Assert.Equal(expected, res);
                        break;
                    }

                    case 18: // External Dictionary.Remove
                    {
                        Assert.Equal(oracle.Remove(key), sut.Remove(key));
                        break;
                    }

                    case 19: // External Dictionary.Clear (occasional)
                    {
                        if (rng.Next(6) == 0)
                        {
                            sut.Clear();
                            oracle.Clear();
                        }
                        break;
                    }

                    case 20: // External EnsureCapacity
                    {
                        var cap = sut.Count + rng.Next(1, 32);
                        sut.EnsureCapacity(cap);
                        oracle.EnsureCapacity(cap);
                        break;
                    }

                    case 21: // External TrimExcess
                    {
                        sut.TrimExcess();
                        oracle.TrimExcess();
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Xunit.Sdk.XunitException(
                    $"String fuzz failed at seed={seed}, step={step}, op={op}, key='{key}': {ex}");
            }

            if ((step & 31) == 31 || step == steps - 1)
            {
                AssertParityAndInternalInvariants(oracle, sut, lookup, seed, step);
            }
        }
    }

    private static void RunValueTypeStructFuzzSeed(int seed, int steps)
    {
        var rng = new Random(seed * 104729 + 31);
        var sut = new Dictionary<int, FuzzRecord>(capacity: seed % 5);
        var oracle = new Dictionary<int, FuzzRecord>(capacity: seed % 5);
        var savedOccupied = new List<OccupiedEntry<int, FuzzRecord>>(capacity: 8);

        for (var step = 0; step < steps; step++)
        {
            var key = rng.Next(0, 20);
            var tag = (step % 4 == 0) ? null : $"t_{step}";
            var fresh = new FuzzRecord(rng.Next(1000), tag, step * 17L);

            switch (rng.Next(8))
            {
                case 0:
                    sut.Entry(key)
                        .AndModify(step, static (ref r, s) =>
                        {
                            r.Counter += 1;
                            r.Checksum ^= s;
                        })
                        .OrInsert(fresh);

                    if (oracle.TryGetValue(key, out var cur))
                    {
                        cur.Counter += 1;
                        cur.Checksum ^= step;
                        oracle[key] = cur;
                    }
                    else
                    {
                        oracle[key] = fresh;
                    }
                    break;

                case 1:
                    if (sut.Entry(key) is { IsOccupied: true } occupiedEntry)
                    {
                        ref var r = ref occupiedEntry.ValueRef;
                        r.Counter += 5;
                        r.Tag = tag;
                        var o = oracle[key];
                        o.Counter += 5;
                        o.Tag = tag;
                        oracle[key] = o;
                    }
                    break;

                case 2:
                    var occ = sut.Entry(key).InsertEntry(fresh);
                    oracle[key] = fresh;
                    if (savedOccupied.Count < 8) savedOccupied.Add(occ);
                    break;

                case 3:
                    if (savedOccupied.Count > 0)
                    {
                        var idx = rng.Next(savedOccupied.Count);
                        var sOcc = savedOccupied[idx];
                        savedOccupied.RemoveAt(idx);
                        sOcc.Insert(fresh);
                        oracle[sOcc.Key] = fresh;
                    }
                    break;

                case 4:
                    ref var defRef = ref sut.Entry(key).OrDefault();
                    if (!oracle.TryGetValue(key, out var defCur))
                    {
                        defCur = default;
                    }
                    defRef.Counter += 2;
                    defCur.Counter += 2;
                    oracle[key] = defCur;
                    break;

                case 5:
                    Assert.Equal(oracle.Remove(key), sut.Remove(key));
                    break;

                case 6:
                    if (rng.Next(8) == 0)
                    {
                        sut.Clear();
                        oracle.Clear();
                    }
                    break;

                case 7:
                    sut.TrimExcess();
                    oracle.TrimExcess();
                    break;
            }

            if ((step & 31) == 31 || step == steps - 1)
            {
                AssertValueTypeParityAndInvariants(oracle, sut, seed, step);
            }
        }
    }

    private static void AssertParityAndInternalInvariants(
        Dictionary<string, int> oracle,
        Dictionary<string, int> sut,
        Dictionary<string, int>.AlternateLookup<ReadOnlySpan<char>> lookup,
        int seed,
        int step)
    {
        if (oracle.Count != sut.Count)
        {
            throw new Xunit.Sdk.XunitException($"Count mismatch at seed={seed}, step={step}: expected {oracle.Count}, actual {sut.Count}");
        }

        foreach (var (k, expectedVal) in oracle)
        {
            if (!sut.TryGetValue(k, out var actualVal) || actualVal != expectedVal)
            {
                throw new Xunit.Sdk.XunitException($"Value mismatch for '{k}' at seed={seed}, step={step}");
            }

            if (!lookup.TryGetValue(k.AsSpan(), out var spanVal) || spanVal != expectedVal)
            {
                throw new Xunit.Sdk.XunitException($"Span lookup mismatch for '{k}' at seed={seed}, step={step}");
            }
        }

        var enumerated = 0;
        foreach (var (k, v) in sut)
        {
            if (!oracle.TryGetValue(k, out var expectedVal) || expectedVal != v)
            {
                throw new Xunit.Sdk.XunitException($"Enumerator mismatch for '{k}' at seed={seed}, step={step}");
            }
            enumerated++;
        }

        Assert.Equal(oracle.Count, enumerated);
        DictionaryStructureVerifier<string, int>.Verify(sut, seed, step);
    }

    private static void AssertValueTypeParityAndInvariants(
        Dictionary<int, FuzzRecord> oracle,
        Dictionary<int, FuzzRecord> sut,
        int seed,
        int step)
    {
        Assert.Equal(oracle.Count, sut.Count);
        foreach (var (k, expectedVal) in oracle)
        {
            Assert.True(sut.TryGetValue(k, out var actualVal));
            Assert.Equal(expectedVal, actualVal);
        }

        DictionaryStructureVerifier<int, FuzzRecord>.Verify(sut, seed, step);
    }

    private record struct FuzzRecord(int Counter, string? Tag, long Checksum);

    private sealed class LowEntropySpanStringComparer
        : IEqualityComparer<string>, IAlternateEqualityComparer<ReadOnlySpan<char>, string>
    {
        public static readonly LowEntropySpanStringComparer Instance = new();

        public bool Equals(string? x, string? y) => string.Equals(x, y, StringComparison.Ordinal);
        public int GetHashCode(string obj) => obj.Length & 3;
        public bool Equals(ReadOnlySpan<char> alternate, string other) => alternate.SequenceEqual(other.AsSpan());
        public int GetHashCode(ReadOnlySpan<char> alternate) => alternate.Length & 3;
        public string Create(ReadOnlySpan<char> alternate) => alternate.ToString();
    }

    private static class DictionaryStructureVerifier<TKey, TValue> where TKey : notnull
    {
        private static readonly FieldInfo BucketsField =
            typeof(Dictionary<TKey, TValue>).GetField("_buckets", BindingFlags.Instance | BindingFlags.NonPublic)!;
        private static readonly FieldInfo EntriesField =
            typeof(Dictionary<TKey, TValue>).GetField("_entries", BindingFlags.Instance | BindingFlags.NonPublic)!;
        private static readonly FieldInfo CountField =
            typeof(Dictionary<TKey, TValue>).GetField("_count", BindingFlags.Instance | BindingFlags.NonPublic)!;
        private static readonly FieldInfo FreeListField =
            typeof(Dictionary<TKey, TValue>).GetField("_freeList", BindingFlags.Instance | BindingFlags.NonPublic)!;
        private static readonly FieldInfo FreeCountField =
            typeof(Dictionary<TKey, TValue>).GetField("_freeCount", BindingFlags.Instance | BindingFlags.NonPublic)!;

        private static readonly Type EntryType =
            typeof(Dictionary<TKey, TValue>).GetNestedType("Entry", BindingFlags.NonPublic)!
                .MakeGenericType(typeof(TKey), typeof(TValue));
        private static readonly FieldInfo NextField = EntryType.GetField("next", BindingFlags.Instance | BindingFlags.Public)!;

        public static void Verify(Dictionary<TKey, TValue> dict, int seed, int step)
        {
            var buckets = (int[]?)BucketsField.GetValue(dict);
            var entries = (Array?)EntriesField.GetValue(dict);
            var count = (int)CountField.GetValue(dict)!;
            var freeList = (int)FreeListField.GetValue(dict)!;
            var freeCount = (int)FreeCountField.GetValue(dict)!;

            if (buckets is null)
            {
                Assert.Null(entries);
                Assert.Equal(0, count);
                Assert.Equal(0, freeCount);
                Assert.Empty(dict);
                return;
            }

            Assert.NotNull(entries);
            Assert.Equal(buckets.Length, entries!.Length);
            Assert.InRange(count, 0, entries.Length);
            Assert.InRange(freeCount, 0, count);
            Assert.Equal(dict.Count, count - freeCount);

            var freeVisited = new HashSet<int>();
            var f = freeList;
            while (f >= 0)
            {
                if (f >= count || !freeVisited.Add(f))
                {
                    throw new Xunit.Sdk.XunitException($"Corrupt _freeList at seed={seed}, step={step}, index={f}");
                }

                var entryBoxed = entries.GetValue(f)!;
                var nextRaw = (int)NextField.GetValue(entryBoxed)!;
                Assert.True(nextRaw <= -2, $"Expected free entry.next <= -2, got {nextRaw} at seed={seed}, step={step}");
                f = -3 - nextRaw;
            }

            if (freeVisited.Count != freeCount)
            {
                throw new Xunit.Sdk.XunitException(
                    $"_freeList count mismatch at seed={seed}, step={step}: walked {freeVisited.Count}, _freeCount={freeCount}");
            }

            var liveVisited = new HashSet<int>();
            for (var b = 0; b < buckets.Length; b++)
            {
                var i = buckets[b] - 1;
                while (i >= 0)
                {
                    if (i >= count || freeVisited.Contains(i) || !liveVisited.Add(i))
                    {
                        throw new Xunit.Sdk.XunitException($"Corrupt bucket chain at seed={seed}, step={step}, bucket={b}, index={i}");
                    }

                    var entryBoxed = entries.GetValue(i)!;
                    var next = (int)NextField.GetValue(entryBoxed)!;
                    Assert.True(next >= -1, $"Expected live entry.next >= -1, got {next} at seed={seed}, step={step}");
                    i = next;
                }
            }

            if (liveVisited.Count != dict.Count)
            {
                throw new Xunit.Sdk.XunitException(
                    $"Live bucket chain count mismatch at seed={seed}, step={step}: walked {liveVisited.Count}, Count={dict.Count}");
            }
        }
    }
}
