using System;
using TrivialTestRunner;
using FastExpressionKitTests;
using System.Threading.Tasks;

namespace FastExpressionKit.Test
{
    class Program
    {
        static async Task Main(string[] args)
        {
            TRunner.CrashHard = true;
            TRunner.AddTests<FastExprKitTest>();
            await TRunner.RunTestsAsync();
            TRunner.ReportAll();
        }
    }
}
