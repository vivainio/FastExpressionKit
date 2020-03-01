using System;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using TrivialTestRunner;

namespace FastExpressionKit.Integration.Tests
{
    class Program
    {
        static void RunBenchmarks()
        {
            var config = DefaultConfig.Instance.With(ConfigOptions.DisableOptimizationsValidator);
            var summary = BenchmarkRunner.Run(typeof(Program).Assembly, config);


        }
        static void Main(string[] args)

        {
            if (args.Length > 0)
            {
                RunBenchmarks();
                
            }
            else
            {
                TRunner.CrashHard = false;
                TRunner.AddTests<IntegrationTests>();
                TRunner.RunTests();
                TRunner.ReportAll();
                
            }
        }
    }
}
