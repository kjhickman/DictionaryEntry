namespace DictionaryEntry.Tests.TestHelpers;

internal static class AllocationAssert
{
    public static TResult ZeroAllocations<TState, TResult>(
        TState state,
        Func<TState, TResult> action,
        int warmupIterations = 200)
        where TState : allows ref struct
    {
        for (var i = 0; i < warmupIterations; i++)
        {
            _ = action(state);
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        var result = action(state);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
#if !DEBUG
        Assert.Equal(0, allocated);
#else
        _ = allocated;
#endif
        return result;
    }

    public static TResult ZeroAllocations<TArg1, TArg2, TResult>(
        TArg1 arg1,
        TArg2 arg2,
        Func<TArg1, TArg2, TResult> action,
        int warmupIterations = 200)
        where TArg2 : allows ref struct
    {
        for (var i = 0; i < warmupIterations; i++)
        {
            _ = action(arg1, arg2);
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        var result = action(arg1, arg2);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
#if !DEBUG
        Assert.Equal(0, allocated);
#else
        _ = allocated;
#endif
        return result;
    }
}
