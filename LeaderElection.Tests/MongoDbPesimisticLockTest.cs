using System;
using System.Configuration;
using System.Threading;
using LeaderElection.Impl;
using NUnit.Framework;

namespace LeaderElection.Tests
{
    [TestFixture]
    public class MongoDbPesimisticLockTest
    {
        private readonly string connectionString = "mongodb://localhost/LeaderElectionTest";

        [Test]
        public void TestAcquireLock_SameId()
        {
            using (MongoDbPessimisticLocker locker = new MongoDbPessimisticLocker(connectionString))
            {
                string id = "id-to-lock";
                string keeperId1 = "job1";
                string keeperId2 = "job2";

                Assert.True(locker.AcquireLock(id, keeperId1));
                Assert.False(locker.AcquireLock(id, keeperId2));
            }
        }

        [Test]
        public void TestAcquireLock_DifferentId()
        {
            using (MongoDbPessimisticLocker locker = new MongoDbPessimisticLocker(connectionString))
            {
                string id1 = "id-to-lock1";
                string id2 = "id-to-lock2";
                string keeperId1 = "job1";
                string keeperId2 = "job2";

                Assert.True(locker.AcquireLock(id1, keeperId1));
                Assert.True(locker.AcquireLock(id2, keeperId2));
                Assert.False(locker.AcquireLock(id1, keeperId2));
                Assert.False(locker.AcquireLock(id2, keeperId1));
            }
        }

        [Test]
        public void TestAcquireLock_SameId_Timeout_LockReleased_By_MongoDB()
        {
            int ttl = 1;
            using (MongoDbPessimisticLocker locker = new MongoDbPessimisticLocker(connectionString, ttl, true))
            {
                string id = "id-to-lock";
                string keeperId1 = "job1";
                string keeperId2 = "job2";

                Assert.True(locker.AcquireLock(id, keeperId1));
                Thread.Sleep(TimeSpan.FromSeconds(ttl + 60)); // give mongodb a little bit time to remove lock
                Assert.True(locker.AcquireLock(id, keeperId2));
                Assert.False(locker.AcquireLock(id, keeperId1));
            }
        }

        [Test]
        public void TestAcquireLock_SameId_Timeout_KeepAllive()
        {
            int ttl = 1;
            using (MongoDbPessimisticLocker locker = new MongoDbPessimisticLocker(connectionString, ttl, true))
            {

                string id = "id-to-lock";
                string keeperId1 = "job1";
                string keeperId2 = "job2";

                Assert.True(locker.AcquireLock(id, keeperId1));
                for (int i = 0; i < 59; i++)
                {
                    Thread.Sleep(1000);
                    Assert.True(locker.AcquireLock(id, keeperId1));
                }
                Assert.False(locker.AcquireLock(id, keeperId2));
                Assert.True(locker.AcquireLock(id, keeperId1));
            }
        }
    }
}