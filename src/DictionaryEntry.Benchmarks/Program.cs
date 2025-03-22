using System.Reflection;
using BenchmarkDotNet.Running;

BenchmarkSwitcher.FromAssembly(Assembly.Load("DictionaryEntry.Benchmarks")).Run(args);
