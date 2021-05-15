namespace PlotMonster.Client.Feature
{
    public static class WellknownSystemType
    {
        public static string Linux64Bit = @"linux-x64";
        // todo - get the runtime string for windows.. add variable process start based on config 
        public static string Windows10 = @"windows10x64";
    }
    public class ChiaPlotterFeatureContextConfiguration
    {
        public List<string> TempDrives {get;set;}
        public List<string> DestinationDrives {get;set;}
        public List<PlotSize> PlotSizes {get;set;}
        public int MaxParallelPlotsPerTempDrive {get;set;}
        public bool StaggeredPlots {get;set;}
        public string SystemType {get;set;}
    }
}