using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PlotMonster.Access.Abstraction
{
    public interface IPlotOutputAccess
    {
        Task AddPlotOutput(ChiaPlotOutput chiaPlotOutput, CancellationToken cancellationToken);
        Task<IAsyncEnumerable<ChiaPlotOutput>> GetPlotOutputs(CancellationToken cancellationToken);
    }
}