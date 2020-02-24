using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace FastExpressionKit.BulkInsert
{
    public struct MapperPropertyData
    {
        public string Name;
        public string DbColumnName;

        public Type FieldType { get; internal set; }
    }

    public class TableBulkInserter<TEntity>
    {
        
        public string TableName { get; set; }
        public IReadOnlyList<MapperPropertyData> Properties { get; set; }
        public FieldExtract<TEntity, object> FieldExtracter { get; set; }

    }

    public static class BulkInserter
    {

        public static string CreateInsertSql(IReadOnlyList<MapperPropertyData> Properties)
        {
            return "";


        }
        public static void CreateBulkInserter<T>()
        {
            var props = ReflectionHelper.GetProps<T>();

            var tableAttrs = typeof(T).GetCustomAttributes(typeof(TableAttribute), true);
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
                mappedProps.Add(new MapperPropertyData {
                    Name = prop.Name,
                    DbColumnName = columnName,
                    FieldType = prop.PropertyType
                });

                sql.Append(columnName);
                sql.Append(",");
                index++;
                valuesSql.Append(":B");
                valuesSql.Append(index);
                valuesSql.Append(",");
            };

            // remove last ','
            sql.Length--;
            sql.Append(") ");
            valuesSql.Length--;
            valuesSql.Append(')');

            sql.Append(valuesSql);

            var finalSql = sql.ToString();
        }

    }
}
