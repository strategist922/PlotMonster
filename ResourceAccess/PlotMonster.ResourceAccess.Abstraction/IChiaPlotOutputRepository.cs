using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PlotMonster.ResourceAccess.Abstraction
{
    public interface IChiaPlotOutputRepository
    {
        Task AddProcessAsync(ChiaPlotOutput chiaPlotProcess, CancellationToken cancellationToken);
        IAsyncEnumerable<IEnumerable<ChiaPlotOutput>> GetProcessesAsync(CancellationToken cancellationToken);
    }
}