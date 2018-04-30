using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OnAuth.Migrate.AdoNet
{
    public abstract class AdoNetDialect
    {
        protected static PropertyCache _cache = new PropertyCache();

        public abstract string GetParameterToken();

        protected abstract string ToDbType(PropertyInfo pi);


        public virtual string[] GetDatabases(DbConnection con)
        {
            var result = new List<string>();
            {
                using (var cmd = con.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM sys.databases where name like 'OnTms%'";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var db = reader.GetString(0);
                            result.Add(db);
                        }
                    }
                }
            }
            return result.ToArray();
        }

        public virtual void CreateDatabase(DbConnection c, string name)
        {
            using (var cmd = c.CreateCommand())
            {
                cmd.CommandText = string.Format("CREATE DATABASE [{0}];", name);
                cmd.ExecuteNonQuery();
            }
        }

        public virtual void DeleteDatabase(DbConnection c, string name)
        {
            using (var cmd = c.CreateCommand())
            {
                var sql = @"USE master;
ALTER DATABASE [{0}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
DROP DATABASE [{0}] ;";
                cmd.CommandText = string.Format(sql, name);
                cmd.ExecuteNonQuery();
            }
        }

        public void UpdateTableFor<T>(DbConnection c)
        {
            try
            {
                CreateTableFor<T>(c);
            }
            catch { }
        }

        public virtual void CreateTableFor<T>(DbConnection c)
        {
            var tableName = typeof(T).Name;

            var properties = _cache.GetProperties(typeof(T));

            var columns = properties.Select(p => string.Format("{0} {1} {2}", p.FieldName, ToDbType(p.PropertyInfo), NullNotNull(p.PropertyInfo)));

            using (var cmd = c.CreateCommand())
            {
                cmd.CommandText = string.Format(@"CREATE Table {0}
                (
                    {1},
                    --CONSTRAINT [PK_{0}] PRIMARY KEY (Id)
                )", tableName, string.Join("," + System.Environment.NewLine, columns));
                cmd.ExecuteNonQuery();
            }
        }

        public virtual void DropTable<T>(DbConnection con)
        {
            try
            {
                using (var cmd = con.CreateCommand())
                {
                    cmd.CommandText = string.Format("DROP TABLE [{0}];", typeof(T).Name);
                    cmd.ExecuteNonQuery();
                }
            }
            catch { }
        }

        public virtual void CreatePrimaryKey<T>(DbConnection con)
        {
            var tableName = typeof(T).Name;

            using (var cmd = con.CreateCommand())
            {
                cmd.CommandText = $"ALTER TABLE [dbo].[{tableName}] ADD CONSTRAINT[PK_{tableName}] PRIMARY KEY CLUSTERED([Id] ASC);";
                cmd.ExecuteNonQuery();
            }
        }


        public virtual void Insert<T>(DbTransaction tx, T entity)
        {
            var tableName = entity.GetType().Name;
            Insert<T>(tx, entity, tableName);
        }

        public virtual void Insert<T>(DbTransaction tx, T entity, string tableName)
        {
            using (var cmd = tx.Connection.CreateCommand())
            {
                cmd.Transaction = tx;
                var properties = _cache.GetProperties(typeof(T));
                var fields = string.Join(",", properties.Select(p => p.FieldName));
                var placeHolders = string.Join(",", properties.Select(p => GetParameterToken() + p.FieldName));

                cmd.CommandText = $"INSERT INTO {tableName} ({fields}) Values ({placeHolders})";

                foreach (var pi in properties)
                {
                    var par = cmd.CreateParameter();
                    par.ParameterName = pi.FieldName;
                    par.Value = pi.GetValue(entity);
                    cmd.Parameters.Add(par);

                    // ApplyDbType(par, pi.PropertyInfo.PropertyType);
                }

                var count = cmd.ExecuteNonQuery();
                if (count != 1)
                    throw new Exception();
            }
        }

        public virtual int Insert(DbCommand cmd, object document)
        {
            var type = document.GetType();
            var name = type.Name;
            var properties = _cache.GetProperties(type);

            var columns = string.Join(",", properties.Select(r => r.FieldName));
            var values = string.Join(",", properties.Select(r => GetParameterToken() + r.FieldName));

            cmd.CommandText = string.Format("INSERT INTO {0} ({1}) VALUES({2})", name, columns, values);

            var par = properties.Select(p => CreateParameter(cmd, p, document)).ToList();
            par.ForEach(p => cmd.Parameters.Add(p));

            return cmd.ExecuteNonQuery();
        }

        public virtual int Insert(DbTransaction tx, object document)
        {
            using (var cmd = tx.Connection.CreateCommand())
            {
                cmd.Transaction = tx;
                return Insert(cmd, document);
            }
        }


        public void Update<T>(DbTransaction tx, T entity, string whereClause)
        {
            var tableName = entity.GetType().Name;

            using (var cmd = tx.Connection.CreateCommand())
            {
                var properties = _cache.GetProperties(typeof(T));
                var asd = properties.Select(p => p.FieldName).ToArray();

                var fields = string.Join(",", asd.Select(s => $"{s}={GetParameterToken()}{s}"));

                cmd.CommandText = $"UPDATE {tableName} SET {fields} WHERE {whereClause}";

                foreach (var pi in properties)
                {
                    var par = cmd.CreateParameter();
                    par.ParameterName = pi.FieldName;
                    par.Value = pi.GetValue(entity);
                    cmd.Parameters.Add(par);
                }

                var count = cmd.ExecuteNonQuery();
                if (count != 1)
                    throw new Exception();
            }
        }

        public virtual int Update(DbCommand cmd, object document, Guid concurrency)
        {
            var type = document.GetType();
            var name = type.Name;
            var properties = _cache.GetProperties(type);

            var columns = properties.Select(p => p.FieldName).Where(p => p != "Id").ToArray();
            var set = string.Join(",", columns.Select(r => string.Format("{0}={1}{0}", r, GetParameterToken())));

            cmd.CommandText = string.Format("UPDATE [{0}] SET {1} WHERE Id={2}Id AND Concurrency={2}OldConcurrency", name, set, GetParameterToken());

            var parameters = properties.Select(p => CreateParameter(cmd, p, document)).ToList();
            parameters.Add(CreateParameter(cmd, "OldConcurrency", concurrency));
            parameters.ForEach(p => cmd.Parameters.Add(p));

            return cmd.ExecuteNonQuery();
        }

        public virtual int Update(DbCommand cmd, object document)
        {
            var type = document.GetType();
            var name = type.Name;
            var properties = _cache.GetProperties(type);

            var columns = properties.Select(p => p.FieldName).Where(p => p != "Id").ToArray();
            var set = string.Join(",", columns.Select(r => string.Format("{0}={1}{0}", r, GetParameterToken())));

            cmd.CommandText = string.Format("UPDATE [{0}] SET {1} WHERE Id={2}Id", name, set, GetParameterToken());

            var parameters = properties.Select(p => CreateParameter(cmd, p, document)).ToList();
            parameters.ForEach(p => cmd.Parameters.Add(p));

            return cmd.ExecuteNonQuery();
        }


        public virtual IDbDataParameter CreateParameter(DbCommand cmd, OnTmsPropertyDescriptor pd, object document)
        {
            var p = cmd.CreateParameter();
            var piValue = pd.GetValue(document);
            if (piValue == null || double.NaN.Equals(piValue))
                p.Value = DBNull.Value;
            else
                p.Value = piValue;
            p.ParameterName = GetParameterToken() + pd.FieldName;
            return p;
        }

        public virtual IDbDataParameter CreateParameter(DbCommand cmd, string name, object value)
        {
            var p = cmd.CreateParameter();
            p.Value = value;
            p.ParameterName = GetParameterToken() + name;
            return p;
        }



        protected virtual string NullNotNull(PropertyInfo pi)
        {
            if (pi.Name == "Id")
                return "NOT NULL";
            return "NULL";
        }

        protected virtual void ApplyDbType(DbParameter cmd, Type type)
        {

        }

        public virtual void BulkInsert<T>(DbConnection con, DbTransaction tx, string tableName, IEnumerable<T> items)
        {
            throw new NotImplementedException();
        }
        public virtual void BulkInsert<T>(DbConnection con, string tableName, IEnumerable<T> items)
        {
            throw new NotImplementedException();
        }
    }

    public class AdoNetOracleDialect : AdoNetDialect
    {
        public override string GetParameterToken()
        {
            return ":";
        }

        protected override string ToDbType(PropertyInfo pi)
        {
            throw new NotImplementedException();
        }
    }
}
