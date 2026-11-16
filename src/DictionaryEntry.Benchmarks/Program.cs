using BenchmarkDotNet.Running;
using DictionaryEntry.Benchmarks;

BenchmarkSwitcher.FromAssembly(typeof(BenchmarkBase).Assembly).Run(args);
