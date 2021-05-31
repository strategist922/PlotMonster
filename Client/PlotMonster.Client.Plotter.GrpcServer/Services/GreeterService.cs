using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace PlotMonster.Client.Plotter.GrpcServer
{
    public class PlotManagerService : PlotManager.PlotManagerBase
    {
        private readonly ILogger<PlotManagerService> _logger;
        public PlotManagerService(ILogger<PlotManagerService> logger)
        {
            _logger = logger;
        }

        public override Task<PlotProcessReply> GetPlotProcesses(PlotProcessesRequest request, ServerCallContext context)
        {
            // TODO: how do we return a IAsyncEnumerable?
            throw new NotImplementedException();
        }
    }
}
