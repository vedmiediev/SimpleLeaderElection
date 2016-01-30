using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Builders;

namespace LeaderElection.Impl
{
    internal class PessimisticLockDto
    {
        public string Id { get; set; } 
        [BsonElement("ts")]
        public DateTime TimeStamp { get; set; } //Timestamp of the lock
        [BsonElement("kid")]
        public string KeeperId { get; set; } //lock Keeper Id
    }

    public class MongoDbPessimisticLocker : ILocker, IDisposable
    {

        private static MongoDatabase GetDatabase(string connectionString)
        {
            var client = new MongoClient(connectionString);
            var database = client.GetServer().GetDatabase(new MongoUrl(connectionString).DatabaseName);
            return database;
        }

        private readonly MongoCollection<PessimisticLockDto> locks;
        private HashSet<string> knownLockKeepers = new HashSet<string>();

        public MongoDbPessimisticLocker(string connectionString, int ttl = 90, bool dropCollection = false)
            : this(GetDatabase(connectionString), ttl, dropCollection)
        {

        }

        internal MongoDbPessimisticLocker(MongoDatabase database, int ttl, bool dropCollection)
        {
            locks = database.GetCollection<PessimisticLockDto>("PessimisticLocks");
            if (dropCollection)
                locks.Drop();

            var indexes = locks.GetIndexes().ToArray();
            var tsIndex = indexes.FirstOrDefault(x => x.Name != "ts");
            if (tsIndex == null)
            {
                locks.CreateIndex(new IndexKeysBuilder().Ascending("ts"),
                    new IndexOptionsBuilder().SetTimeToLive(TimeSpan.FromSeconds(ttl)).SetName("ts"));
            }
            locks.CreateIndex(new IndexKeysBuilder().Ascending("kid"), new IndexOptionsBuilder().SetUnique(false));
        }

        public bool AcquireLock(string lockId, string keeperId)
        {
            try
            {
                knownLockKeepers.Add(keeperId);
                //try to update or insert lock document with current timestamp.
                //1. if lock is acquired document will be inserted or updated with latest timestamp
                //2. if lock is acquired by other keeper then query by id and keeper id will return 0 documents and when mongo will try to upsert new document MongoDublicateKeyException will be rised
                return locks.Update(Query<PessimisticLockDto>.Where(x => x.Id == lockId && x.KeeperId == keeperId),
                    Update<PessimisticLockDto>.CurrentDate(x => x.TimeStamp), UpdateFlags.Upsert, WriteConcern.WMajority).DocumentsAffected == 1;
            }
            catch (Exception)
            {
                return false;
            }

        }

        public void ReleaseLock(string lockId, string keeperId)
        {
            locks.Remove(Query<PessimisticLockDto>.Where(x => x.Id == lockId && x.KeeperId == keeperId), WriteConcern.WMajority);
        }

        public void ReleaseAllLocks()
        {
            locks.Remove(Query<PessimisticLockDto>.In(x => x.KeeperId, knownLockKeepers.ToArray()), WriteConcern.WMajority);
        }

        #region Disposing

        private bool disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Manual release of managed resources.
                }
                ReleaseAllLocks();
                disposed = true;
            }
        }

        ~MongoDbPessimisticLocker() { Dispose(false); }

        #endregion
    }
}