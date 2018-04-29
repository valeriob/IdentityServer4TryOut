using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnAuth.ConfigurationStore
{
    public class ResourceStore : IResourceStore
    {
        InMemoryCacheStore _store;
        ILogger<ClientStore> _logger;

        Dictionary<string, ApiResource> _api;
        Dictionary<string, IdentityResource> _identity;

        public ResourceStore(InMemoryCacheStore store, ILogger<ClientStore> logger)
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
}
