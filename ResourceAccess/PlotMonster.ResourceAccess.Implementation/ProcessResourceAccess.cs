using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using PlotMonster.ResourceAccess.Abstraction;

namespace PlotMonster.ResourceAccess.Infrastructure
{
    public class ProcessResourceAccess: IProcessResourceAccess
    {
        private readonly Func<Process, Task> chiaPlotStarterDelegate;

        public ProcessResourceAccess(
            Func<Process, Task> chiaPlotStarterDelegate
            )
        {
            this.chiaPlotStarterDelegate = chiaPlotStarterDelegate;
        }
        
        public async Task<IAsyncEnumerable<string>> CreateAsync(PlotProcessMetadata plotProcessMetadata, CancellationToken cancellationToken)
        {
            var process = new Process();
            var channel = channel.CreateUnbounded();
            process.StartInfo.FileName = "/bin/bash";
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.OutputDataReceived += new DataReceivedEventHandler(async (sender, e) => {
                if (cancellationToken.IsCancellationRequested)
                {
                    await channel.WriteLineAsync("Cancellation requested.  Terminating process...");
                    process.CancelOutputRead();
                    process.Kill();
                    return;
                }
                await channel.WriteLineAsync(e.Data);
            });
            process.Start();
            process.BeginOutputReadLine();
            await chiaPlotStarterDelegate.Invoke(process);
            // @TODO - these lines is what is used in the starter delegate
            // await process.StandardInput.WriteLineAsync("cd ~/chia-blockchain");
            // await process.StandardInput.WriteLineAsync(". ./activate");
            // await process.StandardInput.WriteLineAsync($"chia plots create -k {plotProcessMetadata.KSize} -r {plotProcessMetadata.Threads} -b {plotProcessMetadata.Ram} -t {plotProcessMetadata.TempDrive} -2 {plotProcessMetadata.TempDrive} -d {plotProcessMetadata.DestDrive}");
            return channel.Reader;
        }
    }
}