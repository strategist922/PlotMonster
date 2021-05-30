using System.Threading;
using System.Threading.Tasks;

namespace PlotMonster.Business.Abstraction
{
    public interface IPlotDeterminationEngine
    {
        Task Process(CancellationToken cancellationToken);
    }
}