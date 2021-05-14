using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using chia_plotter.Business.Abstraction;
using chia_plotter.ResourceAccess.Abstraction;
using chia_plotter.ResourceAccess.Infrastructure;

namespace chia_plotter.Business.Infrastructure
{
    public class ChiaPlotManager : IChiaPlotManager
    {
        // private readonly ChiaPlotManagerContextConfiguration chiaPlotManagerContextConfiguration;
        // private readonly Func<string, string, string, string, string, ChiaPlotProcessRepository, IChiaPlotProcessChannel> chiaPlotProcessChannelFactory;
        // private readonly Action<ICollection<ChiaPlotOutput>, StringBuilder> allOutputsDelegate;
        // private readonly Func<string, Task> tempDriveCleanerDelegate;
        // private readonly IRunningTasksRepository runningTasksRepository;

        // private readonly ChiaPlotProcessRepository processRepo;
        // private readonly IChiaPlotEngine chiaPlotEngine;
        private readonly IRulesEngine rulesEngine;
        // private readonly IMapper<string, ChiaPlotOutput> chiaPlotOutputMapper;
        // private readonly Func<CancellationToken, Task> plotProcessStarter;
        private readonly IChiaPlotOutputRepository chiaPlotOutputRepository;
        private readonly IPlotSizeDeterminationEngine plotSizeDeterminationEngine;
        private readonly IProcessResourceAccess processResourceAccess;

        public ChiaPlotManager(
            // ChiaPlotManagerContextConfiguration chiaPlotManagerContextConfiguration, 
            // ChiaPlotProcessRepository processRepo,
            // IRunningTasksRepository runningTasksRepository,
            // Func<string, string, string, string, string, ChiaPlotProcessRepository, IChiaPlotProcessChannel> chiaPlotProcessChannelFactory,
            // Action<ICollection<ChiaPlotOutput>, StringBuilder> allOutputsDelegate,
            // Func<string, Task> tempDriveCleanerDelegate,
            // IChiaPlotEngine chiaPlotEngine,
            // IAsyncEnumerable<string> input,
            // Action<string> output,
            IRulesEngine rulesEngine,
            // IMapper<string, ChiaPlotOutput> chiaPlotOutputMapper,
            // Func<CancellationToken, Task> plotProcessStarter,
            IChiaPlotOutputRepository chiaPlotOutputRepository,
            IPlotSizeDeterminationEngine plotSizeDeterminationEngine,
            IProcessResourceAccess processResourceAccess
            )
        {
            this.rulesEngine = rulesEngine;
            // this.chiaPlotOutputMapper = chiaPlotOutputMapper;
            // this.plotProcessStarter = plotProcessStarter;
            this.chiaPlotOutputRepository = chiaPlotOutputRepository;
            this.plotSizeDeterminationEngine = plotSizeDeterminationEngine;
            this.processResourceAccess = processResourceAccess;
        }

        public async Task ProcessAsync(CancellationToken cancellationToken)
        {
            // need to start at lease one process or GetRunningProcesses will never output
            var plotToStart = rulesEngine.ProcessAsync(outputs);
            if (plotToStart == null) 
            {
                throw new Exception("Starting initial plot failed.  There is no plot to start based on the current configuration");
            }
            await startPlotProcessAsync(plotToStart, cancellationToken);

            await foreach(var outputs in chiaPlotOutputRepository.GetRunningProcesses(cancellationToken))
            {
                var plotToStart = rulesEngine.ProcessAsync(outputs, cancellationToken);
                if (plotToStart != null)
                {
                    await startPlotProcessAsync(plotToStart, cancellationToken);
                }
            }
        }

        private async Task startPlotProcessAsync(PlotToStart plotToStart, CancellationToken cancellationToken)
        {
            var kSize = plotSizeDeterminationEngine.DeterminePlotSizeAsync(plotToStart.TempDrive, plotToStart.DestinationDrive);
            var applicablePlot = plots.Whaere(p => p.KSize == kSize).First();
            await chiaPlotOutputRepository.AddProcessAsync(
                await processResourceAccess.CreateAsync(
                    new PlotProcessMetadata
                    {
                        TempDrive = plotToStart.TempDrive,
                        DestDrive = plotToStart.DestinationDrive,
                        KSize = applicablePlot.KSize,
                        Ram = applicablePlot.Ram,
                        Threads = applicablePlot.Threads
                    }, cancellationToken));
        }
/*
    ProcessAsync sequence of events 
OLD
 1. get running chia plot process repo.  this outputs the process string
 2. map string to ChiaPlotOutput. how do we do this?  each output needs to do it, we don't aggregate it.  so this is a Explicit map class that 1) isolates the ugly mapping. 2) tracks a single ChiaPlotOutput. 3) is transient.
 3. aggregate all ChiaPlotOutput streams into a dictionary of unique ChiaPlotOutput.  we have this somewhere in a repo
 4. pass this unique ChiaPlotOutput list into the start plot process rules engine
 5. if we should start a new procees, 1) we do by calling the repo and to get the process. 2) then pass that into the input channel. (input channel is passed into this process, which is the output channel of the first repo we listen to in step 1)
NEW
 1. listen to the unique list output repo, 
 2. pass that into the rules engine
 3. if rules engine is true, get a running chia plot process
 4. map from string to ChiaPlotOutput
 5. pass that into the unique outputs repo (this is where it is aggregated and output to the stream we listen to initially)

IOptionsMonitor<T> - use this to get config changes.  used to do things like list of temp, dest drives...
still need ability to pass commands like kill ID 
    can read each input and then output it, like a fake cursor.  the input even would need to trigger a redraw, which would be a listener on the client draw stuff
commands
    list temp
    list dest
    add temp
    add dest
    kill id
    pause id - future enahncement

*/

            // if (rulesEngine.Process(cancellationToken) == true)
            // {
            //     await chiaPlotEngine.ProcessAsync(cancellationToken);
            // }
        

        // public async Task ProcessXAsync(CancellationToken cancellationToken) 
        // {
        //     //data flow
        //     //process -> output
        //     //output stream
        //     //  process output - emits a new string from the process stdout
        //     //  mapper/filter - converts from process string to object
        //     //  aggregator - outputs groups by unique id
        //     // end of stream
        //     //input stream
        //     //start new process listener - the smart thing that can start new process when specific criteria is met
        //     //GUI listener - outputs to the gui


        //     var currentDestinationIndex = 0;
        //     var uniqueOutputs = new Dictionary<string, ChiaPlotOutput>();
        //     var outputChannel = Channel.CreateUnbounded<ChiaPlotOutput>();
        //     var ignoredDrives = new List<string>();
        //     var staticText = new StringBuilder();
        //     // initialization process to start 2 plots per temp drive
        //     foreach (var tempDrive in chiaPlotManagerContextConfiguration.TempPlotDrives) 
        //     {
        //         var destinations = new List<string>();
        //         while (destinations.Count != chiaPlotManagerContextConfiguration.PlotsPerDrive)
        //         {
        //             var destination = chiaPlotManagerContextConfiguration.DestinationPlotDrives.Skip(currentDestinationIndex).FirstOrDefault();
        //             if (destination == null)
        //             {
        //                 currentDestinationIndex = 0;
        //                 destination = chiaPlotManagerContextConfiguration.DestinationPlotDrives.Skip(currentDestinationIndex).First();
        //             }
        //             destinations.Add(destination);
        //             currentDestinationIndex++;
        //         }
        //         foreach(var dest in destinations) 
        //         {
        //             var process = await startProcessAsync(dest, tempDrive, cancellationToken);
        //             if (process != null)
        //             {
        //                 // this is the mapping from the return of the process repo
        //                 // 
        //                 var first = await process.Reader.ReadAsync();
        //                 if (!string.IsNullOrWhiteSpace(first.InvalidDrive)) 
        //                 {
        //                     if(!ignoredDrives.Any(id => id == first.InvalidDrive))
        //                     {
        //                         ignoredDrives.Add(first.InvalidDrive);
        //                     }
        //                 }
        //                 else
        //                 {
        //                     await foreach(var value in process.Reader.ReadAllAsync())
        //                     {
        //                         if (!string.IsNullOrWhiteSpace(value.Id))
        //                         {
        //                             uniqueOutputs[value.Id] = value;
        //                             break;
        //                         }
        //                     }
        //                     // not here though
        //                     Task task = Task.Run(async () => {
        //                         await foreach(var value in process.Reader.ReadAllAsync())
        //                         {
        //                             await outputChannel.Writer.WriteAsync(value);
        //                         }
        //                     });
        //                 }
        //             }
        //         }
        //     }

        //     // watching process that keeps at least 2 running and will start one when transfer starts

        //     var keepRunning = true;
        //     while (keepRunning)
        //     {
        //         await foreach(var output in outputChannel.Reader.ReadAllAsync())
        //         {
        //             if (string.IsNullOrWhiteSpace(output.Id))
        //             {
        //                 continue;
        //             }
        //             if (output.Id == "static")
        //             {
        //                 staticText.AppendLine(output.Output);
        //                 continue;
        //             }
        //             uniqueOutputs[output.Id] = output;
        //             var outputs = uniqueOutputs.Values;
                    
        //             if (!ignoredDrives.Any(d => d == output.DestinationDrive || d == output.TempDrive))
        //             {
        //                 var startNewProcess = false;
        //                 var related = outputs.Where(o => o.TempDrive == output.TempDrive);
        //                 var completed = related.Where(o => o.IsPlotComplete);
        //                 var remaining = related.Where(o => !o.IsPlotComplete);
        //                 if (remaining.Count() < chiaPlotManagerContextConfiguration.PlotsPerDrive
        //                     && completed.Where(c => c.IsTransferComplete == false).Count() < 2)
        //                 {
        //                     startNewProcess = true;
        //                 }

        //                 if (startNewProcess)
        //                 {
        //                     var process = await startProcessAsync(output.DestinationDrive, output.TempDrive, cancellationToken);
        //                     var first = await process.Reader.ReadAsync();
        //                     if (!string.IsNullOrWhiteSpace(first.InvalidDrive)) 
        //                     {
        //                         if(!ignoredDrives.Any(id => id == first.InvalidDrive))
        //                         {
        //                             ignoredDrives.Add(first.InvalidDrive);
        //                         }
        //                     }
        //                     else
        //                     {
        //                         // await outputChannel.Writer.WriteAsync(new ChiaPlotOutput { Id = "static", Output = "started new plot"});
        //                         await foreach(var value in process.Reader.ReadAllAsync())
        //                         {
        //                             if (!string.IsNullOrWhiteSpace(value.Id))
        //                             {
        //                                 uniqueOutputs[value.Id] = value;
        //                                 break;
        //                             }
        //                         }
        //                         Task task = Task.Run(async () => {
        //                             await foreach(var value in process.Reader.ReadAllAsync())
        //                             {
        //                                 await outputChannel.Writer.WriteAsync(value);
        //                             }
        //                         });
        //                         break;
        //                     }
        //                 }
        //             }
                    
        //             var ignoredDrivesOutput = new StringBuilder();
        //             ignoredDrivesOutput.Append("Ingored Drives: ");
        //             ignoredDrivesOutput.AppendJoin(',', ignoredDrives);
        //             ignoredDrivesOutput.AppendLine(staticText.ToString());
        //             allOutputsDelegate.Invoke(outputs, ignoredDrivesOutput);
        //         }
        //         keepRunning = (chiaPlotManagerContextConfiguration.TempPlotDrives.All(t => ignoredDrives.Any(i => i == t)) || chiaPlotManagerContextConfiguration.DestinationPlotDrives.All(t => ignoredDrives.Any(i => i == t))) == false;
        
        //         if (!keepRunning)
        //         {
        //             Console.WriteLine("WHY?");
        //         }
        //     }
        // }

        // private async Task<Channel<ChiaPlotOutput>> startProcessAsync(string destination, string temp, CancellationToken cancellationToken) 
        // {
        //     // this becomes a rule... well rules
        //     // the engine is just a mapper from string to ChiaPlotOuput
        //     var destinationDrive = new DriveInfo(destination);
        //     var tempDrive = new DriveInfo(temp);
        //     await tempDriveCleanerDelegate.Invoke(temp);
        //     ChiaPlotEngine engine = null;
        //     foreach(var kSize in chiaPlotManagerContextConfiguration.KSizes) 
        //     {
        //         if (destinationDrive.AvailableFreeSpace > kSize.PlotSize && tempDrive.AvailableFreeSpace > kSize.WorkSize)
        //         {
        //             engine = new ChiaPlotEngine(chiaPlotProcessChannelFactory.Invoke(
        //                 temp,
        //                 destination,
        //                 kSize.K,
        //                 kSize.Ram.ToString(),
        //                 kSize.Threads.ToString(),
        //                 processRepo
        //             ), runningTasksRepository);
        //             return await engine.ProcessAsync(cancellationToken);
        //         }
        //         else
        //         {
        //             if (destinationDrive.AvailableFreeSpace > kSize.PlotSize)
        //             {
        //                 var channel = Channel.CreateBounded<ChiaPlotOutput>(1); 
        //                 await channel.Writer.WriteAsync(new ChiaPlotOutput { InvalidDrive = destination }); 
        //                 return channel;
        //             }
        //             else if (tempDrive.AvailableFreeSpace > kSize.WorkSize)
        //             {
        //                 var channel = Channel.CreateBounded<ChiaPlotOutput>(1); 
        //                 await channel.Writer.WriteAsync(new ChiaPlotOutput { InvalidDrive = temp }); 
        //                 return channel;
        //             }
        //         }
        //     }
        //     throw new Exception("Should not make it here!");
        // }
    }
}