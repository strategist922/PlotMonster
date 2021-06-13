using System;
using System.Threading.Channels;
using System.Threading.Tasks;
using chia_plotter.Business.Abstraction;
using chia_plotter.ResourceAccess.Abstraction;

namespace chia_plotter.Business.Infrastructure
{
    public class ChiaPlotEngine
    {
        private readonly IChiaPlotProcessChannel repository;
        public ChiaPlotEngine(IChiaPlotProcessChannel repository)
        {
            this.repository = repository;
        }

// TODO - extend on this to provide more knowledge (phase steps and completion times) about each phases progress.  That knowledge will then allow downstream to calculate progress percentage based on the average plot time.
        public async Task<Channel<ChiaPlotOutput>> Process()
        {
            var channel = await repository.Get();
            
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
                    if (line.IndexOf("PROCESSID:") > -1)
                    {   
                        var processId = 0;
                        int.TryParse(line.Substring(10), out processId);
                        report.ProcessId = processId;
                    }
                    else if (line.IndexOf("ID:") > -1)
                    {
                        report.Id = line.Substring(4);
                        await outputChannel.Writer.WriteAsync(report);
                        break;
                    }
                }
                await foreach(var line in channel.ReadAllAsync())
                {
                    report.Output = line;
                    if (line.IndexOf("Final File size: ") > -1)
                    {
                        
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
                        // report.IsTransferComplete = true;
                        report.Duration = DateTime.Now.Subtract(report.StartTime);
                        var copyTime = line.Substring(12);
                        report.CopyTime = copyTime.Substring(0, copyTime.IndexOf(" seconds"));
                    }
                    else if (line.IndexOf("Error No space left on device") > -1) {
                        //Could not copy "/chia/plottemp4/plot-k32-2021-05-26-02-29-422d6204f0e59df87f48bb9778b79194b656868bafeae9c9e73187ec670a1aa9.plot.2.tmp" to "/chia/plots/205/plot-k32-2021-05-26-02-29-422d6204f0e59df87f48bb9778b79194b656868bafeae9c9e73187ec670a1aa9.plot.2.tmp". Error No space left on device. Retrying in five minutes.
                        // that line is the wrong line.  that is if something was removed
                        report.IsPlotComplete = true;
                        report.IsTransferComplete = true;
                        report.IsTransferError = true;
                        break;

                        // TODO: need to start the file transfer and rename
                    }
                    else if (line.IndexOf("Renamed final file from") > -1)
                    {
                        //Renamed final file from "g:\\plots\\plot-k32-2021-05-06-03-15-ebff482bedeaa97a11a275eded32ad444e37be0f129369bb781b1824697825.plot.2.tmp" to "g:\\plots\\plot-k32-2021-05-06-03-15-ebff482bedeaa97a11a275e5f3ed32a1b34e37be0f129369bb781b1824697825.plot"
                        var finalFilePath = line.Substring(line.IndexOf("\" to \"") + 6);
                        finalFilePath = finalFilePath.Substring(0, finalFilePath.IndexOf(".plot\"") + 5);
                        report.FinalFilePath = finalFilePath;
                        report.IsPlotComplete = true;
                        report.IsTransferComplete = true;
                        await outputChannel.Writer.WriteAsync(report);
                        outputChannel.Writer.Complete();
                        break;
                        
                    }
                    await outputChannel.Writer.WriteAsync(report);
                }
            });
            return outputChannel;
        }
    }
}