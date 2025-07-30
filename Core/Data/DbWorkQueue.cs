using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace UnityIntelligenceMCP.Core.Data
{
    public class DbWorkQueue : IDbWorkQueue
    {
        private readonly Channel<IDbWorkItem> _queue;

        public ChannelWriter<IDbWorkItem> Writer => _queue.Writer;
        public ChannelReader<IDbWorkItem> Reader => _queue.Reader;

        public DbWorkQueue()
        {
            var options = new UnboundedChannelOptions { SingleReader = true };
            _queue = Channel.CreateUnbounded<IDbWorkItem>(options);
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
