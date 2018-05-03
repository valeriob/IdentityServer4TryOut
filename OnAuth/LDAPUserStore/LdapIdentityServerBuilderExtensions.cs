using OnAuth.LDAP;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class LdapIdentityServerBuilderExtensions
    {
        public static IIdentityServerBuilder AddLdapUserStore(this IIdentityServerBuilder builder)
        {
            builder.Services.AddSingleton(new LdapUserStore());
            builder.AddProfileService<ProfileService>();
            builder.AddResourceOwnerValidator<CustomResourceOwnerPasswordValidator>();

            return builder;
        }
    }
}