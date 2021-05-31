using System.Threading;
using System.Threading.Tasks;

namespace PlotMonster.Access.Abstraction
{
    public interface IPlotProcessAccess
    {
        Task<IAsyncEnumerable<string>> AddPlotProcess(PlotProcessMetadata metadata, CancellationToken cancellationToken);
    }
}