using System;

namespace Chia.PlotProcess.Store.Implementation
{
    public class PlotOutputStore: IPlotOutputStore
    {
        private readonly Channel<ChiaPlotOutput> outputChannel;
        private readonly ICollection<ICollection<ChiaPlotOutput>> chiaPlotCollection;

        public PlotOutputStore()
        {
            this.chiaPlotCollection = new List<ICollection<ChiaPlotOutput>>();
        }

        public Task AddPlotOutputAsync(IAsyncEnumerable<ChiaPlotOutput> chiaPlotOutputStream, CancellationToken cancellationToken)
        {
            var outputCollection = new List<ChiaPlotOutput>();
            chiaPlotCollection.Add(outputCollection);
            var task = Task.RunAsync(async () => {
                await foreach(var chiaPlotOutput in chiaPlotOutputStream)
                {
                    outputCollection.Add(chiaPlotOutput);
                    await outputChannel.WriteAsync(chiaPlotOutput);
                }
            }, cancellationToken);
            return task;
        }

        public Task<IAsyncEnumerable<ChiaPlotOutput>> GetPlotOutputsAsync(CancellationToken cancellationToken)
        {
            foreach(var chiaPlots in chiaPlotCollection)
            {
                // how do i send the last of each collection and then tie into the outputChannel
                yield return chiaPlotCollection.Last();
            }
            return outputChannel;
        }
    }
}
