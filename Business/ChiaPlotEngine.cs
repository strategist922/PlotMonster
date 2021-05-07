using System;
using System.Threading.Channels;
using System.Threading.Tasks;
using chia_plotter.Business.Abstraction;
using chia_plotter.ResourceAccess.Abstraction;

namespace chia_plotter.Business.Infrastructure
{
    // this is where I clean it up so the engine listens to the chiaPlotEnumerable and return the clean model
    public class ChiaPlotEngine
    {
        private readonly IChiaPlotProcessChannel repository;
        public ChiaPlotEngine(IChiaPlotProcessChannel repository)
        {
            this.repository = repository;
        }

        public async Task<Channel<ChiaPlotOutput>> Process()
        {
            var channel = await repository.Get(); //.Where(v => !string.IsNullOrWhiteSpace(v));
            var report = new ChiaPlotOutput();
            report.StartTime = DateTime.Now;
            var outputChannel = Channel.CreateUnbounded<ChiaPlotOutput>();
            Task task = Task.Run(async () => {
                await foreach(var line in channel.ReadAllAsync())
                {
                    if (line.IndexOf("TEMPDRIVE:") > -1)
                    {
                        report.TempDrive = line.Substring(10);
                    }
                    if (line.IndexOf("DESTDRIVE:") > -1)
                    {
                        report.DestinationDrive = line.Substring(10);
                    }
                    if (line.IndexOf("ID:") > -1)
                    {
                        report.Id = line.Substring(4);
                        await outputChannel.Writer.WriteAsync(report);
                        break;
                    }
                }
                await foreach(var line in channel.ReadAllAsync())
                {
                    if (report.IsTransferComplete == true) 
                    {
                        await outputChannel.Writer.WriteAsync(report);
                        break;
                    }
                    report.Output = line;
                    if (line.IndexOf("Final File size: ") > -1)
                    {
                        report.IsPlotComplete = true;
                    }
                    else if (line.IndexOf("Time for phase") > -1)
                    {
                        if (line.IndexOf("phase 1") > -1)
                        {
                            
                        }
                        else if (line.IndexOf("phase 2") > -1)
                        {
                            
                        }
                        else if (line.IndexOf("phase 3") > -1)
                        {
                            
                        }
                        else if (line.IndexOf("phase 4") > -1)
                        {
                            
                        }
                    }
                    else if (line.IndexOf("Starting phase") > -1)
                    {
                        if (line.IndexOf("phase 1") > -1)
                        {
                            report.CurrentPhase = "1";
                        }
                        else if (line.IndexOf("phase 2") > -1)
                        {
                            report.CurrentPhase = "2";
                        }
                        else if (line.IndexOf("phase 3") > -1)
                        {
                            report.CurrentPhase = "3";
                        }
                        else if (line.IndexOf("phase 4") > -1)
                        {
                            report.CurrentPhase = "4";
                        }
                    }
                    else if (line.IndexOf("Plot size is") > -1)
                    {
                        report.KSize = line.Substring(14);
                    }
                    else if (line.IndexOf("Buffer size is") > -1)
                    {
                        report.Ram = line.Substring(16);
                    }
                    else if (line.IndexOf("threads of stripe size") > -1)
                    {
                        report.Threads = line.Substring(6, 2);
                    }
                    else if (line.IndexOf("Total time =") > -1)
                    {
                        var totalTime = line.Substring(13);
                        totalTime = totalTime.Substring(0, totalTime.IndexOf(" seconds"));
                        
                    }
                    else if (line.IndexOf("Copy time =") > -1)
                    {
                        report.IsTransferComplete = true;
                        report.Duration = DateTime.Now.Subtract(report.StartTime);
                        var copyTime = line.Substring(12);
                        report.CopyTime = copyTime.Substring(0, copyTime.IndexOf(" seconds"));
                    }
                    await outputChannel.Writer.WriteAsync(report);
                }
            });
            return outputChannel;
        }
    }
}