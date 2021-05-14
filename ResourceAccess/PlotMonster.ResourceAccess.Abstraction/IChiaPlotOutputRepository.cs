using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PlotMonster.ResourceAccess.Abstraction
{
    public interface IChiaPlotOutputRepository
    {
        Task AddProcessAsync(ChiaPlotProcess chiaPlotProcess, CancellationToken cancellationToken);
        IAsyncEnumerable<ICollection<ChiaPlotProcess>> GetRunningProcesses(CancellationToken cancellationToken);
    }
}