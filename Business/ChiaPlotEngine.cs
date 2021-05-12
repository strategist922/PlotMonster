using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using chia_plotter.Business.Abstraction;
using chia_plotter.ResourceAccess.Abstraction;
using chia_plotter.Utiltiy.Abstraction;

namespace chia_plotter.Business.Infrastructure
{
    public class ChiaPlotProcessContextConfiguration
    {
        public string TempDrive {get;set;}
        public string DestinationDrive {get;set;}
    }
    public class ChiaPlotEngine : IChiaPlotEngine
    {
        private readonly IAsyncEnumerable<ICollection<ChiaPlotOutput>> inputChannel;
        private readonly IEnumerable<Func<ICollection<ChiaPlotOutput>, ChiaPlotOutput>> decisionMakers;
        private readonly Func<string, Task> processStarter;
        public ChiaPlotEngine(
            IAsyncEnumerable<ICollection<ChiaPlotOutput>> inputChannel,
            IEnumerable<Func<ICollection<ChiaPlotOutput>, ChiaPlotOutput>> decisionMakers,
            Func<string, Task> processStarter
            )
        {
            this.inputChannel = inputChannel;
            this.decisionMakers = decisionMakers;
            this.processStarter = processStarter;
        }
// this is the thing that makes the decisions to start a new process...  it can take in a IEnumerable of Func<ICollection<ChiaPlotOutput>, bool> decisionMakers
        public Task ProcessAsync(CancellationToken cancellationToken)
        {
            return Task.Run(async() =>
            {
                await foreach(var chiaPlotOutputs in inputChannel)
                {
                    var plotToStart = decisionMakers.Select(d => d.Invoke(chiaPlotOutputs)).Where(d => d != null).Select(d => d.TempDrive).FirstOrDefault();
                    if (!string.IsNullOrEmpty(plotToStart))
                    {
                        await processStarter.Invoke(plotToStart);
                    }
                }
            }, cancellationToken);
            // var outputChannel = Channel.CreateUnbounded<ChiaPlotOutput>();
            // await tasksRepository.AddTaskAsync(Task.Run(async () => {
            //     var channel = await repository.GetAsync(cancellationToken);
            //     var report = new ChiaPlotOutput();
            //     report.StartTime = DateTime.Now;
            //     report.TempDrive = chiaPlotEngineContextConfiguration.TempDrive;
            //     report.DestinationDrive = chiaPlotEngineContextConfiguration.DestinationDrive;
            //     await foreach(var line in channel.ReadAllAsync())
            //     {
            //         // if (line.IndexOf("TEMPDRIVE:") > -1)
            //         // {
            //         //     report.TempDrive = line.Substring(10);
            //         // }
            //         // if (line.IndexOf("DESTDRIVE:") > -1)
            //         // {
            //         //     report.DestinationDrive = line.Substring(10);
            //         // }
            //         // TODO - delegate this work so we can have versioned delegates to automatically switch over based on chia version.  Need a resource access service to get the version.  Change the engine to set that delegate based on the version.
            //         //     if we make the engine a factory where it injects in IEnumerable<IPlotOutputProcess<>>
            //         // this is just an IMapper<string, ChiaPlotOutput>
            //         // we create a mapper factory that return the mapper based on version (future enhancement)
            //         // now we can bring in the stuff from the manager to wrap this mapper
            //         if (line.IndexOf("ID:") > -1)
            //         {
            //             report.Id = line.Substring(4);
            //             await outputChannel.Writer.WriteAsync(report);
            //             break;
            //         }
            //     }
            //     await foreach(var line in channel.ReadAllAsync())
            //     {
                    
            //         var report = tryMapOutputDelegate.Map(line);
            //         if (tryMapOutputDelegate.Invoke(report, line))
            //         {
            //             await outputChannel.Writer.WriteAsync(report);
            //         }
            //     }
            // }, cancellationToken), cancellationToken);
            // return outputChannel;
        }
    }
}