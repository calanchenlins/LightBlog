using BenchmarkDotNet.Running;
using K.Basis.Performance.Benchmarks;
using System;

namespace KaneBlake.Basis.Performance
{
    class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run(typeof(StringSearchBenchmark2));
            Console.ReadLine();
        }
    }
}
