using System.Collections.Generic;
using chia_plotter.Business.Abstraction;

namespace chia_plotter.Business.Infrastructure
{
    public class ChiaPlotManagerContextConfiguration
    {
        public int PlotsPerDrive {get;set;}
        public int PlotsPerStagger {get;set;}
        public string StaggerAfterPhase {get;set;}
        public List<string> TempPlotDrives {get;set;}
        public List<string> DestinationPlotDrives {get;set;}
        public bool ExternalPlotTransferProcess {get;set;}
        public List<KSizeMetadata> KSizes {get;set;}
        public string FarmerPublicKey {get;set;}
        public string PoolPublicKey {get;set;}
    }

}