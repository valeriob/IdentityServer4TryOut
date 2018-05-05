using IdentityServer4.Services;
using IdentityServer4.Stores;
using System;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;
using IdentityServer4.Models;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Linq;
using OnAuth.JSonFilesConfigurationStore;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods to add Json files support to IdentityServer.
    /// </summary>
    public static class JSonFilesConfigurationStoreExtensions
    {
        /// <summary>
        /// Configures Json files implementation of IClientStore, IResourceStore, and ICorsPolicyService with IdentityServer.
        /// </summary>
        /// <typeparam name="TContext">The IConfigurationDbContext to use.</typeparam>
        /// <param name="builder">The builder.</param>
        /// <param name="storeOptionsAction">The store options action.</param>
        /// <returns></returns>
        public static IIdentityServerBuilder AddJsonFilesConfigurationStore(this IIdentityServerBuilder builder, Action<ConfigurationStoreOptions> storeOptionsAction = null)
        {
            var options = new ConfigurationStoreOptions();
            builder.Services.AddSingleton(options);
            storeOptionsAction?.Invoke(options);

            var store = new JSonFilesInMemoryCache(options);
            builder.Services.AddSingleton(store);

            builder.AddClientStore<JSonFilesClientStore>();
            builder.AddResourceStore<JSonFilesResourceStore>();
            builder.AddCorsPolicyService<JSonFilesCorsPolicyService>();

            return builder;
        }

    }


    public class ConfigurationStoreOptions
    {
        public string BaseFolder { get; set; }

        public ConfigurationStoreOptions()
        {
            BaseFolder = ".";
        }
    }
}