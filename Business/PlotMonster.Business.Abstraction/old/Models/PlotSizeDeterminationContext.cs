using System.Collections.Generic;
using System.IO;
using PlotMonster.ResourceAccess.Abstraction;

namespace PlotMonster.Business.Abstraction
{
    public class PlotSizeDeterminationContext
    {
        public DriveInfo DestinationDriveInfo {get;set;}
        public PlotSize PlotSize {get;set;}
        public IEnumerable<ChiaPlotOutput> ChiaPlotOutputs {get;set;}
    }
}