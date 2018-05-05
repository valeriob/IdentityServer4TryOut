using IdentityServer4.Models;
using IdentityServer4.Stores;
using LightningDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnAuth.PersistentGrants
{
    public class LMDBPersistedGrantStore : IPersistedGrantStore, IDisposable
    {
        LightningEnvironment _env;
        string _mainDb;
        string _indexBySubjectDb;


        public LMDBPersistedGrantStore()
        {
            _env = BuildEnvironment("Configuration");
            _mainDb = "main";
            _indexBySubjectDb = "indexBySubject";
        }


        public Task<IEnumerable<PersistedGrant>> GetAllAsync(string subjectId)
        {
            var result = new List<PersistedGrant>();

            using (var tx = _env.BeginTransaction())
            using (var mainDb = tx.OpenDatabase(_mainDb, new DatabaseConfiguration { Flags = DatabaseOpenFlags.Create }))
            using (var indexDb = tx.OpenDatabase(_indexBySubjectDb, new DatabaseConfiguration { Flags = DatabaseOpenFlags.Create }))
            {
                ProcessGrantsBySubjectId(subjectId, tx, mainDb, indexDb, cursor =>
                {
                    var current = cursor.GetCurrent();

                    var valueBytes = tx.Get(mainDb, current.Value);
                    var value = Deserialize(valueBytes);
                    result.Add(value);
                });
            }
            return Task.FromResult(result.AsEnumerable());
        }

        public Task<PersistedGrant> GetAsync(string key)
        {
            using (var tx = _env.BeginTransaction())
            using (var db = tx.OpenDatabase(_mainDb, new DatabaseConfiguration { Flags = DatabaseOpenFlags.Create }))
            {
                var keyBytes = Encoding.UTF8.GetBytes(key);

                var valueBytes = tx.Get(db, keyBytes);

                var value = Deserialize(valueBytes);
                return Task.FromResult(value);
            }
        }


        public Task RemoveAllAsync(string subjectId, string clientId)
        {
            Delete(subjectId, grant =>
            {
                return grant.ClientId == clientId;
            });
            return Task.CompletedTask;
        }

        public Task RemoveAllAsync(string subjectId, string clientId, string type)
        {
            Delete(subjectId, grant =>
            {
                return grant.ClientId == clientId && grant.Type == type;
            });
            return Task.CompletedTask;
        }


        public Task RemoveAsync(string key)
        {
            using (var tx = _env.BeginTransaction())
            using (var mainDb = tx.OpenDatabase(_mainDb, new DatabaseConfiguration { Flags = DatabaseOpenFlags.Create }))
            using (var indexDb = tx.OpenDatabase(_indexBySubjectDb, new DatabaseConfiguration { Flags = DatabaseOpenFlags.Create }))
            {
                var keyBytes = Encoding.UTF8.GetBytes(key);
                var valueBytes = tx.Get(mainDb, keyBytes);
                var value = Deserialize(valueBytes);

                tx.Delete(mainDb, keyBytes);

                using (var cursor = tx.CreateCursor(indexDb))
                {
                    if (cursor.MoveTo(keyBytes, Encoding.UTF8.GetBytes(value.SubjectId)))
                        cursor.Delete();
                }

                tx.Commit();
            }
            return Task.CompletedTask;
        }


        public Task StoreAsync(PersistedGrant grant)
        {
            using (var tx = _env.BeginTransaction())
            using (var mainDb = tx.OpenDatabase(_mainDb, new DatabaseConfiguration { Flags = DatabaseOpenFlags.Create }))
            using (var indexDb = tx.OpenDatabase(_indexBySubjectDb, new DatabaseConfiguration { Flags = DatabaseOpenFlags.Create | DatabaseOpenFlags.DuplicatesFixed }))
            {
                var key = Encoding.UTF8.GetBytes(grant.Key);
                var value = Serialize(grant);

                tx.Put(mainDb, key, value);

                var subjectIdBytes = Encoding.UTF8.GetBytes(grant.SubjectId);

                using (var cur = tx.CreateCursor(indexDb))
                    cur.PutMultiple(subjectIdBytes, new[] { key });

                tx.Commit();
            }

            return Task.CompletedTask;
        }


        void Delete(string subjectId, Predicate<PersistedGrant> predicate)
        {
            using (var tx = _env.BeginTransaction())
            using (var mainDb = tx.OpenDatabase(_mainDb, new DatabaseConfiguration { Flags = DatabaseOpenFlags.Create }))
            using (var indexDb = tx.OpenDatabase(_indexBySubjectDb, new DatabaseConfiguration { Flags = DatabaseOpenFlags.Create }))
            {
                ProcessGrantsBySubjectId(subjectId, tx, mainDb, indexDb, cursor =>
                {
                    var current = cursor.GetCurrent();

                    var valueBytes = tx.Get(mainDb, current.Value);
                    var value = Deserialize(valueBytes);
                    if (predicate(value))
                    {
                        cursor.Delete();
                        tx.Delete(mainDb, current.Value);
                    }
                });

                tx.Commit();
            }
        }


        void ProcessGrantsBySubjectId(string subjectId, LightningTransaction tx, LightningDatabase mainDb, LightningDatabase indexDb, Action<LightningCursor> callback)
        {
            var subjectIdBytes = Encoding.UTF8.GetBytes(subjectId);
            using (var cursor = tx.CreateCursor(indexDb))
            {
                if (cursor.MoveTo(subjectIdBytes))
                {
                    do
                    {
                        callback(cursor);
                    } while (cursor.MoveNextDuplicate());
                }
            }
        }

        byte[] Serialize(PersistedGrant grant)
        {
            return MessagePack.LZ4MessagePackSerializer.Typeless.Serialize(grant);
        }

        PersistedGrant Deserialize(byte[] bytes)
        {
            return MessagePack.LZ4MessagePackSerializer.Typeless.Deserialize(bytes) as PersistedGrant;
        }

        static LightningEnvironment BuildEnvironment(string path)
        {
            var env = new LightningEnvironment(path);
            env.MaxDatabases = 2;
            env.Open();
            return env;
        }

        public void Dispose()
        {
            if (_env != null)
            {
                _env.Dispose();
                _env = null;
            }
        }
    }
}
