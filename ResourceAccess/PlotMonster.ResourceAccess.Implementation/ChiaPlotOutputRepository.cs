namespace PlotMonster.ResourceAccess.Implementation
{
    // singleton
    public class ChiaPlotOutputRepository : IChiaPlotOutputRepository
    {
        private Dictionary<string, ChiaPlotOutput> outputs {get;set;}
        private readonly Channel channel;
        public ChiaPlotOutputRepository()
        {
            this.channel = Channel.CreateUnbounded();
        }

        public async Task AddProcessAsync(ChiaPlotOutput chiaPlotOutput, CancellationToken cancellationToken)
        {
            outputs[chiaPlotOutput.Id] = chiaPlotOutput;
            await channel.WriteAsync(await outputs.Values.Where(p => p.IsPlotComplete == false).ToListAsync(cancellationToken));
        }

        public IAsyncEnumerable<ICollection<ChiaPlotOutput>> GetRunningProcesses(CancellationToken cancellationToken)
        {
            return channel.ChannelReader;
        }
    }
}