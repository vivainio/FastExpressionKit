using AutoFixture;
using Devart.Data.Oracle;
using FastExpressionKit.BulkInsert;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrivialTestRunner;

namespace FastExpressionKit.Integration.Tests
{
    public class IntegrationTests
    {
        [fCase]
        public static void CreateParameters()
        {

            var fixture = new AutoFixture.Fixture();
            var rows = fixture.CreateMany<FastExpressionKitTests.D>().ToArray();
            var bi = BulkInserter.CreateBulkInserter<FastExpressionKitTests.D>();
            var instr = bi.BuildInstructionsForRows(rows);
            //var ds = fixture.Freeze <FastExpressionKitTests.D>();
            var op = instr.Select(it => new OracleParameter(it.ParameterName, (OracleDbType)(int)it.DbParamType, System.Data.ParameterDirection.Input));                         

        }


    }
}
