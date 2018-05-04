using IdentityServer4.Models;
using Newtonsoft.Json;
using OnAuth.PersistentGrants;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace OnAuth.Test
{
    public class LMDBPersistedGrantStore_should
    {
        [Fact]
        public async Task serialize_Client()
        {
            var store = new LMDBPersistedGrantStore();

            string key1 = "key1";
            string key2 = "key2";
            string sub = "subject1";

            var grant1 = new PersistedGrant
            {
                Key = key1,
                SubjectId = sub,
            };

            var grant2 = new PersistedGrant
            {
                Key = key2,
                SubjectId = sub,
            };

            await store.StoreAsync(grant1);
            await store.StoreAsync(grant2);

            var grant1R = await store.GetAsync(key1);
            var grant2R = await store.GetAsync(key2);

            AssertEquals(grant1, grant1R);
            AssertEquals(grant2, grant2R);

            var grants = await store.GetAllAsync(sub);

            Assert.Collection(grants, g => AssertEquals(grant1, g), g => AssertEquals(grant2, g));
        }

        void AssertEquals(PersistedGrant a, PersistedGrant b)
        {
            var p = a.Key == b.Key && a.SubjectId == b.SubjectId;
            if (!p)
                throw new Exception("grants not equal");
        }

    }
}
