using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace chia_plotter.ResourceAccess.Abstraction
{
    public interface IProcessResourceAccess
    {
        Task CreateAsync(CancellationToken cancellationToken);
    }
    
}