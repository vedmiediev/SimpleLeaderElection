using System.Threading;
using NUnit.Framework;

namespace LeaderElection.Tests
{
    [TestFixture]
    public class LeadetAwareTaskRunnerTest
    {
        [Test]
        public void TestRubTaskInNotLeaderState()
        {
            string lockId = "lockId";

            // ReSharper disable once RedundantArgumentDefaultValue
            // ReSharper disable once RedundantArgumentNameForLiteralExpression
            var lockerMock = new LockerMock(isLeader: false, id: lockId, keeperId: LeaderAwareTaskRunner.GenerateLockKeeperId());
            LeaderAwareTaskRunner runner = new LeaderAwareTaskRunner(lockerMock, lockId);

            runner.RunTaskOnLeader(() => Assert.True(false));
        }

        [Test]
        public void TestRubTaskInLeaderState()
        {
            string lockId = "lockId";

            int i = 0;
            // ReSharper disable once RedundantArgumentDefaultValue
            // ReSharper disable once RedundantArgumentNameForLiteralExpression
            var lockerMock = new LockerMock(isLeader: true, id: lockId, keeperId: LeaderAwareTaskRunner.GenerateLockKeeperId());
            LeaderAwareTaskRunner runner = new LeaderAwareTaskRunner(lockerMock, lockId);

            runner.RunTaskOnLeader(() => i++ );

            Assert.AreEqual(1, i);

        }

        [Test]
        public void TestBecameALeader()
        {
            string lockId = "lockId";

            int i = 0;
            // ReSharper disable once RedundantArgumentDefaultValue
            // ReSharper disable once RedundantArgumentNameForLiteralExpression
            var lockerMock = new LockerMock(isLeader: true, id: lockId, keeperId: LeaderAwareTaskRunner.GenerateLockKeeperId());
            LeaderAwareTaskRunner runner = new LeaderAwareTaskRunner(lockerMock, lockId, 0);
            runner.RunTaskOnLeader(() => i++);
            Thread.Sleep(200);
            lockerMock.IsLeader = false;
            runner.RunTaskOnLeader(() => i++);
            Assert.AreEqual(1, i);
            Thread.Sleep(200);
            lockerMock.IsLeader = true;
            runner.RunTaskOnLeader(() => i++);
            Assert.AreEqual(2, i);
        }
    }
}