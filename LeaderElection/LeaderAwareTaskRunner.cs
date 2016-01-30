using System;
using System.Diagnostics;

namespace LeaderElection
{
    public class LeaderAwareTaskRunner
    {
        private readonly ILocker locker;
        private readonly string lockId;
        private readonly int lockPingTimeoutInSeconds;
        private bool isALeader;
        private Stopwatch watch = new Stopwatch();
        private readonly string lockKeeperId;

        private bool IsStillALeader()
        {
            if (!isALeader || watch.ElapsedMilliseconds > lockPingTimeoutInSeconds * 1000) // try to acuire lock if not a leader or after 30 seconds of became a leader
            {
                isALeader = locker.AcquireLock(lockId, lockKeeperId);
                if (isALeader) watch.Restart();
            }
            return isALeader;
        }

        public static string GenerateLockKeeperId()
        {
            var process = Process.GetCurrentProcess();
            return string.Format("{2}-LeaderAwareTaskRunner-PID-{0}-Domain-{1}", process.Id, AppDomain.CurrentDomain.FriendlyName,
                process.MachineName);
        }

        public LeaderAwareTaskRunner(ILocker locker, string lockId, int lockPingTimeoutInSeconds = 30)
        {
            this.locker = locker;
            this.lockId = lockId;
            this.lockPingTimeoutInSeconds = lockPingTimeoutInSeconds;
            lockKeeperId = GenerateLockKeeperId();
        }

        public void RunTaskOnLeader(Action task)
        {
            if (IsStillALeader())
                task();
        }
    }
}