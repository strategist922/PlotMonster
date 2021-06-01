using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PlotMonster.Business.Abstraction;
using PlotMonster.Access.Abstraction;
using System.Linq;
using Core.Utiltiy.Abstraction;
using System;
using System.Linq.Async;

namespace PlotMonster.Business.Implementation
{
    public class PlotOutputManager: IPlotOutputManager
    {
        private readonly IPlotOutputAccess plotOutputAccess;
        private readonly Func<PlotMonster.Access.Abstraction.ChiaPlotOutput, Abstraction.ChiaPlotOutput> mapper;
        private readonly Func<PlotMonster.Access.Abstraction.ChiaPlotOutput, bool> filter;

        public PlotOutputManager(
            IPlotOutputAccess plotOutputAccess,
            Func<PlotMonster.Access.Abstraction.ChiaPlotOutput, Abstraction.ChiaPlotOutput> mapper
        )
        {
            this.plotOutputAccess = plotOutputAccess;
            this.mapper = mapper;
        }
        public async Task<IAsyncEnumerable<Abstraction.ChiaPlotOutput>> Process(CancellationToken cancellationToken)
        {
            var outputs = await plotOutputAccess.GetPlotOutputs(cancellationToken);
            return outputs.SelectAsync(mapper);
        }
    }
}