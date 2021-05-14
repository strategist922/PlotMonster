using System;
using System.IO;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using chia_plotter.ResourceAccess.Abstraction;

namespace PlotMonster.ResourceAccess.Infrastructure
{
    public class ProcessResourceAccessMock: IProcessResourceAccess
    {
        public async Task<IAsyncEnumerable<string>> CreateAsync(PlotProcessMetadata plotProcessMetadata, CancellationToken cancellationToken)
        {
            var channel = Channel.CreateUnbounded<string>();
            
            var random = new Random();
            var id = "abcd" + random.Next(1000000000, int.MaxValue);
            
            await channel.Writer.WriteAsync($"TEMPDRIVE:{tempDrive}");
            await channel.Writer.WriteAsync($"DESTDRIVE:{destDrive}");

            var file = Path.Combine(Directory.GetCurrentDirectory(), "mock_plot_process.txt");
            // how can we do the reader without using Task.Run???
            // i just want to set the readLIne as the input of the channel.
            using (StreamReader reader = File.OpenText(file))
            {
                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    line = string.Format(line, id);
                    await channel.Writer.WriteAsync(line);
                    Thread.Sleep(25);
                }
                // @TODO how do i complete the channel?
                channel.Close();
            }
            
            return channel;
        }

    }
}