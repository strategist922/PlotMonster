using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using chia_plotter.Business.Abstraction;
using chia_plotter.ResourceAccess.Abstraction;
using chia_plotter.Utiltiy.Abstraction;

namespace chia_plotter.Business.Infrastructure
{
    public class ChiaPlotEngineContextConfiguration
    {
        public string TempDrive {get;set;}
        public string DestinationDrive {get;set;}
    }
    public class ChiaPlotEngine : IChiaPlotEngine
    {
        private readonly IChiaPlotProcessChannel repository;
        private readonly IRunningTasksRepository tasksRepository;
        private readonly ChiaPlotEngineContextConfiguration chiaPlotEngineContextConfiguration;
        private readonly Func<ChiaPlotOutput, string, bool> tryMapOutputDelegate;
        public ChiaPlotEngine(
            IChiaPlotProcessChannel repository,
            IRunningTasksRepository tasksRepository,
            ChiaPlotEngineContextConfiguration chiaPlotEngineContextConfiguration,
            Func<ChiaPlotOutput, string, bool> tryMapOutputDelegate
            )
        {
            this.repository = repository;
            this.tasksRepository = tasksRepository;
            this.chiaPlotEngineContextConfiguration = chiaPlotEngineContextConfiguration;
            this.tryMapOutputDelegate = tryMapOutputDelegate;
        }

        public async Task<Channel<ChiaPlotOutput>> ProcessAsync(CancellationToken cancellationToken)
        {
            var outputChannel = Channel.CreateUnbounded<ChiaPlotOutput>();
            await tasksRepository.AddTaskAsync(Task.Run(async () => {
                var channel = await repository.GetAsync(cancellationToken);
                var report = new ChiaPlotOutput();
                report.StartTime = DateTime.Now;
                report.TempDrive = chiaPlotEngineContextConfiguration.TempDrive;
                report.DestinationDrive = chiaPlotEngineContextConfiguration.DestinationDrive;
                await foreach(var line in channel.ReadAllAsync())
                {
                    // if (line.IndexOf("TEMPDRIVE:") > -1)
                    // {
                    //     report.TempDrive = line.Substring(10);
                    // }
                    // if (line.IndexOf("DESTDRIVE:") > -1)
                    // {
                    //     report.DestinationDrive = line.Substring(10);
                    // }
                    // TODO - delegate this work so we can have versioned delegates to automatically switch over based on chia version.  Need a resource access service to get the version.  Change the engine to set that delegate based on the version.
                    //     if we make the engine a factory where it injects in IEnumerable<IPlotOutputProcess<>>
                    // this is just an IMapper<string, ChiaPlotOutput>
                    // we create a mapper factory that return the mapper based on version (future enhancement)
                    // now we can bring in the stuff from the manager to wrap this mapper
                    if (line.IndexOf("ID:") > -1)
                    {
                        report.Id = line.Substring(4);
                        await outputChannel.Writer.WriteAsync(report);
                        break;
                    }
                }
                await foreach(var line in channel.ReadAllAsync())
                {
                    

                    if (tryMapOutputDelegate.Invoke(report, line))
                    {
                        await outputChannel.Writer.WriteAsync(report);
                    }
                }
            }, cancellationToken), cancellationToken);
            return outputChannel;
        }
    }
}