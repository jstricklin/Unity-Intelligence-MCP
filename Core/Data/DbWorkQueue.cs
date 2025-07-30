using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace UnityIntelligenceMCP.Core.Data
{
    public class DbWorkQueue : IDbWorkQueue
    {
        private readonly Channel<IDbWorkItem> _queue;

        public DbWorkQueue()
        {
            // Use an unbounded channel to hold all work items.
            _queue = Channel.CreateUnbounded<IDbWorkItem>();
        }

        public async ValueTask EnqueueAsync(IDbWorkItem workItem)
        {
            await _queue.Writer.WriteAsync(workItem);
        }

        public IAsyncEnumerable<IDbWorkItem> DequeueAllAsync(CancellationToken cancellationToken)
        {
            return _queue.Reader.ReadAllAsync(cancellationToken);
        }
    }
}
