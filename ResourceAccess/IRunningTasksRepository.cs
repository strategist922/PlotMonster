using System.Threading;
using System.Threading.Tasks;

namespace chia_plotter
{
    public interface IRunningTasksRepository
    {
        Task AddTaskAsync(Task task, CancellationToken cancellationToken);
    }
}