using IdentityServer4.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace OnAuth.ConfigurationStore
{

    public class InMemoryCacheStore
    {
        ConfigurationStoreOptions _options;

        public Dictionary<string, ApiResource> Api { get; private set; }
        public Dictionary<string, IdentityResource> Identity { get; private set; }
        public Dictionary<string, Client> Clients { get; private set; }

        public InMemoryCacheStore(ConfigurationStoreOptions options)
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
}
