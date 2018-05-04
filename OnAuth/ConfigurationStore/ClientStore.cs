using IdentityServer4.Models;
using IdentityServer4.Stores;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OnAuth.ConfigurationStore
{
    public class ClientStore : IClientStore
    {
        InMemoryCacheStore _store;
        ILogger<ClientStore> _logger;

        public ClientStore(InMemoryCacheStore store, ILogger<ClientStore> logger)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _logger = logger;
        }


        public Task<Client> FindClientByIdAsync(string clientId)
        {
            _store.Clients.TryGetValue(clientId, out Client result);
            return Task.FromResult(result);
        }
    }
}
