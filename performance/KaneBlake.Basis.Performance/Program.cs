using BenchmarkDotNet.Running;
using KaneBlake.Basis.Performance.Benchmarks;
using System;

namespace KaneBlake.Basis.Performance
{
    class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run(typeof(Program).Assembly);

        }
    }
}
