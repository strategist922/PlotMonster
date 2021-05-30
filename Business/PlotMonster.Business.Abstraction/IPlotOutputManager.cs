using System.Threading;
using System.Threading.Tasks;

namespace PlotMonster.Business.Abstraction
{
    public interface IPlotOutputManager
    {
        Task Process(CancellationToken cancellationToken);
    }
}