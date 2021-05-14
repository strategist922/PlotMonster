using System.Threading;
using System.Threading.Tasks;

namespace PlotMonster.Business.Abstraction
{
    public interface IChiaPlotManager
    {
        Task ProcessAsync(CancellationToken cancellationToken);
    }
}