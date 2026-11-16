#if NATIVEAOT
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace DictionaryEntry.Tests.TestHelpers;

[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "DictionaryEntry.Tests is rooted as a TrimmerRootAssembly under PublishAot.")]
[UnconditionalSuppressMessage("Trimming", "IL2062", Justification = "DictionaryEntry.Tests is rooted as a TrimmerRootAssembly under PublishAot.")]
[UnconditionalSuppressMessage("Trimming", "IL2065", Justification = "DictionaryEntry.Tests is rooted as a TrimmerRootAssembly under PublishAot.")]
[UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "DictionaryEntry.Tests is rooted as a TrimmerRootAssembly under PublishAot.")]
[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "DictionaryEntry.Tests is rooted as a TrimmerRootAssembly under PublishAot.")]
internal static class AotTestRunner
{
    public static async Task<int> Main()
    {
        var passed = 0;
        var failed = 0;

        foreach (var type in typeof(AotTestRunner).Assembly.GetExportedTypes())
        {
            if (type.IsAbstract)
            {
                continue;
            }

            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                if (method.GetCustomAttribute<FactAttribute>() is null)
                {
                    continue;
                }

                try
                {
                    var instance = Activator.CreateInstance(type);
                    var result = method.Invoke(instance, null);
                    if (result is Task task)
                    {
                        await task.ConfigureAwait(false);
                    }

                    passed++;
                }
                catch (Exception ex)
                {
                    var inner = ex is TargetInvocationException tie && tie.InnerException is not null ? tie.InnerException : ex;
                    Console.Error.WriteLine($"FAIL {type.FullName}.{method.Name}: {inner}");
                    failed++;
                }
            }
        }

        Console.WriteLine($"NativeAOT tests complete — Passed: {passed}, Failed: {failed}");
        return failed == 0 && passed > 0 ? 0 : 1;
    }
}
#endif
