using AutoFixture;
using Devart.Data.Oracle;
using FastExpressionKit.BulkInsert;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Entity;
using System.IO;
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

            // warning, oracle specific code
            var bi = BulkInserter.CreateBulkInserter<TEntity>();
            var instr = bi.BuildInstructionsForRows(rows);

            var tx = (OracleTransaction)transaction;
            var conn = (OracleConnection) transaction.Connection;

            var inputLen = rows.Count;
            var command = conn.CreateCommand(bi.InsertSql, CommandType.Text);
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

        public static void FastBulkInsertWithConnection<TEntity>(IReadOnlyList<TEntity> rows, DbConnection conn)
        {
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
                    transaction.Commit();
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    if (needClosing)
                    {
                        conn.Close();
                    }
                    throw;
                }
            }
        }


        public static void FastBulkInsertWithEfContext<TEntity>(this DbContext context, IReadOnlyList<TEntity> rows)
        {
            var conn = context.Database.Connection;
            FastBulkInsertWithConnection(rows, conn);
        }
    }
    public class IntegrationTests

    {
        static string ConnectionStringForIntegrationTest => File.ReadAllText(@"C:\o\3rdParty\Devart\connection_string.txt").Trim();

        static OracleConnection GetConnection() => new OracleConnection(ConnectionStringForIntegrationTest);
        
        [Case]
        public static void ConnectToLocalDbWithDevart()
        {
            
            var conn = new OracleConnection(ConnectionStringForIntegrationTest);
            conn.Open();
            conn.Close();

        }
        [fCase]
        public static void InsertFew()
        {
            var f = new Fixture();
            var testentities = f.CreateMany<TestDbEntity>(22000).ToArray();
            var conn = GetConnection();
            DevartFastBulkInsert.FastBulkInsertWithConnection<TestDbEntity>(testentities, conn);
        }
    }
}
