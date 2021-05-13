using System;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using chia_plotter.ResourceAccess.Abstraction;

namespace chia_plotter.ResourceAccess.Infrastructure
{
    public class ChiaPlotProcessChannelMock: IChiaPlotProcessChannel
    {
        private readonly string tempDrive;
        private readonly string destDrive;
        private readonly string kSize;
        private readonly string ram;
        private readonly string threads;
        private readonly ChiaPlotProcessRepository processRepo;
        private readonly IRunningTasksRepository runningTasksRepository;

        public ChiaPlotProcessChannelMock(
            string tempDrive, 
            string destDrive, 
            string kSize, 
            string ram, 
            string threads, 
            ChiaPlotProcessRepository processRepo,
            IRunningTasksRepository runningTasksRepository)
        {
            this.tempDrive = tempDrive;
            this.destDrive = destDrive;
            this.kSize = kSize;
            this.ram = ram;
            this.threads = threads;
            this.processRepo = processRepo;
            this.runningTasksRepository = runningTasksRepository;
        }

        public async Task<ChannelReader<string>> GetAsync(CancellationToken cancellationToken)
        {
            var channel = Channel.CreateUnbounded<string>();
            
            await runningTasksRepository.AddTaskAsync(Task.Run(async () =>
            {
                var random = new Random();
                var id = "abcd" + random.Next(1000000000, int.MaxValue);
                
                await channel.Writer.WriteAsync($"TEMPDRIVE:{tempDrive}");
                await channel.Writer.WriteAsync($"DESTDRIVE:{destDrive}");

                var file = Path.Combine(Directory.GetCurrentDirectory(), "ResourceAccess", "mock_plot_process.txt");
              
                using (StreamReader reader = File.OpenText(file))
                {
                    string line;
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        line = string.Format(line, id);
                        await channel.Writer.WriteAsync(line);
                        Thread.Sleep(25);
                    }
                }
            }, cancellationToken), cancellationToken);
            return channel;
        }

    }
}