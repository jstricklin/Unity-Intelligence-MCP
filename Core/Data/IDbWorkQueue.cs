using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace UnityIntelligenceMCP.Core.Data
{
    public interface IDbWorkQueue
    {
        ValueTask EnqueueAsync(IDbWorkItem workItem);
        IAsyncEnumerable<IDbWorkItem> DequeueAllAsync(CancellationToken cancellationToken);
    }
}
