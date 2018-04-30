using Microsoft.AspNetCore.Identity;
using OnAuth.Migrate.AdoNet;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;

namespace OnAuth.Migrate
{
    public class MigrateFromIdentity_To_IdentityCore
    {
        AdoNetDbContext _from;
        AdoNetDbContext _to;

        public MigrateFromIdentity_To_IdentityCore(AdoNetDbContext from, AdoNetDbContext to)
        {
            _from = from;
            _to = to;
        }

        public void Copy()
        {
            using (var conFrom = _from.CreateOpenedConnection())
            using (var conTo = _to.CreateOpenedConnection())
            using (var txTo = conTo.BeginTransaction())
            {
                AspNetUsers(conFrom, txTo);
                AspNetRoles(conFrom, txTo);
                AspNetUserClaims(conFrom, txTo);

                txTo.Commit();
            }
        }

        /*
         
             
               public abstract class IdentityDbContext<TUser, TRole, TKey, TUserClaim, TUserRole, TUserLogin, TRoleClaim, TUserToken> : IdentityUserContext<TUser, TKey, TUserClaim, TUserLogin, TUserToken>
        where TUser : IdentityUser<TKey>
        where TRole : IdentityRole<TKey>
        where TKey : IEquatable<TKey>
        where TUserClaim : IdentityUserClaim<TKey>
        where TUserRole : IdentityUserRole<TKey>
        where TUserLogin : IdentityUserLogin<TKey>
        where TRoleClaim : IdentityRoleClaim<TKey>
        where TUserToken : IdentityUserToken<TKey>
             */

        void AspNetUsers(DbConnection from, DbTransaction txTo)
        {
            var users = _from.Query<IdentityUser>(from, "select * from AspNetUsers");
            foreach (var user in users)
            {
                if (user.NormalizedUserName == null)
                    user.NormalizedUserName = user.UserName.Normalize().ToUpperInvariant();
                if (user.NormalizedEmail == null && user.Email != null)
                    user.NormalizedEmail = user.Email.Normalize().ToUpperInvariant();
                _to.Insert(txTo, user, "AspNetUsers");
            }
        }

        void AspNetRoles(DbConnection from, DbTransaction txTo)
        {
            var roles = _from.Query<IdentityRole>(from, "select * from AspNetRoles");
            foreach (var role in roles)
            {
                if (role.NormalizedName == null)
                    role.NormalizedName = role.Name.Normalize().ToUpperInvariant();

                _to.Insert(txTo, role, "AspNetRoles");
            }
        }
        void AspNetUserClaims(DbConnection from, DbTransaction txTo)
        {
            var roles = _from.Query<IdentityUserClaim<string>>(from, "select * from AspNetUserClaims");

            using (var cmd = txTo.Connection.CreateCommand())
            {
                cmd.Transaction = txTo;
                cmd.CommandText = "SET IDENTITY_INSERT AspNetUserClaims ON";
                var result = cmd.ExecuteNonQuery();
            }

            foreach (var role in roles)
            {
                _to.Insert(txTo, role, "AspNetUserClaims");
            }
        }


    }
}
