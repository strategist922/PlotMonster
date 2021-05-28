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
        private readonly Func<string, Task> tempDriveCleanerDelegate;

        private readonly ChiaPlotProcessRepository processRepo;
        public ChiaPlotsManager(
            ChiaPlotManagerContextConfiguration chiaPlotManagerContextConfiguration, 
            ChiaPlotProcessRepository processRepo,
            Func<string, string, string, string, string, ChiaPlotProcessRepository, IChiaPlotProcessChannel> chiaPlotProcessChannelFactory,
            Action<ICollection<ChiaPlotOutput>, StringBuilder> allOutputsDelegate,
            Func<string, Task> tempDriveCleanerDelegate
            )
        {
            this.chiaPlotManagerContextConfiguration = chiaPlotManagerContextConfiguration;
            this.processRepo = processRepo;
            this.chiaPlotProcessChannelFactory = chiaPlotProcessChannelFactory;
            this.allOutputsDelegate = allOutputsDelegate;
            this.tempDriveCleanerDelegate = tempDriveCleanerDelegate;
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
            foreach (var tempDrive in chiaPlotManagerContextConfiguration.TempPlotDrives) 
            {
                var destinations = new List<string>();
                while (destinations.Count < maxParallelPlotsPerStagger)
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
                var innerForEachBreaker = false;
                foreach(var dest in destinations) 
                {
                    if (innerForEachBreaker) 
                    {
                        continue;
                    }

                    var process = await startProcess(dest, tempDrive);
                    if (process != null)
                    {
                        var first = await process.Reader.ReadAsync();
                        if (!string.IsNullOrWhiteSpace(first.InvalidDrive)) 
                        {
                            if (first.InvalidDrive == tempDrive && tempDrive != dest) {
                                innerForEachBreaker = true;
                                continue;
                            }

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
                    if (output.IsTransferError) {
                        // add destination to ignored 
                        var process = processRepo.GetAll().Where(p => p.Id == output.ProcessId).FirstOrDefault();
                        process.Kill(true);
                        process.Close();

                        if(!ignoredDrives.Any(id => id == output.DestinationDrive))
                        {
                            ignoredDrives.Add(output.DestinationDrive);
                        }
                        // TODO - this needs to remove the incompleted file from the destinaction
                        // start new transfer process to alternative destination
                        foreach (var destination in chiaPlotManagerContextConfiguration.DestinationPlotDrives)
                        {
                            //var tempFile = Path.Combine(output.TempDrive, $"{output.Id}");
                            if (!ignoredDrives.Any(id => id == destination)) 
                            {
                                var driveInfo = new DriveInfo(destination);
                                var tempFileName = Directory.GetFiles(output.TempDrive, output.Id).FirstOrDefault();

                                if (string.IsNullOrEmpty(tempFileName)) 
                                {
                                    // throw?
                                    break;
                                }
                                var tempFile = new FileInfo(tempFileName);
                                if (driveInfo.AvailableFreeSpace > tempFile.Length)
                                {
                                    var trimmedFileName = tempFileName.Substring(tempFileName.IndexOf("plot-k"));
                                    trimmedFileName = trimmedFileName.Substring(0, trimmedFileName.IndexOf(".plot.") + 5);
                                    tempFile.MoveTo(Path.Combine(destination, trimmedFileName), true);
                                    break;
                                }
                            }
                        }
                        
                        var incompleteTransfer = Directory.GetFiles(output.DestinationDrive, output.Id);
                        foreach (var file in incompleteTransfer) 
                        {
                            var incompleteFile = new FileInfo(file);
                            incompleteFile.Delete();
                        }

                        output.IsTransferError = false;
                        
                    }
                    
                    if (!ignoredDrives.Any(d => d == output.DestinationDrive || d == output.TempDrive))
                    {
                        var startNewProcess = false;
                        var related = outputs.Where(o => o.TempDrive == output.TempDrive);
                        var completed = related.Where(o => o.IsPlotComplete);
                        var remaining = related.Where(o => !o.IsPlotComplete);
                        
                        //1) where get all remaining that is transfering and do not count those
                        if (remaining.Count() < chiaPlotManagerContextConfiguration.PlotsPerDrive
                            && remaining
                                .Where(o => string.IsNullOrWhiteSpace(o.CurrentPhase) || (o.CurrentPhase == "1" || o.CurrentPhase == "2"))
                                    .Count() < maxParallelPlotsPerStagger
                            && completed.Where(c => c.IsTransferComplete == false).Count() < chiaPlotManagerContextConfiguration.PlotsPerDrive)
                        {
                            startNewProcess = true;
                        }

                        if (startNewProcess)
                        {
                            var process = await startProcess(output.DestinationDrive, output.TempDrive);
                            var first = await process.Reader.ReadAsync();
                      
                            if (!string.IsNullOrWhiteSpace(first.InvalidDrive)) 
                            {
                                if ((first.InvalidDrive == first.TempDrive && first.TempDrive != first.DestinationDrive) 
                                    && (!ignoredDrives.Any(id => id == first.InvalidDrive)))
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

        private async Task<Channel<ChiaPlotOutput>> startProcess(string destination, string temp) 
        {
            var destinationDrive = new DriveInfo(destination);
            var tempDrive = new DriveInfo(temp);
            await tempDriveCleanerDelegate.Invoke(temp);
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
                    return await engine.Process();
                }
                else
                {
                    if (destinationDrive.AvailableFreeSpace > kSize.PlotSize)
                    {
                        var channel = Channel.CreateBounded<ChiaPlotOutput>(1); 
                        await channel.Writer.WriteAsync(new ChiaPlotOutput { InvalidDrive = destination }); 
                        return channel;
                    }
                    else if (tempDrive.AvailableFreeSpace > kSize.WorkSize)
                    {
                        var channel = Channel.CreateBounded<ChiaPlotOutput>(1); 
                        await channel.Writer.WriteAsync(new ChiaPlotOutput { InvalidDrive = temp }); 
                        return channel;
                    }
                }
            }
            throw new Exception("Should not make it here!");
        }
    }
}