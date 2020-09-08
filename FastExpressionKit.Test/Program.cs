using System;
using TrivialTestRunner;
using FastExpressionKitTests;
using System.Threading.Tasks;

namespace FastExpressionKit.Test
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            // use while debugging
            // TRunner.CrashHard = true;
            TRunner.AddTests<FastExprKitTest>();
            await TRunner.RunTestsAsync();
            TRunner.ReportAll();
            Console.WriteLine("ok");
            return TRunner.ExitStatus;
        }
    }
}
