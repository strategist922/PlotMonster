using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace chia_plotter.Business.Abstraction
{
    public interface IChiaPlotEngine
    {
        Task<Channel<ChiaPlotOutput>> ProcessAsync(CancellationToken cancellationToken);
    }
}