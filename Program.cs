using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using chia_plotter.Business.Abstraction;
using chia_plotter.Business.Infrastructure;
using chia_plotter.ResourceAccess.Infrastructure;

namespace chia_plotter
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var testing = false;
            var repo = new ChiaPlotProcessRepository();
            try
            {
                var config = new ChiaPlotManagerContextConfiguration();
                config.PlotsPerDrive = 8;
                config.TempPlotDrives = new List<string>() 
                { 
                    "/chia/plottemp1",
                    "/chia/plottemp2",
                    "/chia/plottemp3"
                    // "/chia/plottemp4",
                };
                config.DestinationPlotDrives = new List<string>() 
                { 
                    // "/chia/plots/100", 
                    // "/chia/plots/101", 
                    // "/chia/plots/102", 
                    // "/chia/plots/103", 
                    // "/chia/plots/104", 
                    // "/chia/plots/105", 
                    // "/chia/plots/106", 
                    // "/chia/plots/107", 
                    // "/chia/plots/108", 
                    // "/chia/plots/109" 
                    // "/chia/plots/200", 
                    // "/chia/plots/201",
                    "/chia/plots/202",
                    "/chia/plots/203",
                    "/chia/plots/204",
                    "/chia/plots/206",
                    "/chia/plots/207"
                };
                config.KSizes = new List<KSizeMetadata>
                { 
                    // new KSizeMetadata { PlotSize = 408000000000, WorkSize = 1000000000, K = "34", Threads = 4, Ram = 8000 }, 
                    // new KSizeMetadata { PlotSize = 208000000000, WorkSize = 550000000, K = "33", Threads = 4, Ram = 6000 }, 
                    new KSizeMetadata { PlotSize = 108000000000, WorkSize = 280000000, K = "32", Threads = 2, Ram = 4000 } 
                };
            
                var manager = new ChiaPlotsManager(
                    config, 
                    repo, 
                    (
                        temp,
                        destination,
                        K,
                        Ram,
                        Threads,
                        processRepo
                    ) => {
                        if (!testing) {
                            return new ChiaPlotProcessChannel(temp, destination, K, Ram, Threads, processRepo);
                        } else {
                            return new ChiaPlotProcessChannelMock(temp, destination, K, Ram, Threads, processRepo);
                        }
                    },
                    (outputs, staticTextStringBuilder) => 
                    {
                        var lmr = new List<ChiaPlotOutput>();

                        Console.Clear();

                        Console.WriteLine(DateTime.Now.ToString("T").PadRight(50 * 3, '-'));
                        var displayColumnIndex = 0;
                        // TODO - make configurable
                        const int maxDisplayColumnWidth = 6;
                        var displayBuilder = new DisplayBuilder();
                        foreach (var uniqueOutput in outputs.Where(o => o.IsTransferComplete == false).OrderBy(o => o.TempDrive)) 
                        {   
                            displayBuilder = BuildDisplay(displayBuilder, uniqueOutput);
                            displayColumnIndex++;
                            if (displayColumnIndex == maxDisplayColumnWidth)
                            {
                                displayColumnIndex = 0;
                                Display(displayBuilder);
                                displayBuilder = new DisplayBuilder();
                            }
                        }

                        if (!string.IsNullOrEmpty(displayBuilder.Line1.ToString()))
                        {
                            Display(displayBuilder);
                        }

                        // foreach (var uniqueOutput in outputs.Where(o => o.IsTransferComplete == false)) 
                        // {                            
                        //     lmr.Add(uniqueOutput);
                                                    
                        //     if (lmr.Count == 3)
                        //     {
                        //         displayOutputs(lmr[0], lmr[1], lmr[2]);
                        //         lmr.Clear();
                        //     }
                        // }
                        // if (lmr.Count > 0) {
                        //     while (lmr.Count != 3) {
                        //         lmr.Add(new ChiaPlotOutput { Id = "..." });
                        //     }
                        //     displayOutputs(lmr[0], lmr[1], lmr[2]);
                        // }
                        // Console.WriteLine(string.Empty.PadRight(50 * maxDisplayColumnWidth, '-'), Color.BlueViolet);
                        var avg = outputs.Where(o => o.IsTransferComplete && o.Duration != default).Select(o => o.Duration);
                        var averageTime = TimeSpan.FromSeconds(avg.Any() ? avg.Average(timespan => timespan.TotalSeconds) : 0);
                        var skippedTempDrive = outputs.Where(o => o.InvalidDrive == o.TempDrive && o.TempDrive != o.DestinationDrive);
                        Console.WriteLine($"Completed: {outputs.Where(o => o.IsTransferComplete).Count()} plots with an average time of {averageTime.Hours}:{averageTime.Minutes}:{averageTime.Seconds}");
                        Console.WriteLine($"Skipped {skippedTempDrive.Count()} temp drive{(skippedTempDrive.Count() != 1 ? "s" : string.Empty)}.");
                        // Console.WriteLine(staticTextStringBuilder.ToString());
                    },
                    tempDrive => 
                    {
                        try
                        {
                            if (!testing)
                            {
                                if (Directory.Exists(Path.Combine(tempDrive, ".Trash-1000")))
                                {
                                    Directory.Delete(Path.Combine(tempDrive, ".Trash-1000"), true);
                                }
                            }
                        }
                        catch(Exception ex) 
                        {
                            // do we care?
                            Console.WriteLine($"Exception");
                            Console.WriteLine(ex.Message);
                            Console.WriteLine(ex.StackTrace);
                        }
                        return Task.CompletedTask;
                    }
                );
                await manager.Process();
            }
            catch(Exception ex) 
            {
                Console.WriteLine($"Exception");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                
            }
            finally
            {
                Console.WriteLine("==================");
                var processes = repo.GetAll();
                foreach(var process in processes)
                {
                    if (process.HasExited)
                    {
                        continue;
                    }
                    Console.WriteLine($"Killing process {process.Id}");
                    process.Kill(true);
                    process.Close();
                }
                Console.WriteLine("Done... press any key to exit");
                Console.ReadLine();
            }
        }

        // private static void displayOutputs(ChiaPlotOutput left, ChiaPlotOutput center, ChiaPlotOutput right) 
        // {
        //     var width = 50;
        //     var now = DateTime.Now;
        //     var leftTime = now.Subtract(left.StartTime);
        //     var centerTime = now.Subtract(center.StartTime);
        //     var rightTime = now.Subtract(right.StartTime);
        //     var leftId = !string.IsNullOrWhiteSpace(left.Id) && left.Id.Length > 10 ? left.Id.Substring(left.Id.Length - 10) : "waiting...";
        //     var centerId = !string.IsNullOrWhiteSpace(center.Id) && center.Id.Length > 10 ? center.Id.Substring(center.Id.Length - 10) : "waiting...";
        //     var rightId = !string.IsNullOrWhiteSpace(right.Id) && right.Id.Length > 10 ? right.Id.Substring(right.Id.Length - 10) : "waiting...";
        //     Console.WriteLine($"ID: {leftId}".PadRight(width - 1) + "|" + $"ID: {centerId}".PadRight(width - 1) + "|" + $"ID: {rightId}");
        //     Console.WriteLine($"KSize: {left.KSize}".PadRight(width - 1) + "|" + $"KSize: {center.KSize}".PadRight(width - 1) + "|" + $"KSize: {right.KSize}");
        //     Console.WriteLine($"Memory: {left.Ram}".PadRight(width - 1) + "|" + $"Memory: {center.Ram}".PadRight(width - 1) + "|" + $"Memory: {right.Ram}");
        //     Console.WriteLine($"Threads: {left.Threads}".PadRight(width - 1) + "|" + $"Threads: {center.Threads}".PadRight(width - 1) + "|" + $"Threads: {right.Threads}");
        //     Console.WriteLine($"Destination: {left.DestinationDrive}".PadRight(width - 1) + "|" + $"Destination: {center.DestinationDrive}".PadRight(width - 1) + "|" + $"Destination: {right.DestinationDrive}");
        //     Console.WriteLine($"Temp: {left.TempDrive}".PadRight(width - 1) + "|" + $"Temp: {center.TempDrive}".PadRight(width - 1) + "|" + $"Temp: {right.TempDrive}");
        //     Console.WriteLine($"Xfering: {left.IsPlotComplete}".PadRight(width - 1) + "|" + $"Xfering: {center.IsPlotComplete}".PadRight(width - 1) + "|" + $"Xfering: {right.IsPlotComplete}");
        //     Console.WriteLine($"Phase: {left.CurrentPhase}".PadRight(width - 1) + "|" + $"Phase: {center.CurrentPhase}".PadRight(width - 1) + "|" + $"Phase: {right.CurrentPhase}");
        //     Console.WriteLine($"Start: {left.StartTime.ToString("T")} ({leftTime.Hours}:{leftTime.Minutes}:{leftTime.Seconds})".PadRight(width - 1) + "|" + $"Start: {center.StartTime.ToString("T")} ({centerTime.Hours}:{centerTime.Minutes}:{centerTime.Seconds})".PadRight(width - 1) + "|" + $"Start: {right.StartTime.ToString("T")} ({rightTime.Hours}:{rightTime.Minutes}:{rightTime.Seconds})");
        //     Console.WriteLine(string.Empty.PadRight(width * 3, '-'), Color.BlueViolet);
        //     // Console.WriteLine($"({leftId}): {left.Output}");
        //     // Console.WriteLine($"({centerId}): {center.Output}");
        //     // Console.WriteLine($"({rightId}): {right.Output}");
        //     // Console.WriteLine(string.Empty.PadRight(width * 3, '-'), Color.BlueViolet);
        // }

        private static DisplayBuilder BuildDisplay(DisplayBuilder displayBuilder, ChiaPlotOutput chiaPlotOutput)
        {
            // todo - make a output property called truncatedId where we get the last n characters only once.  
            var width = 50;
            var id = !string.IsNullOrWhiteSpace(chiaPlotOutput.Id) && chiaPlotOutput.Id.Length > 10 ? chiaPlotOutput.Id.Substring(chiaPlotOutput.Id.Length - 10) : "waiting...";
            displayBuilder.Line1.Append($"ID: {id}".PadRight(width - 1) + "|");
            displayBuilder.Line2.Append($"K: {chiaPlotOutput.KSize} Ram: {chiaPlotOutput.Ram} Threads: {chiaPlotOutput.Threads}".PadRight(width - 1) + "|");
            displayBuilder.Line3.Append($"Destination: {chiaPlotOutput.DestinationDrive}".PadRight(width - 1) + "|");
            displayBuilder.Line4.Append($"Temp: {chiaPlotOutput.TempDrive}".PadRight(width - 1) + "|");
            displayBuilder.Line5.Append($"Xfering: {chiaPlotOutput.IsPlotComplete}".PadRight(width - 1) + "|");
            displayBuilder.Line6.Append($"Phase: {chiaPlotOutput.CurrentPhase}".PadRight(width - 1) + "|");
                        // var leftTime = now.Subtract(left.StartTime);
            displayBuilder.Line7.Append($"Start: {chiaPlotOutput.StartTime.ToString("T")}".PadRight(width - 1) + "|");

            // displayBuilder.Line2.Append($"Start: {chiaPlotOutput.StartTime.ToString("T")} ({leftTime.Hours}:{leftTime.Minutes}:{leftTime.Seconds})".PadRight(width - 1) + "|");
            // displayBuilder.Line2.Append();
            // displayBuilder.Line2.Append();
            // displayBuilder.Line2.Append();

            return displayBuilder;
        }

        private static void Display(DisplayBuilder displayBuilder)
        {
            var width = 50 * 6;
            Console.WriteLine(displayBuilder.Line1.ToString());
            Console.WriteLine(displayBuilder.Line2.ToString());
            Console.WriteLine(displayBuilder.Line3.ToString());
            Console.WriteLine(displayBuilder.Line4.ToString());
            Console.WriteLine(displayBuilder.Line5.ToString());
            Console.WriteLine(displayBuilder.Line6.ToString());
            Console.WriteLine(displayBuilder.Line7.ToString());
            Console.WriteLine(string.Empty.PadRight(width, '-'), Color.BlueViolet);
        }
    }

    public class DisplayBuilder
    {
        public StringBuilder Line1 {get;set;} = new StringBuilder();
        public StringBuilder Line2 {get;set;} = new StringBuilder();
        public StringBuilder Line3 {get;set;} = new StringBuilder();
        public StringBuilder Line4 {get;set;} = new StringBuilder();
        public StringBuilder Line5 {get;set;} = new StringBuilder();
        public StringBuilder Line6 {get;set;} = new StringBuilder();
        public StringBuilder Line7 {get;set;} = new StringBuilder();
        public StringBuilder Line8 {get;set;} = new StringBuilder();
        public StringBuilder Line9 {get;set;} = new StringBuilder();
        public StringBuilder Line10 {get;set;} = new StringBuilder();
    }
}
