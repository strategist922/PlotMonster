namespace PlotMonster.Access.Implementation
{
    public class PlotOutputAccess: IPlotOutputAccess
    {
        private readonly IPlotOutputStore plotOutputStore;
        private readonly Func<PlotMonster.Access.Abstraction.ChiaPlotOutput, bool> filter;

        public PlotOutputAccess(
            IPlotOutputStore plotOutputStore,
            Func<PlotMonster.Access.Abstraction.ChiaPlotOutput, bool> filter
            )
        {
            this.plotOutputStore = plotOutputStore;
            this.filter = filter;
        }

        public Task AddPlotOutput(IAsyncEnumerable<ChiaPlotOutput> chiaPlotOutput, CancellationToken cancellationToken)
        {
            await plotOutputStore.AddOutputAsync(chiaPlotOutput, cancellationToken);
        }

        public async Task<IAsyncEnumerable<ChiaPlotOutput>> GetPlotOutputs(CancellationToken cancellationToken)
        {
            var outputs = await plotOutputStore.GetPlotOutputs(cancellationToken);
            return outputs.Where(filter);
        }
    }
}