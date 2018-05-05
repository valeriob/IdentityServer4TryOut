using OnAuth.PersistentGrants;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class LMDBPersistedGrantStoreBuilderExtensions
    {
        public static IIdentityServerBuilder AddLMDBPersistedGrantStore(this IIdentityServerBuilder builder)
        {
            builder.Services.AddSingleton<LMDBPersistedGrantStore>();

            return builder;
        }
    }
}