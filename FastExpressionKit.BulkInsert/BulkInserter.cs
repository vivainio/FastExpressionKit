using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Text;

namespace FastExpressionKit.BulkInsert
{

    public class TableMappingRules
    {
        public string TableName { get; set; }
        public Func<PropertyInfo, string> ColumnNameGenerator { get; set; }

        static string ToUnderscoreCase(string str) {
            // xx rewrite without linq
            return string.Concat(str.Select((x, i) => i > 0 && char.IsUpper(x) ? 
                "_" + x.ToString() : x.ToString())).ToLower();
        }

        static string GuessColumnNameBasedOnPropertyInfo(PropertyInfo pi)
        {
            if ( pi.PropertyType != typeof(string) && typeof(IEnumerable).IsAssignableFrom(pi.PropertyType))
            {
                return null;
            }
            return ToUnderscoreCase(pi.Name).ToUpperInvariant();
        }

        public static TableMappingRules UpperSnake(string tableName) =>
            new TableMappingRules
            {
                TableName = tableName,
                ColumnNameGenerator = GuessColumnNameBasedOnPropertyInfo
            };
    }
    
    // these are compatible with a proprietary database driver
    public enum DbParameterTypeNumbers
    {
        Date = 8,
        Double = 9,
        Float = 10,
        Integer = 11,
        NVarChar = 18, 
        Number = 19, 
        Raw = 22, 
        TimeStamp = 25,
        VarChar = 28,
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
                var ok = FastBulkInsertUtil.ParameterTypeMap.TryGetValue(prop.FieldType, out var paramType);
                if (!ok)
                {
                    throw new InvalidOperationException($"Can't map property {prop.Name} type: {prop.FieldType.FullName}");
                }
                instructions[i] = new BulkInsertInstructions
                {
                    DbParamType = FastBulkInsertUtil.ParameterTypeMap[prop.FieldType],
                    ParameterName = prop.ParameterNameInQuery,
                    Values = objs[i]
                };
            }
            return instructions;
        }
    }

    // use as singleton
    public static class FastBulkInsertCache
    {
        // no point in concurrent dictionary because it has to be populated well ahead of time
        
        private static Dictionary<Type, object> cache = new Dictionary<Type, object>();

        public static void Add<TEntity>(FastBulkInserter<TEntity> inserter)
        {
            cache.Add(typeof(TEntity), inserter);
        }

        public static FastBulkInserter<TEntity> Get<TEntity>()
        {
            return (FastBulkInserter<TEntity>) cache[typeof(TEntity)];
        }
    }
    public static class FastBulkInsertUtil
    {
        public static readonly Dictionary<Type, DbParameterTypeNumbers> ParameterTypeMap = new Dictionary<Type, DbParameterTypeNumbers>
        {
            [typeof(Guid)] = DbParameterTypeNumbers.Raw,
            [typeof(DateTime)] = DbParameterTypeNumbers.Date,
            [typeof(DateTime?)] = DbParameterTypeNumbers.Date,
            [typeof(string)] = DbParameterTypeNumbers.NVarChar,
            [typeof(Int32)] = DbParameterTypeNumbers.Number,
            [typeof(Int32?)] = DbParameterTypeNumbers.Number,
            [typeof(Int16)] = DbParameterTypeNumbers.Number,
            [typeof(Int16?)] = DbParameterTypeNumbers.Number,
            [typeof(decimal)] = DbParameterTypeNumbers.Number,
            [typeof(decimal?)] = DbParameterTypeNumbers.Number,
        };



        // you should use this to create inserters BUT you should cache them
        // if rules are used, column names are guessed
        // yeah use [Table] and [Column] instead
        public static FastBulkInserter<TEntity> CreateBulkInserter<TEntity>(TableMappingRules rules = null)
        {
            var guessingMode = rules != null;
            var props = ReflectionHelper.GetProps<TEntity>();

            var tableAttrs = typeof(TEntity).GetCustomAttributes(typeof(TableAttribute), true);
            string tableName = null;
            
            if (rules != null)
            {
                tableName = rules.TableName;
            }
            else
            {
                tableName = tableAttrs.Length > 0 ? ((TableAttribute)tableAttrs[0]).Name : null;
            }
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

                if (columnName == null && !guessingMode)
                {
                    // we skip columns with no [Column] attribute
                    continue;
                }

                if (guessingMode)
                {
                    columnName = rules.ColumnNameGenerator(prop);
                    // way to skip property - return null from name generator
                    if (columnName == null)
                    {
                        continue;
                    }
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
