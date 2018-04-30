using OnAuth.Migrate.AdoNet;
using System;
using System.Data.Common;
using System.Data.SqlClient;

namespace OnAuth.Migrate
{
    class Program
    {
        static void Main(string[] args)
        {
            var dbpf = SqlClientFactory.Instance;
            string providerName = "System.Data.SqlClient";
            var csFrom = @"Data Source=10.252.0.1\SQLEXPRESS;Initial Catalog=onauth;Integrated Security=False;User ID=onauth;Password=onit!2016;Connect Timeout=60;Encrypt=False;TrustServerCertificate=False";
            var csTo = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=aspnet-WebApplication2-AC2B563B-8A48-415D-8265-C68F54444838;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
            var from = new AdoNetDbContext(dbpf, providerName, csFrom, "OnAuth");
            var to = new AdoNetDbContext(dbpf, providerName, csTo, "aspnet-WebApplication2-AC2B563B-8A48-415D-8265-C68F54444838");

            var m = new MigrateFromIdentity_To_IdentityCore(from, to);
            m.Copy();
        }
    }
}
