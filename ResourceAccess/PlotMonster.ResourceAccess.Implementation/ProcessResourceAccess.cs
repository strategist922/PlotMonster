using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Core.Utiltiy.Abstraction;
using PlotMonster.ResourceAccess.Abstraction;

namespace PlotMonster.ResourceAccess.Infrastructure
{
    public class ProcessResourceAccess: IProcessResourceAccess
    {
        private readonly Func<Process, Task> chiaPlotStarterDelegate;
        IMapper<string, ChiaPlotOutput> chiaPlotOutputMapper;

        public ProcessResourceAccess(
            Func<Process, Task> chiaPlotStarterDelegate,
            IMapper<string, ChiaPlotOutput> chiaPlotOutputMapper
            )
        {
            this.chiaPlotStarterDelegate = chiaPlotStarterDelegate;
            this.chiaPlotOutputMapper = chiaPlotOutputMapper;
        }
        
        public async Task<IAsyncEnumerable<ChiaPlotOutput>> CreateAsync(PlotProcessMetadata plotProcessMetadata, CancellationToken cancellationToken)
        {
            var process = new Process();
            // var channel = Channel.CreateUnbounded<ChiaPlotOutput>();
            var channel = Channel.CreateBounded<ChiaPlotOutput>(
                new BoundedChannelOptions(1) 
                {
                    FullMode = BoundedChannelFullMode.DropOldest
                }
            );
            process.StartInfo.FileName = "/bin/bash";
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.OutputDataReceived += new DataReceivedEventHandler(async (sender, e) => {
                if (cancellationToken.IsCancellationRequested)
                {
                    // await channel.Writer.WriteAsync("Cancellation requested.  Terminating process...");
                    process.CancelOutputRead();
                    process.Kill();
                    return;
                }
                await channel.Writer.WriteAsync(chiaPlotOutputMapper.Map(e.Data));
            });
            process.Start();
            process.BeginOutputReadLine();
            await chiaPlotStarterDelegate.Invoke(process);
            
            await foreach(var chiaPlotOutput in channel.Reader.ReadAllAsync(cancellationToken))
            {
                if (!string.IsNullOrWhiteSpace(chiaPlotOutput.Id))
                {
                    break;
                }
            }

            // @TODO - these lines is what is used in the starter delegate
            // await process.StandardInput.WriteLineAsync("cd ~/chia-blockchain");
            // await process.StandardInput.WriteLineAsync(". ./activate");
            // await process.StandardInput.WriteLineAsync($"chia plots create -k {plotProcessMetadata.KSize} -r {plotProcessMetadata.Threads} -b {plotProcessMetadata.Ram} -t {plotProcessMetadata.TempDrive} -2 {plotProcessMetadata.TempDrive} -d {plotProcessMetadata.DestDrive}");
            return channel.Reader.ReadAllAsync(cancellationToken);
        }
    }
}