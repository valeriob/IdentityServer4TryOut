using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace OnAuth.Migrate.AdoNet
{
    public class AdoNetContext
    {
        string _providerName;
        string _connectionString;

        protected readonly DbProviderFactory _factory;
        protected readonly AdoNetDialect _dialect;


        public AdoNetContext(DbProviderFactory factory, string providerName, string connectionString)
        {
            _providerName = providerName;
            _connectionString = connectionString;

            _factory = factory;
            _dialect = GetSqlDialect();
        }

#if NETFULL
        public AdoNetContext(string providerName, string connectionString)
        {
            _providerName = providerName;
            _connectionString = connectionString;

            _factory = DbProviderFactories.GetFactory(providerName);
            _dialect = GetSqlDialect();
        }

        public AdoNetContext(string connectionName)
        {
            _providerName = ConfigurationManager.ConnectionStrings[connectionName].ProviderName;
            _connectionString = ConfigurationManager.ConnectionStrings[connectionName].ConnectionString;

            _factory = DbProviderFactories.GetFactory(_providerName);
            _dialect = GetSqlDialect();
        }
#endif
        public string GetProviderName()
        {
            return _providerName;
        }

        public AdoNetDialect GetSqlDialect()
        {
            if (_providerName == "System.Data.SqlClient")
                return new AdoNetSqlDialect();
            //if (_providerName == "Npgsql")
            //    return new AdoNetPgSqlDialect();
            //if (_connectionString.ToUpper().Contains("MONETDB ODBC DRIVER"))
            //    return new AdoNetMonetDbDialect();

            //if (_providerName.ToUpper().Contains("ORACLE"))
            //    return new AdoNetOracleDialect();

            //if (_providerName.ToUpper().Contains("INFORMIX"))
            //    return new AdoNetInformixDialect();

            //if (_providerName.ToUpper().Contains("MYSQL"))
            //    return new AdoNetMySqlDialect();


            throw new NotImplementedException("provider non supportato");
        }


        public AdoNetContext(AdoNetContext ctx)
        {
            _providerName = ctx._providerName;
            _connectionString = ctx._connectionString;
            _factory = ctx._factory;
        }

        public virtual DbConnection CreateOpenedConnection()
        {
            var con = _factory.CreateConnection();
            con.ConnectionString = _connectionString;
            con.Open();
            return con;
        }

        public virtual DbConnection CreateOpenedConnection(string databaseName)
        {
            var con = _factory.CreateConnection();
            con.ConnectionString = _connectionString;
            con.ChangeDatabase(databaseName);
            con.Open();
            return con;
        }

        public DbParameter CreateParameter(string name, object value)
        {
            var p = _factory.CreateParameter();
            p.ParameterName = name;
            p.Value = value;
            return p;
        }


        public IEnumerable<T> Query<T>(DbConnection con, string sql)
        {
            using (var cmd = con.CreateCommand())
            {
                cmd.CommandText = sql;
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        yield return reader.MapTo<T>();
                }
            }
        }

        public IEnumerable<T> Query<T>(DbConnection con, string sql, IDictionary<string, object> parameters)
        {
            using (var cmd = con.CreateCommand())
            {
                cmd.CommandText = sql;
                foreach (var parameter in parameters)
                {
                    var par = cmd.CreateParameter();
                    par.ParameterName = parameter.Key;
                    par.Value = parameter.Value;
                    cmd.Parameters.Add(par);
                }
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        yield return reader.MapTo<T>();
                }
            }
        }

        static PropertyCache _cache = new PropertyCache();


        public void Insert<T>(DbTransaction tx, T entity)
        {
            _dialect.Insert<T>(tx, entity);
        }

        public void Insert<T>(DbTransaction tx, T entity, string tableName)
        {
            _dialect.Insert<T>(tx, entity, tableName);
        }

        public void Update<T>(DbTransaction tx, T entity, string whereClause)
        {
            _dialect.Update<T>(tx, entity, whereClause);
        }

    }



    public static class DataReaderExtensions
    {
        static PropertyCache _cache = new PropertyCache();

        public static T InformixMapTo<T>(this IDataReader reader, T result)
        {
            var curCulture = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                return reader.MapTo<T>(result);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = curCulture;
            }

        }

        public static T MapTo<T>(this IDataReader reader, T result)
        {
            var properties = _cache.GetProperties(typeof(T));

            foreach (var pi in properties)
            {
                if (pi.IgnoreMapping)
                    continue;
                int ordinal = -1;
                try
                {
                    ordinal = reader.GetOrdinal(pi.FieldName);
                }
                catch
                {

                }

                if (ordinal >= 0)
                {
                    var data = reader.GetValue(ordinal);

                    if (data == DBNull.Value)
                    {
                        pi.SetValue(result, null);
                    }
                    else
                    {
                        if (data is string)
                        {
                            var str = data as string;
                            pi.SetValue(result, str.TrimEnd());
                        }
                        else
                        {
                            pi.SetValue(result, data);
                        }
                    }
                }

            }

            return result;
        }

        public static T MapTo<T>(this IDataReader reader)
        {
            var result = Activator.CreateInstance<T>();
            var t = reader.GetType().FullName;
            if (t == "IBM.Data.Informix.IfxDataReader")
            {
                return reader.InformixMapTo<T>(result);
            }
            else
                return reader.MapTo<T>(result);
        }

        public static object MapTo(this IDataReader reader, Type dbType)
        {
            var pis = _cache.GetProperties(dbType);// dbType.GetProperties();
            var e = Activator.CreateInstance(dbType);
            var values = new object[pis.Length];
            reader.GetValues(values);

            for (int i = 0; i < pis.Length; i++)
            {
                var pi = pis[i];
                var value = values[i];
                if (value.Equals(DBNull.Value))
                    pi.SetValue(e, null);
                else
                    pi.SetValue(e, value);
            }
            return e;
        }

        public static DbParameter CreateParameter(this DbCommand cmd, string name, object value)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value;
            return p;

        }
    }



    public class IgnoreMappingAttribute : Attribute
    {
        public readonly static IgnoreMappingAttribute Instance = new IgnoreMappingAttribute();

    }

    public static class IEnumerableExtensions
    {
        public static DataTable AsDataTable<T>(this IEnumerable<T> data)
        {
            var properties = TypeDescriptor.GetProperties(typeof(T));
            var table = new DataTable();
            foreach (PropertyDescriptor prop in properties)
                if (prop.Attributes.Contains(IgnoreMappingAttribute.Instance) == false)
                    table.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
            foreach (T item in data)
            {
                DataRow row = table.NewRow();
                foreach (PropertyDescriptor prop in properties)
                    if (prop.Attributes.Contains(IgnoreMappingAttribute.Instance) == false)
                    {
                        var value = prop.GetValue(item);
                        if (value == null)
                            value = DBNull.Value;
                        if (double.NaN.Equals(value))
                            value = DBNull.Value;

                        row[prop.Name] = value;

                    }
                table.Rows.Add(row);
            }
            return table;
        }
    }
}
