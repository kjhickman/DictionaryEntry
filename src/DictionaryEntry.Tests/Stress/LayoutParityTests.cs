using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace DictionaryEntry.Tests.Stress;

/// <summary>
/// Entry operations write keys, hash codes, chain links, and values directly into <see cref="Dictionary{TKey, TValue}"/> storage.
/// Round-tripping every write through the BCL's own indexer across pathological key/value layouts detects any field offset,
/// alignment, or stride mismatch between the library's view of an entry slot and the runtime's.
/// </summary>
public class LayoutParityTests
{
    private static readonly int[] SampleIntArray = [1, 2];

    [Fact]
    public void AllPathologicalKeyAndValueTypeCombinations_RoundTripThroughBclDictionaryStorage()
    {
        ReadOnlySpan<TypeSample> keys =
        [
            TypeSample.Create<byte>(1, 2),
            TypeSample.Create<short>(10, 20),
            TypeSample.Create<int>(100, 200),
            TypeSample.Create<long>(1000L, 2000L),
            TypeSample.Create(new Int128(1, 2), new Int128(3, 4)),
            TypeSample.Create(Vector128.Create((byte)1), Vector128.Create((byte)2)),
            TypeSample.Create(Vector256.Create(1L), Vector256.Create(2L)),
            TypeSample.Create("alpha", "beta"),
            TypeSample.Create(new PackedKeyStruct(1, 100), new PackedKeyStruct(2, 200)),
            TypeSample.Create(new RefBearingKeyStruct(1, "k1"), new RefBearingKeyStruct(2, "k2")),
        ];

        ReadOnlySpan<TypeSample> values =
        [
            TypeSample.Create(false, true),
            TypeSample.Create(ValueTuple.Create(), ValueTuple.Create()),
            TypeSample.Create<byte>(7, 9),
            TypeSample.Create<int?>(null, 42),
            TypeSample.Create<Guid?>(null, Guid.NewGuid()),
            TypeSample.Create(12.34m, 56.78m),
            TypeSample.Create(new Int128(9, 10), new Int128(11, 12)),
            TypeSample.Create(Vector128.Create(1.5, 2.5), Vector128.Create(3.5, 4.5)),
            TypeSample.Create(Vector256.Create(10L, 20L, 30L, 40L), Vector256.Create(50L, 60L, 70L, 80L)),
            TypeSample.Create(new ExplicitUnionStruct { LongValue = 0x1122334455667788L }, new ExplicitUnionStruct { LongValue = 0x99AABBCCL }),
            TypeSample.Create(new PackedOddStruct(3, 0x123456789ABCDEF0L), new PackedOddStruct(4, 0x0FEDCBA987654321L)),
            TypeSample.Create(
                new MixedGcStruct(1, "v1", 99.5m, SampleIntArray, 7),
                new MixedGcStruct(2, null, -42.25m, "boxed", 8)),
        ];

        var verifyGeneric = typeof(LayoutParityTests)
            .GetMethod(nameof(VerifyPair), BindingFlags.Static | BindingFlags.NonPublic)!;

        foreach (var k in keys)
        {
            foreach (var v in values)
            {
                verifyGeneric.MakeGenericMethod(k.Type, v.Type)
                    .Invoke(null, [k.First, k.Second, v.First, v.Second]);
            }
        }
    }

    private static void VerifyPair<TKey, TValue>(object k1Obj, object k2Obj, object v1Obj, object v2Obj)
        where TKey : notnull
    {
        var k1 = (TKey)k1Obj;
        var k2 = (TKey)k2Obj;
        var v1 = (TValue)v1Obj!;
        var v2 = (TValue)v2Obj!;

        var dict = new Dictionary<TKey, TValue>(capacity: 4);

        // Vacant insert, OccupiedEntry.Insert, RefAction, and OccupiedEntry.Remove each write directly into entry storage;
        // the BCL indexer reads it back through the runtime's own layout.
        dict.Entry(k1).OrInsert(v1);
        dict.Entry(k2).OrInsert(v2);
        Assert.Equal(v1, dict[k1]);
        Assert.Equal(v2, dict[k2]);

        dict.Entry(k1).ToOccupied().Insert(v2);
        Assert.Equal(v2, dict[k1]);

        dict.Entry(k2).AndModify(v1, static (ref slot, replacement) => slot = replacement);
        Assert.Equal(v1, dict[k2]);

        var removed = dict.Entry(k1).ToOccupied().Remove();
        Assert.Equal(v2, removed);
        Assert.Single(dict);
        Assert.Equal(v1, dict[k2]);
    }

    private readonly record struct TypeSample(Type Type, object? First, object? Second)
    {
        public static TypeSample Create<T>(T first, T second) => new(typeof(T), first, second);
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private readonly record struct PackedKeyStruct(byte Tag, int Id);

    private readonly record struct RefBearingKeyStruct(int Id, string Label);

    [StructLayout(LayoutKind.Explicit, Size = 16)]
    private record struct ExplicitUnionStruct
    {
        [FieldOffset(0)] public long LongValue;
        [FieldOffset(0)] public int LowInt;
        [FieldOffset(8)] public double SecondHalf;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private readonly record struct PackedOddStruct(byte Prefix, long Payload);

    private readonly record struct MixedGcStruct(
        byte Tag,
        string? Name,
        decimal Amount,
        object? Extra,
        short Tail);
}
