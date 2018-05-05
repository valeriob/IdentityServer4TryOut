using OnAuth.LDAPUserStore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class LDAPIdentityServerBuilderExtensions
    {
        public static IIdentityServerBuilder AddLdapUserStore(this IIdentityServerBuilder builder)
        {
            builder.Services.AddSingleton<LdapUserStore>();
            builder.AddProfileService<ProfileService>();
            builder.AddResourceOwnerValidator<CustomResourceOwnerPasswordValidator>();

            return builder;
        }
    }
}