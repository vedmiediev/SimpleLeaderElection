using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeaderElection
{
    public interface ILocker
    {
        bool AcquireLock(string lockId, string keeperId);
        void ReleaseLock(string lockId, string keeperId);
    }
}
