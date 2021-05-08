using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace chia_plotter.ResourceAccess.Infrastructure
{
    public class RunningTasksRepository : IRunningTasksRepository
    {
        private List<Task> repo = new List<Task>();

        public Task AddTaskAsync(Task task, CancellationToken cancellationToken)
        {
            this.repo.Add(task);
            return Task.CompletedTask;
        }
    }
}