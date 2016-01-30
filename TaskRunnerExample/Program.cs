using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LeaderElection;
using LeaderElection.Impl;

namespace TaskRunnerExample
{
    class Program
    {
        static void Main(string[] args)
        {
            string lockId = "task1";
            string connectionString = "mongodb://localhost/LeaderElectionTest";
            using (var locker = new MongoDbPessimisticLocker(connectionString))
            {
                var leaderAwareTaskRunner = new LeaderAwareTaskRunner(locker, lockId);

                var taskRunner = new TaskRunner(500, ()=>
                {
                    leaderAwareTaskRunner.RunTaskOnLeader(() =>
                    {
                        Console.WriteLine("task executed on a leader");
                    });
                });

                taskRunner.Start();
                Console.ReadLine();
                taskRunner.Stop();
            }
        }
    }

    public class TaskRunner 
    {
        private readonly Action actionToRun;
        Timer timer;
        TimeSpan interval;

        public TaskRunner(int interval, Action actionToRun)
        {
            this.actionToRun = actionToRun;
            this.interval = TimeSpan.FromMilliseconds(interval);
        }

        public void Start()
        {
            timer = new Timer(_ => actionToRun(),null ,TimeSpan.Zero, interval);
        }

        public void Stop()
        {
            if (timer != null)
            {
                timer.Dispose();
                timer = null;
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
