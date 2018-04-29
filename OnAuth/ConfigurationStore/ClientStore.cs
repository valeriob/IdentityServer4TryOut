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
        Dictionary<string, Client> _clients;

        public ClientStore(InMemoryCacheStore store, ILogger<ClientStore> logger)
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
}
