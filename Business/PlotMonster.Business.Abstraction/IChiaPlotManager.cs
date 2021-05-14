using System.Threading;
using System.Threading.Tasks;

namespace chia_plotter.Business.Abstraction
{
    public interface IChiaPlotManager
    {
        Task ProcessAsync(CancellationToken cancellationToken);
    }
}