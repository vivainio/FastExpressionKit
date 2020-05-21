using System;
using TrivialTestRunner;
using FastExpressionKitTests;

namespace FastExpressionKit.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            TRunner.CrashHard = true;
            TRunner.AddTests<FastExprKitTest>();
            TRunner.RunTests();
            TRunner.ReportAll();
        }
    }
}
