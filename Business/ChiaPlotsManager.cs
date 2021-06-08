using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using chia_plotter.Business.Abstraction;
using chia_plotter.ResourceAccess.Abstraction;
using chia_plotter.ResourceAccess.Infrastructure;
using Microsoft.Extensions.Logging;

namespace chia_plotter.Business.Infrastructure
{
    public class ChiaPlotsManager
    {
        private readonly ChiaPlotManagerContextConfiguration chiaPlotManagerContextConfiguration;
        private readonly Func<string, string, string, string, string, ChiaPlotProcessRepository, IChiaPlotProcessChannel> chiaPlotProcessChannelFactory;
        private readonly Action<ICollection<ChiaPlotOutput>, StringBuilder> allOutputsDelegate;
        private readonly Func<string, Task> tempDriveCleanerDelegate;
        private readonly ILogger<ChiaPlotsManager> logger;

        private readonly ChiaPlotProcessRepository processRepo;
        public ChiaPlotsManager(
            ChiaPlotManagerContextConfiguration chiaPlotManagerContextConfiguration, 
            ChiaPlotProcessRepository processRepo,
            Func<string, string, string, string, string, ChiaPlotProcessRepository, IChiaPlotProcessChannel> chiaPlotProcessChannelFactory,
            Action<ICollection<ChiaPlotOutput>, StringBuilder> allOutputsDelegate,
            Func<string, Task> tempDriveCleanerDelegate,
            ILogger<ChiaPlotsManager> logger
            )
        {
            this.chiaPlotManagerContextConfiguration = chiaPlotManagerContextConfiguration;
            this.processRepo = processRepo;
            this.chiaPlotProcessChannelFactory = chiaPlotProcessChannelFactory;
            this.allOutputsDelegate = allOutputsDelegate;
            this.tempDriveCleanerDelegate = tempDriveCleanerDelegate;
            this.logger = logger;
        }

        public async Task Process() 
        {
            var currentDestinationIndex = 0;
            var uniqueOutputs = new Dictionary<string, ChiaPlotOutput>();
            var outputChannel = Channel.CreateUnbounded<ChiaPlotOutput>();
            var ignoredDrives = new List<string>();
            var staticText = new StringBuilder();
            var maxParallelPlotsPerStagger = 4;
            // initialization process to start 2 plots per temp drive
            // don't ignore plot drives but do check plot drives are not on the ignoreDrives list.  This will allow us to have the same temp and dest drive and ignore when full.
// where do I add the logic to stagger?
            var smallestPlotSize = chiaPlotManagerContextConfiguration.KSizes.OrderBy(k => k.PlotSize).First();
            foreach (var tempDrive in chiaPlotManagerContextConfiguration.TempPlotDrives) 
            {
                Console.WriteLine($"Starting plots for tempDrive: {tempDrive}");
                var destinations = new List<string>();
                while (destinations.Count < maxParallelPlotsPerStagger)
                {
                    var destination = chiaPlotManagerContextConfiguration.DestinationPlotDrives.Skip(currentDestinationIndex).FirstOrDefault();
                    if (string.IsNullOrEmpty(destination))
                    {
                        currentDestinationIndex = 0;
                        destination = chiaPlotManagerContextConfiguration.DestinationPlotDrives.Skip(currentDestinationIndex).First();
                    }

                    var destinationInfo = new DriveInfo(destination);
                    
                    var outputs = uniqueOutputs.Values;
                    var totalSpaceNeeded = outputs.Where(o => o.IsTransferComplete == false).Where(o => o.DestinationDrive == destination).Select(o => smallestPlotSize.PlotSize).Sum() + smallestPlotSize.PlotSize;
                    
                    var gotit = false;
                    while (gotit == false) 
                    {
                        try
                        {
                            if (destinationInfo.AvailableFreeSpace > totalSpaceNeeded) 
                            {
                                destinations.Add(destination);
                            }
                            gotit = true;
                        }
                        catch(Exception ex) 
                        {
                            logger.LogError(ex, $"Found during destinationInfo invoke for {destination}");
                        }
                    }
                    currentDestinationIndex++;
                }

                var innerForEachBreaker = false;
                foreach(var dest in destinations) 
                {
                    if (innerForEachBreaker) 
                    {
                        break;
                    }

                    var process = await startProcess(dest, tempDrive);
                    if (process != null)
                    {
                        var first = await process.Reader.ReadAsync();
                        if (!string.IsNullOrWhiteSpace(first.InvalidDrive)) 
                        {
                            Console.WriteLine($"Invalid drive {first.InvalidDrive} for plot process from {tempDrive} to {dest}");
                            if (first.InvalidDrive == tempDrive && tempDrive != dest) {
                                innerForEachBreaker = true;
                                continue;
                            }

                            // if(!ignoredDrives.Any(id => id == first.InvalidDrive))
                            // {
                            //     ignoredDrives.Add(first.InvalidDrive);
                            // }
                        }
                        else
                        {
                            await foreach(var value in process.Reader.ReadAllAsync())
                            {
                                if (!string.IsNullOrWhiteSpace(value.Id))
                                {
                                    uniqueOutputs[value.Id] = value;
                                    break;
                                }
                            }
                            Task task = Task.Run(async () => {
                                await foreach(var value in process.Reader.ReadAllAsync())
                                {
                                    await outputChannel.Writer.WriteAsync(value);
                                }
                            });
                        }
                    }
                    else
                    {
                        Console.WriteLine($"NULL PROCESS FOUND FOR: {tempDrive} to {dest}");
                    }
                }
            }

            // watching process that keeps at least 2 running and will start one when transfer starts

            var keepRunning = true;
            
            var xferInProgress = new Dictionary<string, Task>();
            while (keepRunning)
            {
                await foreach(var output in outputChannel.Reader.ReadAllAsync())
                {
                    if (string.IsNullOrWhiteSpace(output.Id))
                    {
                        continue;
                    }
                    if (output.Id == "static")
                    {
                        staticText.AppendLine(output.Output);
                        continue;
                    }
                    uniqueOutputs[output.Id] = output;
                    var outputs = uniqueOutputs.Values;
                    
                    // since the destination is the temp drive, we will get the file we need without the .2.tmp
                    if (!string.IsNullOrEmpty(output.FinalFilePath) && !xferInProgress.ContainsKey(output.Id)) 
                    {
                    // we are going to kill the process and move the file ourself.
                        xferInProgress.Add(output.Id, Task.Run(() => {
                            var plotFileName = Path.GetFileName(output.FinalFilePath);

                            if (string.IsNullOrEmpty(plotFileName)) 
                            {
                                // throw?
                                return;
                            }
                            // this can fail is drive is not accessable...
                            // this is why this should happen as a feature... then this can return a destination. the feature can then move something
                            while (true)
                            {
                                // this should be wrapped in a repository to handle the retry logic.
                                try 
                                {
                                    File.Move(output.FinalFilePath, Path.Combine(output.DestinationDrive, plotFileName), true);
                                    break;
                                }
                                catch(Exception ex)
                                {
                                    logger.LogError(ex, $"Error while moving file from {output.FinalFilePath} to {Path.Combine(output.DestinationDrive, plotFileName)}.");
                                    // log so we can handle different errors.
                                    // this could get into an infnite loop where destination is full
                                    //      just another reason for it to be a feature
                                }
                            }
                        }));
                    }
                 
                    if (!ignoredDrives.Any(d => d == output.DestinationDrive || d == output.TempDrive))
                    {
                        var startNewProcess = false;
                        var related = outputs.Where(o => o.TempDrive == output.TempDrive);
                        var completed = related.Where(o => o.IsPlotComplete);
                        var remaining = related.Where(o => !o.IsPlotComplete);
                        
                        // this isn't going to work unless there is another related temp drive that emits since there is a huge delay between the plot completed output and the transfer completed output.  This is going to become another feature that returns the next available resource to start a process.  so the client periodically checks for the next process and then calls the plot registration feature. need a scheduler and check every 60 seconds. initally it will check faster until it gets a null response and then increase the time. needed so it doesn't rely on the natural output.  This also fixes the first plot problem where if we never get a message, how do we know what to start?  chicken or egg, no longer.
                        if (remaining.Count() < chiaPlotManagerContextConfiguration.PlotsPerDrive
                            && remaining
                                .Where(o => string.IsNullOrWhiteSpace(o.CurrentPhase) || (o.CurrentPhase == "1"))
                                    .Count() < maxParallelPlotsPerStagger)
                        {
                            startNewProcess = true;
                        }
// so when one gets done, it isn't transfered when the next one starts.  this will cause there to be space not accounted for on the destination.
// we need to consider tasks not completed.
                        if (startNewProcess)
                        {
                            var destinationDrive = string.Empty;
                            while(string.IsNullOrEmpty(destinationDrive))
                            {
                                var destination = chiaPlotManagerContextConfiguration.DestinationPlotDrives.Skip(currentDestinationIndex).FirstOrDefault();
                                if (string.IsNullOrEmpty(destination))
                                {
                                    currentDestinationIndex = 0;
                                    destination = chiaPlotManagerContextConfiguration.DestinationPlotDrives.Skip(currentDestinationIndex).First();
                                }

                                var destinationInfo = new DriveInfo(destination);
                                var totalSpaceNeeded = outputs.Where(o => o.IsTransferComplete == false).Where(o => o.DestinationDrive == destination).Select(o => smallestPlotSize.PlotSize).Sum() + smallestPlotSize.PlotSize;
                                if (destinationInfo.AvailableFreeSpace > totalSpaceNeeded) 
                                {
                                    destinationDrive = destination;
                                }
                                currentDestinationIndex++;
                            }

                            var process = await startProcess(destinationDrive, output.TempDrive);
                            var first = await process.Reader.ReadAsync();

                            await foreach(var value in process.Reader.ReadAllAsync())
                            {
                                if (!string.IsNullOrWhiteSpace(value.Id))
                                {
                                    uniqueOutputs[value.Id] = value;
                                    break;
                                }
                            }
                            Task task = Task.Run(async () => {
                                await foreach(var value in process.Reader.ReadAllAsync())
                                {
                                    await outputChannel.Writer.WriteAsync(value);
                                }
                            });
                        }
                    }
                    
                    var ignoredDrivesOutput = new StringBuilder();
                    ignoredDrivesOutput.Append("Ingored Drives: ");
                    ignoredDrivesOutput.AppendJoin(',', ignoredDrives);
                    ignoredDrivesOutput.AppendLine(staticText.ToString());
                    allOutputsDelegate.Invoke(outputs, ignoredDrivesOutput);
                }
                keepRunning = (chiaPlotManagerContextConfiguration.TempPlotDrives.All(t => ignoredDrives.Any(i => i == t)) || chiaPlotManagerContextConfiguration.DestinationPlotDrives.All(t => ignoredDrives.Any(i => i == t))) == false;
        
                if (!keepRunning)
                {
                    Console.WriteLine("WHY?");
                }
            }
        }

        private async Task<Channel<ChiaPlotOutput>> startProcess(string destination, string temp) 
        {
            var destinationDrive = new DriveInfo(destination);
            var tempDrive = new DriveInfo(temp);
            await tempDriveCleanerDelegate.Invoke(temp);
            ChiaPlotEngine engine = null;
            var kSize = chiaPlotManagerContextConfiguration.KSizes.Where(k => k.K == "32").First();
 
            engine = new ChiaPlotEngine(chiaPlotProcessChannelFactory.Invoke(
                temp,
                destination,
                kSize.K,
                kSize.Ram.ToString(),
                kSize.Threads.ToString(),
                processRepo
            ));
            return await engine.Process();
        }

        // private async Task<Channel<ChiaPlotOutput>> startProcess(string destination, string temp) 
        // {
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
        //             ));
        //             return await engine.Process();
        //         }
        //         else
        //         {
        //             if (destinationDrive.AvailableFreeSpace < kSize.PlotSize)
        //             {
        //                 var channel = Channel.CreateBounded<ChiaPlotOutput>(1); 
        //                 await channel.Writer.WriteAsync(new ChiaPlotOutput { InvalidDrive = destination }); 
        //                 return channel;
        //             }
        //             else if (tempDrive.AvailableFreeSpace < kSize.WorkSize)
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