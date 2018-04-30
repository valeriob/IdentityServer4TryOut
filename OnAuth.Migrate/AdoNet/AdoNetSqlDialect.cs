using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OnAuth.Migrate.AdoNet
{
    public class AdoNetSqlDialect : AdoNetDialect
    {
        public override string GetParameterToken()
        {
            return "@";
        }

        protected override string ToDbType(PropertyInfo pi)
        {
            if (pi.Name == "Id")
                return "varchar(128)";

            if (pi.PropertyType == typeof(int))
                return "int";
            if (pi.PropertyType == typeof(long))
                return "bigint";
            if (pi.PropertyType == typeof(string))
                return "varchar(max)";

            if (pi.PropertyType == typeof(DateTime))
                return "datetime2";

            if (pi.PropertyType == typeof(Guid))
                return "uniqueidentifier";

            if (pi.PropertyType == typeof(double))
                return "float";

            if (pi.PropertyType.IsGenericType && pi.PropertyType.GenericTypeArguments.Length == 1)
            {
                var inner = ToDbType(pi.PropertyType.GenericTypeArguments[0]);
                if (inner != null)
                    return inner;
            }

            throw new NotImplementedException();
        }

        string ToDbType(Type type)
        {
            if (type == typeof(int))
                return "int";
            if (type == typeof(long))
                return "bigint";
            if (type == typeof(string))
                return "varchar(max)";
            if (type == typeof(DateTime))
                return "datetime2";
            if (type == typeof(Guid))
                return "uniqueidentifier";

            if (type == typeof(double))
                return "float";

            return null;
        }

        SqlDbType ToSqlDbType(SqlDbType currentValue, Type type)
        {
            if (type == typeof(DateTime) || type == typeof(DateTime?))
                return SqlDbType.DateTime2;
            return currentValue;
        }

        public override IDbDataParameter CreateParameter(DbCommand cmd, OnTmsPropertyDescriptor pd, object document)
        {
            var sqlcmd = cmd as SqlCommand;

            var p = sqlcmd.CreateParameter();
            var piValue = pd.GetValue(document);
            if (piValue == null || double.NaN.Equals(piValue))
                p.Value = DBNull.Value;
            else
                p.Value = piValue;

            p.ParameterName = GetParameterToken() + pd.FieldName;
            if (pd.PropertyInfo.PropertyType == typeof(DateTime) || pd.PropertyInfo.PropertyType == typeof(DateTime?))
                p.SqlDbType = SqlDbType.DateTime2;
            return p;
        }

        protected override void ApplyDbType(DbParameter par, Type type)
        {
            var sqlPar = par as System.Data.SqlClient.SqlParameter;
            if (type == typeof(DateTime) || type == typeof(DateTime?))
                sqlPar.SqlDbType = SqlDbType.DateTime2;
        }


        //internal void Insert<T>(DbConnection con, string tableName, IEnumerable<T> items)
        //{
        //    using (var bulkCopy = new SqlBulkCopy(con as SqlConnection))
        //    {
        //        bulkCopy.BatchSize = 100;
        //        bulkCopy.DestinationTableName = tableName;
        //        var dataTable = items.AsDataTable();

        //        var properties = TypeDescriptor.GetProperties(typeof(T));
        //        foreach (PropertyDescriptor prop in properties)
        //            bulkCopy.ColumnMappings.Add(prop.Name, prop.Name);

        //        bulkCopy.WriteToServer(dataTable);
        //    }
        //}

        public override void BulkInsert<T>(DbConnection con, string tableName, IEnumerable<T> items)
        {
            using (var bulkCopy = new SqlBulkCopy(con as SqlConnection))
            {
                bulkCopy.BatchSize = 512;
                bulkCopy.DestinationTableName = tableName;
                var dataTable = items.AsDataTable();

                var properties = TypeDescriptor.GetProperties(typeof(T));
                foreach (PropertyDescriptor prop in properties)
                        bulkCopy.ColumnMappings.Add(prop.Name, prop.Name);

                bulkCopy.WriteToServer(dataTable);
            }
        }

        public override void BulkInsert<T>(DbConnection con, DbTransaction tx, string tableName, IEnumerable<T> items)
        {
            using (var bulkCopy = new SqlBulkCopy(con as SqlConnection, SqlBulkCopyOptions.CheckConstraints, tx as SqlTransaction))
            {
                bulkCopy.BatchSize = 512;
                bulkCopy.DestinationTableName = tableName;

                bulkCopy.WriteToServer(items.AsDataTable());
            }
        }

    }


}
