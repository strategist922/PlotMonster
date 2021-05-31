/*
we need another manager that outputs a IAsyncEnumerable that the clients plug into.
this will eventually morph into some service invocation ingress so plotting can happen while the UI is being developed.
*/
using System.Collections.Generic;
using System.Threading;
using PlotMonster.Business.Abstraction;
using PlotMonster.ResourceAccess.Abstraction;

namespace PlotMonster.Business.Implementation
{
    public class ChiaPlotsOutputManager: IChiaPlotsOutputManager
    {
        private readonly IChiaPlotOutputRepository chiaPlotOutputRepository;
        public ChiaPlotsOutputManager(
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