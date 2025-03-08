using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace Sparkitect.Benchmark;

public static class Program
{
    static void Main()
    {
        BenchmarkRunner.Run<RandomMaskComp>();
    }
}