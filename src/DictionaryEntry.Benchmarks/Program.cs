using System.Reflection;
using BenchmarkDotNet.Running;

// BenchmarkRunner.Run<GetOrAddBenchmarks>();
BenchmarkSwitcher.FromAssembly(Assembly.Load("DictionaryEntry.Benchmarks")).Run(args);
