using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PlotMonster.ResourceAccess.Abstraction;

namespace PlotMonster.Business.Abstraction
{
    public interface IPlotSizeDeterminationEngine
    {
        Task<AvailablePlotResource> DeterminePlotSizeAsync(string tempDrive, IEnumerable<ChiaPlotOutput> outputs, CancellationToken cancellationToken);
    }
}