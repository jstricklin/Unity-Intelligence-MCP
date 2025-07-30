using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace UnityIntelligenceMCP.Core.Data
{
    public interface IDbWorkQueue
    {
        ChannelWriter<IDbWorkItem> Writer { get; }
        ChannelReader<IDbWorkItem> Reader { get; }
        ValueTask EnqueueAsync(IDbWorkItem workItem);
        IAsyncEnumerable<IDbWorkItem> DequeueAllAsync(CancellationToken cancellationToken);
    }
}
