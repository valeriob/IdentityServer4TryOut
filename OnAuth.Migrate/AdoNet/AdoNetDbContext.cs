using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;

namespace OnAuth.Migrate.AdoNet
{
    public class AdoNetDbContext : AdoNetContext
    {
        string _databaseName;

        public AdoNetDbContext(AdoNetContext ctx, string databaseName) : base(ctx)
        {
            _databaseName = databaseName;
        }
#if NETFULL
        public AdoNetDbContext(string connectionName, string databaseName) : base(connectionName)
        {
            _databaseName = databaseName;
        }
#endif
        public AdoNetDbContext(DbProviderFactory factory, string providerName, string connectionString, string databaseName) : base(factory, providerName, connectionString)
        {
            _databaseName = databaseName;
        }

        public override DbConnection CreateOpenedConnection()
        {
            var con = base.CreateOpenedConnection();
            con.ChangeDatabase(_databaseName);
            return con;
        }
    }

}
