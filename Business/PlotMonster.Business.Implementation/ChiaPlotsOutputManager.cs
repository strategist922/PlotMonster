/*
we need another manager that outputs a IAsyncEnumerable that the clients plug into.
this will eventually morph into some service invocation ingress so plotting can happen while the UI is being developed.
*/
namespace PlotMonster.Business.Implementation
{
    public class ChiaPlotsProcessesOutputManager
    {
        private readonly IChiaPlotOutputRepository chiaPlotOutputRepository;
        public ChiaPlotsProcessesOutputManager(
            IChiaPlotOutputRepository chiaPlotOutputRepository
        )
        {
            this.chiaPlotOutputRepository = chiaPlotOutputRepository;
        }

        public IAsyncEnumerable<ICollection<ChiaPlotOutput>> Process(CancellationToken cancellationToken)
        {
            return chiaPlotOutputRepository.GetRunningProcesses(cancellationToken);
        }
    }
}