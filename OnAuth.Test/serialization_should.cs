using IdentityServer4.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace OnAuth.Test
{
    public class serialization_should
    {
        [Fact]
        public void serialize_Client()
        {
            var c = new Client
            {

            };
            var json = JsonConvert.SerializeObject(c);
            var c2 = JsonConvert.DeserializeObject<Client>(json);

        }

        [Fact]
        public void serialize_Resource()
        {
            var api = new ApiResource()
            {
                ApiSecrets = new[] { new Secret("s1") },
                Description = "desc",
                DisplayName = "dn",
                Enabled = true,
                UserClaims = new[] { "c1" }
            };
            var json = JsonConvert.SerializeObject(api);
            var api2 = JsonConvert.DeserializeObject<ApiResource>(json);

        }
    }
}
