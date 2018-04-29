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

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods to add Json files support to IdentityServer.
    /// </summary>
    public static class FileSystemExtensions
    {
        /// <summary>
        /// Configures Json files implementation of IClientStore, IResourceStore, and ICorsPolicyService with IdentityServer.
        /// </summary>
        /// <typeparam name="TContext">The IConfigurationDbContext to use.</typeparam>
        /// <param name="builder">The builder.</param>
        /// <param name="storeOptionsAction">The store options action.</param>
        /// <returns></returns>
        public static IIdentityServerBuilder AddJsonConfigurationStore(this IIdentityServerBuilder builder, Action<ConfigurationStoreOptions> storeOptionsAction = null)
        {
            var options = new ConfigurationStoreOptions();
            builder.Services.AddSingleton(options);
            storeOptionsAction?.Invoke(options);

            var store = new InMemoryStore(options);
            builder.Services.AddSingleton(store);

            builder.AddClientStore<ClientStore>();
            builder.AddResourceStore<ResourceStore>();
            builder.AddCorsPolicyService<CorsPolicyService>();

            return builder;
        }

    }

    public class ClientStore : IClientStore
    {
        InMemoryStore _store;
        ILogger<ClientStore> _logger;
        Dictionary<string, Client> _clients;

        public ClientStore(InMemoryStore store, ILogger<ClientStore> logger)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _logger = logger;

            _clients = store.Clients;
        }


        public Task<Client> FindClientByIdAsync(string clientId)
        {
            _clients.TryGetValue(clientId, out Client result);
            return Task.FromResult(result);
        }
    }

    public class InMemoryStore
    {
        ConfigurationStoreOptions _options;

        public Dictionary<string, ApiResource> Api { get; private set; }
        public Dictionary<string, IdentityResource> Identity { get; private set; }
        public Dictionary<string, Client> Clients { get; private set; }

        public InMemoryStore(ConfigurationStoreOptions options)
        {
            _options = options;

            LoadClients();
            LoadApiAndIdentity();
        }

        void LoadClients()
        {
            var resourcesFolder = Path.Combine(_options.BaseFolder, "ClientStore");
            Directory.CreateDirectory(resourcesFolder);

            var clientExample = new Client
            {
            };
            var apiExampleFile = Path.Combine(resourcesFolder, "client.example");
            File.Delete(apiExampleFile);
            File.WriteAllText(apiExampleFile, Newtonsoft.Json.JsonConvert.SerializeObject(clientExample, Newtonsoft.Json.Formatting.Indented));


            Clients = Load<Client>(resourcesFolder).ToDictionary(r => r.ClientId);
        }

        void LoadApiAndIdentity()
        {
            var resourcesFolder = Path.Combine(_options.BaseFolder, "ResourceStore");
            var apiFolder = Path.Combine(resourcesFolder, "Api");
            Directory.CreateDirectory(apiFolder);
            var identityFolder = Path.Combine(resourcesFolder, "Identity");
            Directory.CreateDirectory(identityFolder);

            var _apiExample = new ApiResource
            {
                Description = "Description",
                DisplayName = "Display Name",
                Enabled = true,
                Name = "Name",
                UserClaims = new[] { "userClaim1" },
                Scopes = new[] { new Scope("name", "disp name", new[] { "claim1", }) },
                ApiSecrets = new[] { new Secret("value", "description", DateTime.Now) },
            };
            var apiExampleFile = Path.Combine(apiFolder, "api.example");
            File.Delete(apiExampleFile);
            File.WriteAllText(apiExampleFile, Newtonsoft.Json.JsonConvert.SerializeObject(_apiExample, Newtonsoft.Json.Formatting.Indented));


            var _identityExample = new IdentityResource
            {
                Description = "Description",
                DisplayName = "Display Name",
                Enabled = true,
                Name = "Name",
                UserClaims = new[] { "userClaim1" },
                Emphasize = true,
                Required = false,
                ShowInDiscoveryDocument = true,
            };
            var identityExampleFile = Path.Combine(identityFolder, "identity.example");
            File.Delete(identityExampleFile);
            File.WriteAllText(identityExampleFile, Newtonsoft.Json.JsonConvert.SerializeObject(_identityExample, Newtonsoft.Json.Formatting.Indented));

            Api = Load<ApiResource>(apiFolder).ToDictionary(r => r.Name);
            Identity = Load<IdentityResource>(identityFolder).ToDictionary(r => r.Name);
        }

        T[] Load<T>(string folder)
        {
            var result = Directory.EnumerateFiles(folder, "*.json").Select(file =>
            {
                var json = File.ReadAllText(file);
                return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
            }).ToArray();
            return result;
        }
    }

    public class ResourceStore : IResourceStore
    {
        InMemoryStore _store;
        ILogger<ClientStore> _logger;

        Dictionary<string, ApiResource> _api;
        Dictionary<string, IdentityResource> _identity;

        public ResourceStore(InMemoryStore store, ILogger<ClientStore> logger)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _logger = logger;

            _api = store.Api;
            _identity = store.Identity;
        }


        public Task<ApiResource> FindApiResourceAsync(string name)
        {
            _api.TryGetValue(name, out ApiResource r);
            return Task.FromResult(r);
        }

        public Task<IEnumerable<ApiResource>> FindApiResourcesByScopeAsync(IEnumerable<string> scopeNames)
        {
            var result = from api in _api.Values
                         where api.Scopes.Where(x => scopeNames.Contains(x.Name)).Any()
                         select api;
            return Task.FromResult(result);
        }

        public Task<IEnumerable<IdentityResource>> FindIdentityResourcesByScopeAsync(IEnumerable<string> scopeNames)
        {
            var result = _identity.Values.Where(i => scopeNames.Contains(i.Name));
            return Task.FromResult(result);
        }

        public Task<Resources> GetAllResourcesAsync()
        {
            var result = new Resources(_identity.Values, _api.Values);
            return Task.FromResult(result);
        }
    }


    public class CorsPolicyService : ICorsPolicyService
    {
        readonly IHttpContextAccessor _context;
        readonly ILogger<CorsPolicyService> _logger;

        public CorsPolicyService(IHttpContextAccessor context, ILogger<CorsPolicyService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger;
        }

        /// <summary>
        /// Determines whether origin is allowed.
        /// </summary>
        /// <param name="origin">The origin.</param>
        /// <returns></returns>
        public Task<bool> IsOriginAllowedAsync(string origin)
        {
            // doing this here and not in the ctor because: https://github.com/aspnet/CORS/issues/105
            //var dbContext = _context.HttpContext.RequestServices.GetRequiredService<IConfigurationDbContext>();

            //var origins = dbContext.Clients.SelectMany(x => x.AllowedCorsOrigins.Select(y => y.Origin)).ToList();

            //var distinctOrigins = origins.Where(x => x != null).Distinct();

            //var isAllowed = distinctOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase);

            //_logger.LogDebug("Origin {origin} is allowed: {originAllowed}", origin, isAllowed);

            //return Task.FromResult(isAllowed);
            throw new NotImplementedException();
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