using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.Business.Abstraction;
using PlotMonster.Business.Abstraction;
using PlotMonster.ResourceAccess.Abstraction;

namespace PlotMonster.Business.Implementation
{
    public class PlotSizeDeterminationEngine: IPlotSizeDeterminationEngine
    {
        private readonly ICollection<PlotSize> plotSizes {get;set;}
        private readonly ICollection<string> destinationDrives {get;set;}
        // may need a ignoredDestinationDrive resource.  eventaully we can have a periodic check for ignore destinations where we consider running processes.  if no running processes, nothing to clean up and no space, we can remove it from the config 
        private readonly ICollection<string> ignoredDestinationDrives {get;set;}

        //DriveInfo and plotSize and outputs.
        private readonly IRulesEngine<PlotSizeDeterminationContext, Task<bool>> rulesEngine {get;set;}
        public PlotSizeDeterminationEngine(
            Func<ICollection<PlotSize>> plotSizesDelegate,
            Func<ICollection<string>> destinationDrives,
            IRulesEngine<PlotSizeDeterminationContext, Task<bool>> rulesEngine
        )
        {
            this.plotSizes = plotSizesDelegate.Invoke().OrderBy(p => p.K).ToList();
            this.destinationDrives = destinationDrives.Invoke();
            this.ignoredDestinationDrives = new List<string>();
            this.rulesEngine = rulesEngine;
        }
        
        public async Task<AvailablePlotResource> DeterminePlotSizeAsync(string tempDrive, IEnumerable<ChiaPlotOutput> outputs, CancellationToken cancellationToken)
        {
            foreach (var plotSize in plotSizes)
            {
                foreach(var destinationDrive in destinationDrives.Where(d => !ignoredDestinationDrives.Any(i => i == d)))
                {
                    // how expensive is drive info to instantiate?
                    var drive = new DriveInfo(destinationDrive);
                    // if we delegate this as a rule, you get a DriveInfo and plotSize and outputs.
                    // we need to consider the ksizes that are currently being processes
                    //      this can produce a false positive where a process is transfering but we don't know how much data is transfered.
                    //          2 options)
                    //              1) track the xfer % - this is probably expensive
                    //              2) with the cleanup logic for ignored drives, we will eventually know to fill it.
                    
                    if (await rulesEngine.Process(
                        new PlotSizeDeterminationContext
                        {
                            DestinationDriveInfo = drive,
                            PlotSize = plotSize,
                            ChiaPlotOutputs = outputs
                        }
                    , cancellationToken))
                    {
                        // we have a winner and can return
                        return new AvailablePlotResource
                        {
                            DestinationDrive = destinationDrive,
                            PlotSize = plotSize
                        };
                    }

                    // this is our rule
                    // var currentOutputsToDestination = outputs.Where(o => o.DestinationDrive == destinationDrive);
                    // var totalAccumulatedDestinationSizePrediction = currentOutputsToDestination.Select(o => plotSizes.Where(p => p.K.ToString() == o.KSize).Select(p => p.FinalPlotSize).First()).Sum();

                    // if (drive.TotalFreeSpace - totalAccumulatedDestinationSizePrediction < plotSize.FinalPlotSize)
                    // {
                    //     continue;
                    // }
                    
                }
            }
            return null;
        }
    }
}