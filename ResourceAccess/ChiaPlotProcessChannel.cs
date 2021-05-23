using System.Diagnostics;
using System.Threading.Channels;
using System.Threading.Tasks;
using chia_plotter.ResourceAccess.Abstraction;

namespace chia_plotter.ResourceAccess.Infrastructure
{
    public class ChiaPlotProcessChannel: IChiaPlotProcessChannel
    {
        private readonly string tempDrive;
        private readonly string destDrive;
        private readonly string kSize;
        private readonly string ram;
        private readonly string threads;
        private readonly ChiaPlotProcessRepository processRepo;

        public ChiaPlotProcessChannel(string tempDrive, string destDrive, string kSize, string ram, string threads, ChiaPlotProcessRepository processRepo)
        {
            this.tempDrive = tempDrive;
            this.destDrive = destDrive;
            this.kSize = kSize;
            this.ram = ram;
            this.threads = threads;
            this.processRepo = processRepo;
        }

        public async Task<ChannelReader<string>> Get()
        {
            var channel = Channel.CreateUnbounded<string>();

            var process = new Process();
            await channel.Writer.WriteAsync($"PROCESSID:{process.Id}");
            await channel.Writer.WriteAsync($"TEMPDRIVE:{tempDrive}");
            await channel.Writer.WriteAsync($"DESTDRIVE:{destDrive}");

            processRepo.Add(process);
            process.StartInfo.FileName = "/bin/bash";
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.OutputDataReceived += new DataReceivedEventHandler(async (sender, e) => {
                await channel.Writer.WriteAsync(e.Data);
            });
            process.Start();
            
            process.StandardInput.WriteLine("cd ~/chia-blockchain");
            process.StandardInput.WriteLine(". ./activate");
            process.StandardInput.WriteLine($"chia plots create -k {kSize} -r {threads} -b {ram} -t {tempDrive} -2 {tempDrive} -d {destDrive}");
            process.BeginOutputReadLine();

            return channel;
        }
    }
}