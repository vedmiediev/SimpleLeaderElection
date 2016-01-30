using System;
using System.Collections.Generic;
using System.Linq;

namespace LeaderElection.Tests
{
    public class LockerMock : ILocker
    {
        private readonly string id;
        private readonly string keeperId;

        public LockerMock(bool isLeader, string id, string keeperId)
        {
            this.id = id;
            this.keeperId = keeperId;
            IsLeader = isLeader;
        }

        public bool IsLeader { get; set; }

        public bool AcquireLock(string id, string keeperId)
        {
            return IsLeader && id == this.id && keeperId == this.keeperId;
        }

        public void ReleaseLock(string id, string keeperId)
        {

        }
    }
}