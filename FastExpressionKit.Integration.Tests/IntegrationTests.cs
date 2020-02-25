using AutoFixture;
using Devart.Data.Oracle;
using FastExpressionKit.BulkInsert;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrivialTestRunner;

namespace FastExpressionKit.Integration.Tests
{

    public static class DevartFastBulkInsert
    {
        public static void FastBulkInsert<TEntity>(IReadOnlyList<TEntity> rows, IDbTransaction transaction)
        {

            var bi = BulkInserter.CreateBulkInserter<TEntity>();
            var instr = bi.BuildInstructionsForRows(rows);

            var tx = (OracleTransaction)transaction;
            var conn = (OracleConnection) transaction.Connection;

            var inputLen = rows.Count;
            var command = conn.CreateCommand();
            var allParams = instr.Select(it =>
            {
                var param = new OracleParameter(it.ParameterName,
                    (OracleDbType)(int)it.DbParamType,
                    it.Values,
                    System.Data.ParameterDirection.Input);
                param.ArrayLength = inputLen;
                return param;
            }).ToArray();
            command.Parameters.AddRange(allParams);
            command.ExecuteArray(inputLen);

        }

        public static void FastBulkInsertWithEfContext<TEntity>(this DbContext context, IReadOnlyList<TEntity> rows)
        {
            var conn = context.Database.Connection;

            var needClosing = false;
            if (conn.State == System.Data.ConnectionState.Closed)
            {
                conn.Open();
                needClosing = true;
            };

            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    FastBulkInsert(rows, transaction);
                }

                catch (Exception)
                {
                    transaction.Rollback();
                    if (needClosing)
                    {
                        conn.Close();

                    }
                }
            }

        }
    }
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
            

        }


    }
}
