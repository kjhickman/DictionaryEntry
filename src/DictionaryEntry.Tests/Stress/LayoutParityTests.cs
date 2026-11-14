using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace DictionaryEntry.Tests.Stress;

public class LayoutParityTests
{
    private static readonly Type OpenDictionaryAccessType =
        typeof(Entry<,>).Assembly.GetType("DictionaryEntry.DictionaryAccess`2", throwOnError: true)!;

    private static readonly Type OpenRawEntryType =
        OpenDictionaryAccessType.GetNestedType("RawEntry", BindingFlags.NonPublic | BindingFlags.Public)!;

    private static readonly Type OpenBclEntryType =
        typeof(Dictionary<,>).GetNestedType("Entry", BindingFlags.NonPublic)!;

    [Fact]
    public void AllPathologicalKeyAndValueTypeCombinations_HaveIdenticalBclAndRawEntryLayouts()
    {
        ReadOnlySpan<KeySample> keys =
        [
            KeySample.Create<byte>(1, 2),
            KeySample.Create<short>(10, 20),
            KeySample.Create<int>(100, 200),
            KeySample.Create<long>(1000L, 2000L),
            KeySample.Create(new Int128(1, 2), new Int128(3, 4)),
            KeySample.Create(Vector128.Create((byte)1), Vector128.Create((byte)2)),
            KeySample.Create(Vector256.Create(1L), Vector256.Create(2L)),
            KeySample.Create("alpha", "beta"),
            KeySample.Create(new PackedKeyStruct(1, 100), new PackedKeyStruct(2, 200)),
            KeySample.Create(new RefBearingKeyStruct(1, "k1"), new RefBearingKeyStruct(2, "k2")),
        ];

        ReadOnlySpan<ValueSample> values =
        [
            ValueSample.Create(false, true),
            ValueSample.Create(ValueTuple.Create(), ValueTuple.Create()),
            ValueSample.Create<byte>(7, 9),
            ValueSample.Create<int?>(null, 42),
            ValueSample.Create<Guid?>(null, Guid.NewGuid()),
            ValueSample.Create(12.34m, 56.78m),
            ValueSample.Create(new Int128(9, 10), new Int128(11, 12)),
            ValueSample.Create(Vector128.Create(1.5, 2.5), Vector128.Create(3.5, 4.5)),
            ValueSample.Create(Vector256.Create(10L, 20L, 30L, 40L), Vector256.Create(50L, 60L, 70L, 80L)),
            ValueSample.Create(new ExplicitUnionStruct { LongValue = 0x1122334455667788L }, new ExplicitUnionStruct { LongValue = 0x99AABBCCL }),
            ValueSample.Create(new PackedOddStruct(3, 0x123456789ABCDEF0L), new PackedOddStruct(4, 0x0FEDCBA987654321L)),
            ValueSample.Create(
                new MixedGcStruct(1, "v1", 99.5m, new int[] { 1, 2 }, 7),
                new MixedGcStruct(2, null, -42.25m, "boxed", 8)),
        ];

        var verifyGeneric = typeof(LayoutParityTests)
            .GetMethod(nameof(VerifyPair), BindingFlags.Static | BindingFlags.NonPublic)!;

        var checkedPairs = 0;
        foreach (var k in keys)
        {
            foreach (var v in values)
            {
                verifyGeneric.MakeGenericMethod(k.Type, v.Type)
                    .Invoke(null, [k.First, k.Second, v.First, v.Second]);
                checkedPairs++;
            }
        }

        Assert.Equal(120, checkedPairs);
    }

    private static void VerifyPair<TKey, TValue>(object k1Obj, object k2Obj, object v1Obj, object v2Obj)
        where TKey : notnull
    {
        var bclEntryType = OpenBclEntryType.MakeGenericType(typeof(TKey), typeof(TValue));
        var rawEntryType = OpenRawEntryType.MakeGenericType(typeof(TKey), typeof(TValue));

        // 1. Verify exact array stride and field byte offsets between Dictionary.Entry and DictionaryAccess.RawEntry
        var bclLayout = MeasureStructLayout(bclEntryType);
        var rawLayout = MeasureStructLayout(rawEntryType);

        Assert.Equal(bclLayout.ElementStride, rawLayout.ElementStride);
        Assert.Equal(bclLayout.HashCodeOffset, rawLayout.HashCodeOffset);
        Assert.Equal(bclLayout.NextOffset, rawLayout.NextOffset);
        Assert.Equal(bclLayout.KeyOffset, rawLayout.KeyOffset);
        Assert.Equal(bclLayout.ValueOffset, rawLayout.ValueOffset);

        // 2. Verify RawDictionary._entries projection returns the exact same Array instance as Dictionary._entries
        var k1 = (TKey)k1Obj;
        var k2 = (TKey)k2Obj;
        var v1 = (TValue)v1Obj!;
        var v2 = (TValue)v2Obj!;

        var dict = new Dictionary<TKey, TValue>(capacity: 4);
        var bclEntriesField = typeof(Dictionary<TKey, TValue>).GetField("_entries", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var getEntriesMethod = OpenDictionaryAccessType
            .MakeGenericType(typeof(TKey), typeof(TValue))
            .GetMethod("GetEntries", BindingFlags.Static | BindingFlags.Public)!;

        var bclEntries = bclEntriesField.GetValue(dict);
        var projectedEntries = getEntriesMethod.Invoke(null, [dict]);
        Assert.NotNull(bclEntries);
        Assert.Same(bclEntries, projectedEntries);

        // 3. End-to-end functional round-trip across vacant insert, RefAction, OccupiedEntry.Insert, and Remove
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

    private static EntryLayout MeasureStructLayout(Type entryStructType)
    {
        var arr = Array.CreateInstance(entryStructType, 2);

        var strideMethod = new DynamicMethod("MeasureStride", typeof(nint), [typeof(Array)], restrictedSkipVisibility: true);
        var strideIl = strideMethod.GetILGenerator();
        strideIl.Emit(OpCodes.Ldarg_0);
        strideIl.Emit(OpCodes.Ldc_I4_1);
        strideIl.Emit(OpCodes.Ldelema, entryStructType);
        strideIl.Emit(OpCodes.Ldarg_0);
        strideIl.Emit(OpCodes.Ldc_I4_0);
        strideIl.Emit(OpCodes.Ldelema, entryStructType);
        strideIl.Emit(OpCodes.Sub);
        strideIl.Emit(OpCodes.Ret);
        var measureStride = strideMethod.CreateDelegate<Func<Array, nint>>();

        return new EntryLayout(
            ElementStride: measureStride(arr),
            HashCodeOffset: MeasureFieldOffset(entryStructType, "hashCode", arr),
            NextOffset: MeasureFieldOffset(entryStructType, "next", arr),
            KeyOffset: MeasureFieldOffset(entryStructType, "key", arr),
            ValueOffset: MeasureFieldOffset(entryStructType, "value", arr));
    }

    private static nint MeasureFieldOffset(Type entryStructType, string fieldName, Array twoElementArray)
    {
        var field = entryStructType.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
        var dm = new DynamicMethod($"Offset_{fieldName}", typeof(nint), [typeof(Array)], restrictedSkipVisibility: true);
        var il = dm.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Ldelema, entryStructType);
        il.Emit(OpCodes.Ldflda, field);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4_0);
        il.Emit(OpCodes.Ldelema, entryStructType);
        il.Emit(OpCodes.Sub);
        il.Emit(OpCodes.Ret);
        return dm.CreateDelegate<Func<Array, nint>>()(twoElementArray);
    }

    private readonly record struct EntryLayout(
        nint ElementStride,
        nint HashCodeOffset,
        nint NextOffset,
        nint KeyOffset,
        nint ValueOffset);

    private readonly record struct KeySample(Type Type, object First, object Second)
    {
        public static KeySample Create<T>(T first, T second) where T : notnull => new(typeof(T), first, second);
    }

    private readonly record struct ValueSample(Type Type, object? First, object? Second)
    {
        public static ValueSample Create<T>(T first, T second) => new(typeof(T), first, second);
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private readonly record struct PackedKeyStruct(byte Tag, int Id);

    private readonly record struct RefBearingKeyStruct(int Id, string Label);

    [StructLayout(LayoutKind.Explicit, Size = 16)]
    private struct ExplicitUnionStruct : IEquatable<ExplicitUnionStruct>
    {
        [FieldOffset(0)]
        public long LongValue;

        [FieldOffset(0)]
        public int LowInt;

        [FieldOffset(8)]
        public double SecondHalf;

        public readonly bool Equals(ExplicitUnionStruct other) =>
            LongValue == other.LongValue && BitConverter.DoubleToInt64Bits(SecondHalf) == BitConverter.DoubleToInt64Bits(other.SecondHalf);

        public override readonly bool Equals(object? obj) => obj is ExplicitUnionStruct other && Equals(other);
        public override readonly int GetHashCode() => HashCode.Combine(LongValue, SecondHalf);
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
