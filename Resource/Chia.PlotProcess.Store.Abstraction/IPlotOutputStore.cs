using System;

namespace Chia.PlotProcess.Store.Abstraction
{
    public class IPlotOutputStore
    {
        Task AddPlotOutputAsync(IAsyncEnumerable<ChiaPlotOutput> chiaPlotOutput, CancellationToken cancellationToken);
        IAsyncEnumerable<ChiaPlotOutput> GetPlotOutputsAsync(CancellationToken cancellationToken);
    }
}
