using System.Threading;
using System.Threading.Tasks;

namespace PlotMonster.Access.Abstraction
{
    public interface IPlotProcessAccess
    {
        Task AddPlotProcess(PlotProcessMetadata metadata, CancellationToken cancellationToken);
        Task<IAsyncEnumerable<List<ChiaPlotOutput>>> GetPlotProcesses(CancellationToken cancellationToken);
    }
}