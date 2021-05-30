using System.Threading;
using System.Threading.Tasks;

namespace PlotMonster.Access.Implementation
{
    public class PlotProcessAccess
    {
        private IDictionary<string, ChiaPlotOutput> outputs {get;set;}
        private readonly Channel<ChiaPlotOutput> channel;
        private readonly List<Channel<ChiaPlotOutput>> channels;
        private readonly Func<Process, Task> chiaPlotStarterDelegate;
        IMapper<string, ChiaPlotOutput> chiaPlotOutputMapper;

        public Task AddPlotProcess(PlotProcessMetadata metadata, CancellationToken cancellationToken)
        {
            /*
                1) start process
                2) when value emitted, output that as a change, not the list.

                the aggregation will happen upstream (the thing that invokes this)
            */
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
                if (!string.IsNullOrWhiteSpace(chiaPlotOutput.Id))
                {
                    var output = chiaPlotOutputMapper.Map(e.Data);
                    outputs[output.ShortId] = output;
                    await channel.Writer.WriteAsync(output);
                }
            });
            process.Start();
            process.BeginOutputReadLine();
            await chiaPlotStarterDelegate.Invoke(process);
            await channel.Reader.WaitToReadAsync(cancellationToken);
        }
        public Task<IAsyncEnumerable<ChiaPlotOutput>> GetPlotProcesses(CancellationToken cancellationToken)
        {
            var chan = new Channel<ChiaPlotOutput>();
            var task = Task.Run(async () => {
                foreach(var output in outputs.Values)
                {
                    await chan.WriteAsync(output);
                }
                await foreach(var chiaPlotOutput in channel.Reader.ReadAllAsync(cancellationToken))
                {
                    await chan.WriteAsync(chiaPlotOutput);
                }
            });
            
            return Task.FromResult(chan.Reader());
        }
    }
}