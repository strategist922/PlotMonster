using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using PlotMonster.ResourceAccess.Abstraction;

namespace PlotMonster.ResourceAccess.Implementation
{
    // singleton
    public class ChiaPlotOutputRepository : IChiaPlotOutputRepository
    {
        private IDictionary<string, ChiaPlotOutput> outputs {get;set;}
        private readonly Channel<IEnumerable<ChiaPlotOutput>> channel;
        public ChiaPlotOutputRepository()
        {
            this.channel = Channel.CreateBounded<IEnumerable<ChiaPlotOutput>>(new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropOldest });
        }

        public async Task AddProcessAsync(ChiaPlotOutput chiaPlotOutput, CancellationToken cancellationToken)
        {
            outputs[chiaPlotOutput.Id] = chiaPlotOutput;
            await channel.Writer.WriteAsync(outputs.Values.Where(p => p.IsPlotComplete == false));
        }

        public IAsyncEnumerable<IEnumerable<ChiaPlotOutput>> GetProcessesAsync(CancellationToken cancellationToken)
        {
            return channel.Reader.ReadAllAsync(cancellationToken);
        }
    }
}