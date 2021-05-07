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

namespace chia_plotter.Business.Infrastructure
{
    public class ChiaPlotsManager
    {
        private readonly ChiaPlotManagerContextConfiguration chiaPlotManagerContextConfiguration;
        private readonly Func<string, string, string, string, string, ChiaPlotProcessRepository, IChiaPlotProcessChannel> chiaPlotProcessChannelFactory;
        private readonly Action<ICollection<ChiaPlotOutput>, StringBuilder> allOutputsDelegate;

        private readonly ChiaPlotProcessRepository processRepo;
        public ChiaPlotsManager(
            ChiaPlotManagerContextConfiguration chiaPlotManagerContextConfiguration, 
            ChiaPlotProcessRepository processRepo,
            Func<string, string, string, string, string, ChiaPlotProcessRepository, IChiaPlotProcessChannel> chiaPlotProcessChannelFactory,
            Action<ICollection<ChiaPlotOutput>, StringBuilder> allOutputsDelegate
            )
        {
            this.chiaPlotManagerContextConfiguration = chiaPlotManagerContextConfiguration;
            this.processRepo = processRepo;
            this.chiaPlotProcessChannelFactory = chiaPlotProcessChannelFactory;
            this.allOutputsDelegate = allOutputsDelegate;
        }

        public async Task Process() 
        {
            var currentDestinationIndex = 0;
            var uniqueOutputs = new Dictionary<string, ChiaPlotOutput>();
            var outputChannel = Channel.CreateUnbounded<ChiaPlotOutput>();
            var ignoredDrives = new List<string>();
            var staticText = new StringBuilder();
            // initialization process to start 2 plots per temp drive
            // failing here.. i think it is adding a blank destination drive.
                foreach (var tempDrive in chiaPlotManagerContextConfiguration.TempPlotDrives) 
                {
                    var destinations = new List<string>();
                    while (destinations.Count != chiaPlotManagerContextConfiguration.PlotsPerDrive)
                    {
                        var destination = chiaPlotManagerContextConfiguration.DestinationPlotDrives.Skip(currentDestinationIndex).FirstOrDefault();
                        if (destination == null)
                        {
                            currentDestinationIndex = 0;
                            destination = chiaPlotManagerContextConfiguration.DestinationPlotDrives.Skip(currentDestinationIndex).First();
                        }
                        destinations.Add(destination);
                        currentDestinationIndex++;
                    }
                    foreach(var dest in destinations) 
                    {
                        var process = await startProcess(dest, tempDrive);
                        if (process != null)
                        {
                            var first = await process.Reader.ReadAsync();
                            if (!string.IsNullOrWhiteSpace(first.InvalidDrive)) 
                            {
                                if(!ignoredDrives.Any(id => id == first.InvalidDrive))
                                {
                                    ignoredDrives.Add(first.InvalidDrive);
                                }
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
                    }
                }

// watching process that keeps at least 2 running and will start one when transfer starts

            var keepRunning = true;
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
                
                if (!ignoredDrives.Any(d => d == output.DestinationDrive || d == output.TempDrive))
                {
                    var startNewProcess = false;
                    var related = outputs.Where(o => o.TempDrive == output.TempDrive);
                    var completed = related.Where(o => o.IsPlotComplete);
                    var remaining = related.Where(o => !o.IsPlotComplete);
                    if (remaining.Count() < chiaPlotManagerContextConfiguration.PlotsPerDrive
                        && completed.Where(c => c.IsTransferComplete == false).Count() < 2)
                    {
                        startNewProcess = true;
                    }

                    if (startNewProcess)
                    {
                        var process = await startProcess(output.DestinationDrive, output.TempDrive);
                        var first = await process.Reader.ReadAsync();
                        if (!string.IsNullOrWhiteSpace(first.InvalidDrive)) 
                        {
                            if(!ignoredDrives.Any(id => id == first.InvalidDrive))
                            {
                                ignoredDrives.Add(first.InvalidDrive);
                            }
                        }
                        else
                        {
                            // await outputChannel.Writer.WriteAsync(new ChiaPlotOutput { Id = "static", Output = "started new plot"});
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
                            break;
                        }
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

       

        private Task<Channel<ChiaPlotOutput>> startProcess(string destination, string temp) 
        {
            var destinationDrive = new DriveInfo(destination);
            var tempDrive = new DriveInfo(temp);
            ChiaPlotEngine engine = null;
            foreach(var kSize in chiaPlotManagerContextConfiguration.KSizes) 
            {
                if (destinationDrive.AvailableFreeSpace > kSize.PlotSize && tempDrive.AvailableFreeSpace > kSize.WorkSize)
                {
                    engine = new ChiaPlotEngine(chiaPlotProcessChannelFactory.Invoke(
                        temp,
                        destination,
                        kSize.K,
                        kSize.Ram.ToString(),
                        kSize.Threads.ToString(),
                        processRepo
                    ));
                    return engine.Process();
                }
                else
                {
                    if (destinationDrive.AvailableFreeSpace > kSize.PlotSize)
                    {
                        var channel = Channel.CreateBounded<ChiaPlotOutput>(1); 
                        channel.Writer.WriteAsync(new ChiaPlotOutput { InvalidDrive = destination }); 
                        return Task.FromResult(channel);
                    }
                    else if (tempDrive.AvailableFreeSpace > kSize.WorkSize)
                    {
                        var channel = Channel.CreateBounded<ChiaPlotOutput>(1); 
                        channel.Writer.WriteAsync(new ChiaPlotOutput { InvalidDrive = temp }); 
                        return Task.FromResult(channel);
                    }
                }
            }
            throw new Exception("Should not make it here!");
        }
    }
}