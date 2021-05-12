using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using chia_plotter.ResourceAccess.Abstraction;

namespace chia_plotter.ResourceAccess.Infrastructure
{
    public class ProcessResourceAccess: IProcessResourceAccess
    {
        private readonly string tempDrive;
        private readonly string destDrive;
        private readonly string kSize;
        private readonly string ram;
        private readonly string threads;
        // private readonly ChiaPlotProcessRepository processRepo;
        private readonly Func<Func<string, Task>, CancellationToken, Task> preProcessWriter;
        private readonly Func<Func<string, Task>, CancellationToken, Task> processWriter;
        private readonly IAsyncEnumerable<string> messageReader;
        private readonly Func<string, Task> messageWriter;
        private Process process;
        private Task listener;

        public ProcessResourceAccess(
            string tempDrive, 
            string destDrive, 
            string kSize, 
            string ram, 
            string threads, 
            // ChiaPlotProcessRepository processRepo,
            Func<Func<string, Task>, CancellationToken, Task> preProcessWriter,
            Func<Func<string, Task>, CancellationToken, Task> processWriter,
            IAsyncEnumerable<string> messageReader,
            Func<string, Task> messageWriter
            )
        {
            this.tempDrive = tempDrive;
            this.destDrive = destDrive;
            this.kSize = kSize;
            this.ram = ram;
            this.threads = threads;
            // this.processRepo = processRepo;
            this.preProcessWriter = preProcessWriter;
            this.processWriter = processWriter;
            this.messageReader = messageReader;
            this.messageWriter = messageWriter;
        }

        public async Task CreateAsync(CancellationToken cancellationToken)
        {
            // this needs to be a reader per process so processes can be killed on an individual basis or more importantly paused...
            if (process != null)
            {
                throw new Exception("Cuz we suck at coding!");
            }
            
            process = new Process();
            listener = Task.Run(async () =>
            {
                 await foreach(var message in messageReader)
                 {
                     if (message == "STOP")
                     {
                         if (!process.HasExited)
                         {
                             process.CancelOutputRead();
                             process.Close();
                         }
                     }
                     else if (message == "PAUSE")
                     {
                         //@TODO
                     }
                 }
            }, cancellationToken);
            // processRepo.Add(process);
            process.StartInfo.FileName = "/bin/bash";
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.OutputDataReceived += new DataReceivedEventHandler(async (sender, e) => {
                if (cancellationToken.IsCancellationRequested)
                {
                    await messageWriter.Invoke("Cancellation requested.  Terminating process...");
                    process.CancelOutputRead();
                    process.Kill();
                    return;
                }
                await messageWriter.Invoke(e.Data);
            });
            process.Start();
            
            await process.StandardInput.WriteLineAsync("cd ~/chia-blockchain");
            await process.StandardInput.WriteLineAsync(". ./activate");
            await process.StandardInput.WriteLineAsync($"chia plots create -k {kSize} -r {threads} -b {ram} -t {tempDrive} -2 {tempDrive} -d {destDrive}");
            process.BeginOutputReadLine();
            
        }

        // public async Task<ChannelReader<string>> GetAsync(CancellationToken cancellationToken)
        // {
        //     var channel = Channel.CreateUnbounded<string>();
        //     await preProcessWriter.Invoke(async val => await channel.Writer.WriteAsync(val), cancellationToken);
            
        //     // called in pre writer  
        //     // can we remove these?  these are conditions that are checked for based on the output but there has to be a better opportunity upstream to supply this context to the listener
        //     // if this is the case, we don't need a pre process writer..  or a process writer for that matter... these concrete implementations are UbuntuChannel, WIndowsChannel
        //     await channel.Writer.WriteAsync($"TEMPDRIVE:{tempDrive}");
        //     await channel.Writer.WriteAsync($"DESTDRIVE:{destDrive}");
           
        //     await processWriter.Invoke()
        //     // called in processWriter
        //     var process = new Process();

        //     processRepo.Add(process);
        //     process.StartInfo.FileName = "/bin/bash";
        //     process.StartInfo.RedirectStandardInput = true;
        //     process.StartInfo.RedirectStandardOutput = true;
        //     process.OutputDataReceived += new DataReceivedEventHandler(async (sender, e) => {
        //         if (cancellationToken.IsCancellationRequested)
        //         {
        //             await channel.Writer.WriteAsync("Cancellation requested.  Terminating process...");
        //             process.CancelOutputRead();
        //             process.Kill();
        //             return;
        //         }
        //         await channel.Writer.WriteAsync(e.Data);
        //     });
        //     process.Start();
            
        //     await process.StandardInput.WriteLineAsync("cd ~/chia-blockchain");
        //     await process.StandardInput.WriteLineAsync(". ./activate");
        //     await process.StandardInput.WriteLineAsync($"chia plots create -k {kSize} -r {threads} -b {ram} -t {tempDrive} -2 {tempDrive} -d {destDrive}");
        //     process.BeginOutputReadLine();
            
        //     return channel;
        // }
    }
}