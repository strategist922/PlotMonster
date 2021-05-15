using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PlotMonster.ResourceAccess.Abstraction
{
    public interface IProcessResourceAccess
    {
        Task<IAsyncEnumerable<ChiaPlotOutput>> CreateAsync(PlotProcessMetadata plotProcessMetadata, CancellationToken cancellationToken);
    }
    
}