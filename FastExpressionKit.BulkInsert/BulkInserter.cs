using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace FastExpressionKit.BulkInsert
{

    // these are compatible with a proprietary database driver
    public enum DbParameterTypeNumbers
    {
        NVarChar = 18, 
        Number = 19, 
        Raw = 22, 
        TimeStamp = 25
    }


    public class MapperPropertyData
    {
        public string Name;
        public string DbColumnName;
        public string ParameterNameInQuery;
        public Type FieldType { get; set; }
    }

    // you can use this to construct DbParameter objects for your db driver
    // e.g. OracleParameter
    public class BulkInsertInstructions
    {
        public object[] Values { get; set; }

        public DbParameterTypeNumbers DbParamType { get; set; }
        public string ParameterName { get; set; }

    }
    public class FastBulkInserter<TEntity>
    {
        
        public string TableName { get; set; }
        public IReadOnlyList<MapperPropertyData> Properties { get; set; }
        public FieldExtract<TEntity, object> FieldExtractor { get; set; }
        public string InsertSql { get; set; }
        public IReadOnlyList<BulkInsertInstructions> BuildInstructionsForRows(IReadOnlyList<TEntity> rows)
        {
            var objs = FieldExtractUtil.ExtractToObjectArrays(FieldExtractor, rows);
            var instructions = new BulkInsertInstructions[Properties.Count];

            for (var i=0; i < Properties.Count; i++)
            {
                var prop = Properties[i];
                instructions[i] = new BulkInsertInstructions
                {
                    DbParamType = BulkInserter.ParameterTypeMap[prop.FieldType],
                    ParameterName = prop.ParameterNameInQuery,
                    Values = objs[i]
                };
            }
            return instructions;
        }
    }

    public static class BulkInserter
    {
        public static Dictionary<Type, DbParameterTypeNumbers> ParameterTypeMap = new Dictionary<Type, DbParameterTypeNumbers>
        {
            [typeof(Guid)] = DbParameterTypeNumbers.Raw,
            [typeof(DateTime)] = DbParameterTypeNumbers.TimeStamp,
            [typeof(string)] = DbParameterTypeNumbers.NVarChar,
            [typeof(Int32)] = DbParameterTypeNumbers.Number,
            [typeof(Nullable<DateTime>)] = DbParameterTypeNumbers.TimeStamp

        };


        // you should use this to create inserters BUT you should cache them
        public static FastBulkInserter<TEntity> CreateBulkInserter<TEntity>()
        {
            var props = ReflectionHelper.GetProps<TEntity>();

            var tableAttrs = typeof(TEntity).GetCustomAttributes(typeof(TableAttribute), true);
            var tableName = tableAttrs.Length > 1 ? ((TableAttribute)tableAttrs[0]).Name : null;

            var mappedProps = new List<MapperPropertyData>();

            
            var sql = new StringBuilder();
            sql.Append("INSERT INTO ");
            sql.Append(tableName);
            sql.Append(" (");

            // yeah we build this at the same cycle

            var valuesSql = new StringBuilder();
            valuesSql.Append("VALUES (");
            int index = 0;
            var mappedColumnNames = new List<string>(props.Length);
            foreach (var prop in props)        
            {
                var columnAttr = prop.GetCustomAttributes(typeof(ColumnAttribute), true );
                // we only take first column attribute
                var columnName = columnAttr.Length > 0 ? ((ColumnAttribute) columnAttr[0]).Name : null;

                if (columnName == null)
                {
                    // we skip columns with no [Column] attribute
                    continue;
                }

                var paramNameInQuery = ":B" + index.ToString();
                mappedProps.Add(new MapperPropertyData {
                    Name = prop.Name,
                    DbColumnName = columnName,
                    FieldType = prop.PropertyType,
                    ParameterNameInQuery = paramNameInQuery

                });

                mappedColumnNames.Add(prop.Name);

                sql.Append(columnName);
                sql.Append(",");
                index++;
                valuesSql.Append(paramNameInQuery);
                valuesSql.Append(",");
            };

            // remove last ',' and close both strings
            sql.Length--;
            sql.Append(") ");
            valuesSql.Length--;
            valuesSql.Append(')');

            sql.Append(valuesSql);
            var finalSql = sql.ToString();
            var extractor = new FieldExtract<TEntity, object>(mappedColumnNames.ToArray());
            var bulkInserter = new FastBulkInserter<TEntity>()
            {
                InsertSql = finalSql,
                Properties = mappedProps,
                TableName = tableName,
                FieldExtractor = extractor
            };
            return bulkInserter;
        }

    }
}
