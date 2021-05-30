using System.Threading;
using System.Threading.Tasks;

namespace PlotMonster.Business.Abstraction
{
    public interface IPlotEngine
    {
        Task Process(CancellationToken cancellationToken);
    }
}