using System;

namespace Chia.PlotProcess.Store.Abstraction
{
    public class IPlotStore
    {
        Task AddPlotProcessAsync(PlotMetadata plotMetadata, CancellationToken cancellationToken);
        IAsyncEnumerable<PlotProcess> GetPlotsProcesses(CancellationToken cancellationToken);
    }
}
