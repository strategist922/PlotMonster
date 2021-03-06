using System.Threading.Channels;
using System.Threading.Tasks;

namespace chia_plotter.ResourceAccess.Abstraction
{
    public interface IChiaPlotProcessChannel
    {
        Task<ChannelReader<string>> Get();
    }
    
}