using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnAuth.JSonFilesConfigurationStore
{
    public class JSonFilesResourceStore : IResourceStore
    {
        JSonFilesInMemoryCache _store;
        ILogger<JSonFilesClientStore> _logger;


        public JSonFilesResourceStore(JSonFilesInMemoryCache store, ILogger<JSonFilesClientStore> logger)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _logger = logger;
        }


        public Task<ApiResource> FindApiResourceAsync(string name)
        {
            _store.Api.TryGetValue(name, out ApiResource r);
            return Task.FromResult(r);
        }

        public Task<IEnumerable<ApiResource>> FindApiResourcesByScopeAsync(IEnumerable<string> scopeNames)
        {
            var result = from api in _store.Api.Values
                         where api.Scopes.Where(x => scopeNames.Contains(x.Name)).Any()
                         select api;
            return Task.FromResult(result);
        }

        public Task<IEnumerable<IdentityResource>> FindIdentityResourcesByScopeAsync(IEnumerable<string> scopeNames)
        {
            var result = _store.Identity.Values.Where(i => scopeNames.Contains(i.Name));
            return Task.FromResult(result);
        }

        public Task<Resources> GetAllResourcesAsync()
        {
            var result = new Resources(_store.Identity.Values, _store.Api.Values);
            return Task.FromResult(result);
        }
    }
}
