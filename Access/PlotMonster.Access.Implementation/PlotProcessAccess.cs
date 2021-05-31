using System.Threading;
using System.Threading.Tasks;

namespace PlotMonster.Access.Implementation
{
    public class PlotProcessAccess: IPlotProcessAccess
    {
        // private List<IAsyncEnumerable<string>> outputs {get;set;}
        // private readonly Channel<ChiaPlotOutput> channel;
        // private readonly List<Channel<ChiaPlotOutput>> channels;
        private readonly Func<Process, Task> chiaPlotStarterDelegate;
        // IMapper<string, ChiaPlotOutput> chiaPlotOutputMapper;

        public Task<IAsyncEnumerable<string>> AddPlotProcess(PlotProcessMetadata metadata, CancellationToken cancellationToken)
        {
            var process = new Process();
            
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
                    channel.Cancel();
                    process.CancelOutputRead();
                    process.Kill();
                    return;
                }
                    await channel.Writer.WriteAsync(e.Data);
            });
            
            process.Start();
            process.BeginOutputReadLine();
            await chiaPlotStarterDelegate.Invoke(process);
            return channel.Reader;
        }
    }
}