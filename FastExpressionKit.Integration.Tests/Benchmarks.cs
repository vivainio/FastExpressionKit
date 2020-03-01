using System.Threading;
using BenchmarkDotNet.Attributes;

namespace FastExpressionKit.Integration.Tests
{
    public class Benchmarks
    {
        [Benchmark]
        public void Hello()
        {
            Thread.Sleep(100);
        }
    }
}