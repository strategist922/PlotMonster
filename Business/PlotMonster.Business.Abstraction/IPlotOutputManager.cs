using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PlotMonster.Business.Abstraction
{
    public interface IPlotOutputManager
    {
        Task<IAsyncEnumerable<ChiaPlotOutput>> Process(CancellationToken cancellationToken);
    }
}