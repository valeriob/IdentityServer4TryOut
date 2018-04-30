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

        string _basePath;

        public InMemoryCacheStore(ConfigurationStoreOptions options)
        {
            _options = options;
            _basePath = Path.Combine(options.BaseFolder, "Configuration");
            LoadClients();
            LoadApiAndIdentity();
        }

        public void Seed(params ApiResource[] apiResources)
        {
            foreach (var resource in apiResources)
                Store(resource);
        }

        public void Seed(params IdentityResource[] identityResources)
        {
            foreach (var resource in identityResources)
                Store(resource);
        }

        public void Seed(params Client[] clients)
        {
            foreach (var resource in clients)
                Store(resource);
        }

        void LoadClients()
        {
            var resourcesFolder = Path.Combine(_basePath, "Client");
            Directory.CreateDirectory(resourcesFolder);

            Store(new Client { ClientId = "client.example" });

            Clients = Load<Client>(resourcesFolder).ToDictionary(r => r.ClientId);
        }

        void LoadApiAndIdentity()
        {
            var apiFolder = Path.Combine(_basePath, "Api");
            Directory.CreateDirectory(apiFolder);
            var identityFolder = Path.Combine(_basePath, "Identity");
            Directory.CreateDirectory(identityFolder);

            var apiExample = new ApiResource
            {
                Description = "Description",
                DisplayName = "Display Name",
                Enabled = true,
                Name = "api.example",
                UserClaims = new[] { "userClaim1" },
                Scopes = new[] { new Scope("name", "disp name", new[] { "claim1", }) },
                ApiSecrets = new[] { new Secret("value", "description", DateTime.Now) },
            };
            Store(apiExample);

            var identityExample = new IdentityResource
            {
                Description = "Description",
                DisplayName = "Display Name",
                Enabled = true,
                Name = "identity.example",
                UserClaims = new[] { "userClaim1" },
                Emphasize = true,
                Required = false,
                ShowInDiscoveryDocument = true,
            };
            Store(identityExample);

            Api = Load<ApiResource>(apiFolder).ToDictionary(r => r.Name);
            Identity = Load<IdentityResource>(identityFolder).ToDictionary(r => r.Name);
        }

        void Store(ApiResource apiResource)
        {
            var file = Path.Combine(_basePath, "Api", apiResource.Name + ".json");
            Store(apiResource, file);
        }

        void Store(IdentityResource apiResource)
        {
            var file = Path.Combine(_basePath, "Identity", apiResource.Name + ".json");
            Store(apiResource, file);
        }

        void Store(Client client)
        {
            var file = Path.Combine(_basePath, "Client", client.ClientId + ".json");
            Store(client, file);
        }

        void Store(object item, string filePath)
        {
            File.Delete(filePath);
            File.WriteAllText(filePath, Newtonsoft.Json.JsonConvert.SerializeObject(item, Newtonsoft.Json.Formatting.Indented));
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
