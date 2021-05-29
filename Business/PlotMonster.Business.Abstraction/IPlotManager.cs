using System.Threading;
using System.Threading.Tasks;

namespace PlotMonster.Business.Abstraction
{
    public interface IPlotManager
    {
        Task Process(CancellationToken cancellationToken);
    }
}