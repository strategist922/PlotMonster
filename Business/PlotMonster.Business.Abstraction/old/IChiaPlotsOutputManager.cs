using System.Collections.Generic;
using System.Threading;

namespace PlotMonster.Business.Abstraction
{
    public interface IChiaPlotsOutputManager
    {
        IAsyncEnumerable<ICollection<ChiaPlotOutput>> Process(CancellationToken cancellationToken);
    }
}