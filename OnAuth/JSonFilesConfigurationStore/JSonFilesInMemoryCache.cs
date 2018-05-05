using IdentityServer4.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Reactive.Linq;
using System.Text;
using System.Threading;

namespace OnAuth.JSonFilesConfigurationStore
{
    public class JSonFilesInMemoryCache
    {
        ConfigurationStoreOptions _options;

        public Dictionary<string, ApiResource> Api { get; private set; }
        public Dictionary<string, IdentityResource> Identity { get; private set; }
        public Dictionary<string, Client> Clients { get; private set; }

        string _basePath;
        string _clientsPath;
        string _apiPath;
        string _identityPath;

        FileSystemWatcher _fsw;
        Subject<int> _changes;

        public JSonFilesInMemoryCache(ConfigurationStoreOptions options)
        {
            _options = options;
            _basePath = Path.Combine(options.BaseFolder, "Configuration");
            _clientsPath = Path.Combine(_basePath, "Client");
            _apiPath = Path.Combine(_basePath, "Api");
            _identityPath = Path.Combine(_basePath, "Identity");

            SeedSamples();

            LoadAll();

            _changes = new Subject<int>();
            _changes.Throttle(TimeSpan.FromSeconds(1)).Subscribe(_ =>
            {
                LoadAll();
            });

            _fsw = new FileSystemWatcher(_basePath, "*.json");
            _fsw.IncludeSubdirectories = true;
            _fsw.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime;
            _fsw.Changed += FileChanged;
            _fsw.Created += FileChanged;
            _fsw.Deleted += FileChanged;
            _fsw.EnableRaisingEvents = true;
        }

        void FileChanged(object sender, FileSystemEventArgs e)
        {
            _changes.OnNext(1);
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

        void SeedSamples()
        {
            Directory.CreateDirectory(_clientsPath);

            Store(new Client { ClientId = "client.example" });


            Directory.CreateDirectory(_apiPath);
            Directory.CreateDirectory(_identityPath);

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
        }

        void LoadAll()
        {
            Clients = Load<Client>(_clientsPath).ToDictionary(r => r.ClientId);
            Api = Load<ApiResource>(_apiPath).ToDictionary(r => r.Name);
            Identity = Load<IdentityResource>(_identityPath).ToDictionary(r => r.Name);
        }


        void Store(ApiResource apiResource)
        {
            var file = Path.Combine(_apiPath, apiResource.Name + ".json");
            Store(apiResource, file);
        }

        void Store(IdentityResource apiResource)
        {
            var file = Path.Combine(_identityPath, apiResource.Name + ".json");
            Store(apiResource, file);
        }

        void Store(Client client)
        {
            var file = Path.Combine(_clientsPath, client.ClientId + ".json");
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

        public void Refresh()
        {
            LoadAll();
        }

    }
}
