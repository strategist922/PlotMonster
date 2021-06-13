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
using System.Collections.Concurrent;

namespace chia_plotter.Business.Infrastructure
{
    public class ChiaPlotsManager
    {
        private readonly ChiaPlotManagerContextConfiguration chiaPlotManagerContextConfiguration;
        private readonly Func<string, string, string, string, string, ChiaPlotProcessRepository, string, string, IChiaPlotProcessChannel> chiaPlotProcessChannelFactory;
        private readonly Action<ICollection<ChiaPlotOutput>, StringBuilder> allOutputsDelegate;
        private readonly Func<string, Task> tempDriveCleanerDelegate;
        private readonly ILogger<ChiaPlotsManager> logger;
        private readonly Channel<ChiaPlotOutput> outputChannel;
        private readonly ICollection<Task> runningTasks;
        private readonly IDictionary<string, ChiaPlotOutput> uniOutputs;

        private readonly ChiaPlotProcessRepository processRepo;
        public ChiaPlotsManager(
            ChiaPlotManagerContextConfiguration chiaPlotManagerContextConfiguration,
            ChiaPlotProcessRepository processRepo,
            Func<string, string, string, string, string, ChiaPlotProcessRepository, string, string, IChiaPlotProcessChannel> chiaPlotProcessChannelFactory,
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

            this.outputChannel = Channel.CreateUnbounded<ChiaPlotOutput>();
            this.runningTasks = new List<Task>();
            this.uniOutputs = new ConcurrentDictionary<string, ChiaPlotOutput>();
        }

        public async Task Process() 
        {
            var ignoredDrives = new List<string>();
            var staticText = new StringBuilder();
            
            var smallestPlotSize = chiaPlotManagerContextConfiguration.KSizes.OrderBy(k => k.PlotSize).First();
            foreach (var tempDrive in chiaPlotManagerContextConfiguration.TempPlotDrives) 
            {
                logger.LogInformation($"Starting plots for tempDrive: {tempDrive}");
                await startProcess(tempDrive, tempDrive);
            }
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
                    uniOutputs[output.Id] = output;
                    var outputs = uniOutputs.Values;

                        var related = outputs.Where(o => o.TempDrive == output.TempDrive);
                        var completed = related.Where(o => o.IsPlotComplete);
                        var remaining = related.Where(o => !o.IsPlotComplete);
                        // this isn't going to work unless there is another related temp drive that emits since there is a huge delay between the plot completed output and the transfer completed output.  This is going to become another feature that returns the next available resource to start a process.  so the client periodically checks for the next process and then calls the plot registration feature. need a scheduler and check every 60 seconds. initally it will check faster until it gets a null response and then increase the time. needed so it doesn't rely on the natural output.  This also fixes the first plot problem where if we never get a message, how do we know what to start?  chicken or egg, no longer.
                        if (remaining.Count() < chiaPlotManagerContextConfiguration.PlotsPerDrive
                            && remaining
                                .Where(o => string.IsNullOrWhiteSpace(o.CurrentPhase) || (o.CurrentPhase == chiaPlotManagerContextConfiguration.StaggerAfterPhase))
                                    .Count() < chiaPlotManagerContextConfiguration.PlotsPerStagger)
                        {
                            await startProcess(output.TempDrive, output.TempDrive);
                        }
                    var ignoredDrivesOutput = new StringBuilder();
                    ignoredDrivesOutput.Append("Ingored Drives: ");
                    ignoredDrivesOutput.AppendJoin(',', ignoredDrives);
                    ignoredDrivesOutput.AppendLine(staticText.ToString());
                    allOutputsDelegate.Invoke(outputs, ignoredDrivesOutput);
                }
            }
        }

        private async Task startProcess(string destination, string temp) 
        {
            await tempDriveCleanerDelegate.Invoke(temp);
            ChiaPlotEngine engine = null;
            var kSize = chiaPlotManagerContextConfiguration.KSizes.Where(k => k.K == "32").First();
 
            engine = new ChiaPlotEngine(chiaPlotProcessChannelFactory.Invoke(
                temp,
                destination,
                kSize.K,
                kSize.Ram.ToString(),
                kSize.Threads.ToString(),
                processRepo,
                chiaPlotManagerContextConfiguration.FarmerPublicKey,
                chiaPlotManagerContextConfiguration.PoolPublicKey
            ));
            var process = await engine.Process();
            await foreach(var value in process.Reader.ReadAllAsync())
            {
                if (!string.IsNullOrWhiteSpace(value.Id))
                {
                    uniOutputs[value.Id] = value;
                    break;
                }
            }
            Task task = Task.Run(async () => {
                await foreach(var value in process.Reader.ReadAllAsync())
                {
                    await outputChannel.Writer.WriteAsync(value);
                }
            });
            runningTasks.Add(task);
        }
    }
}