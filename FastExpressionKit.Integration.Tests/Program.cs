using System;
using TrivialTestRunner;

namespace FastExpressionKit.Integration.Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            TRunner.AddTests<IntegrationTests>();
            TRunner.RunTests();
            TRunner.ReportAll();
        }
    }
}
