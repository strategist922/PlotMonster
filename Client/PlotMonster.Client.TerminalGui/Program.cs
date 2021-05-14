﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using chia_plotter.Business.Abstraction;
using chia_plotter.Business.Infrastructure;
using chia_plotter.ResourceAccess.Infrastructure;

namespace chia_plotter.Client
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // TODO - DI, config (IConigurationManager will be needed when we change config)
            //      input loop
            var testing = false;
            var repo = new ChiaPlotProcessRepository();
            try
            {
                var config = new ChiaPlotManagerContextConfiguration();
                config.PlotsPerDrive = 3;
                config.TempPlotDrives = new List<string>() 
                { 
                    "/chia/plottemp1",
                    "/chia/plottemp2", 
                    "/chia/plottemp3"
                };
                config.DestinationPlotDrives = new List<string>() 
                { 
                    "/chia/plots/100", 
                    "/chia/plots/101", 
                    "/chia/plots/102", 
                    "/chia/plots/103", 
                    "/chia/plots/104", 
                    "/chia/plots/105", 
                    "/chia/plots/106", 
                    "/chia/plots/107", 
                    "/chia/plots/108", 
                    "/chia/plots/109" 
                };
                config.KSizes = new List<KSizeMetadata>
                { 
                    // new KSizeMetadata { PlotSize = 408000000000, WorkSize = 1000000000000, K = "34", Threads = 8, Ram = 15000 }, 
                    new KSizeMetadata { PlotSize = 208000000000, WorkSize = 550000000000, K = "33", Threads = 4, Ram = 4000 }, 
                    new KSizeMetadata { PlotSize = 108000000000, WorkSize = 280000000000, K = "32", Threads = 2, Ram = 2000 } 
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
                        foreach (var uniqueOutput in outputs.Where(o => o.IsTransferComplete == false)) 
                        {                            
                            lmr.Add(uniqueOutput);
                                                    
                            if (lmr.Count == 3)
                            {
                                displayOutputs(lmr[0], lmr[1], lmr[2]);
                                lmr.Clear();
                            }
                        }
                        if (lmr.Count > 0) {
                            while (lmr.Count != 3) {
                                lmr.Add(new ChiaPlotOutput { Id = "..." });
                            }
                            displayOutputs(lmr[0], lmr[1], lmr[2]);
                        }
                        Console.WriteLine(string.Empty.PadRight(50 * 3, '-'), Color.BlueViolet);
                        var avg = outputs.Where(o => o.IsTransferComplete && o.Duration != default).Select(o => o.Duration);
                        var averageTime = TimeSpan.FromSeconds(avg.Any() ? avg.Average(timespan => timespan.TotalSeconds) : 0);
                        Console.WriteLine($"Completed: {outputs.Where(o => o.IsTransferComplete).Count()} plots with and average time of {averageTime.Hours}:{averageTime.Minutes}:{averageTime.Seconds}");
                        Console.WriteLine(staticTextStringBuilder.ToString());
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
                    },
                    new ChiaPlotEngine(
                        null,
                        null,
                        null,
                        (output, line) =>
                        {
                            // if (output.IsTransferComplete == true) 
                            // {
                                //TODO how does the inside get us out of this loop?
                            //     await outputChannel.Writer.WriteAsync(output);
                            //     break;
                            // }

                            output.Output = line;
                            if (string.IsNullOrEmpty(output.Id))
                            {
                                if (line.IndexOf("ID:") > -1)
                                {
                                    output.Id = line.Substring(4);
                                    return true;
                                }
                                return false;
                            }
                            if (line.IndexOf("Final File size: ") > -1)
                            {
                                output.IsPlotComplete = true;
                            }
                            else if (line.IndexOf("Time for phase") > -1)
                            {
                                if (line.IndexOf("phase 1") > -1)
                                {
                                    
                                }
                                else if (line.IndexOf("phase 2") > -1)
                                {
                                    
                                }
                                else if (line.IndexOf("phase 3") > -1)
                                {
                                    
                                }
                                else if (line.IndexOf("phase 4") > -1)
                                {
                                    
                                }
                            }
                            else if (line.IndexOf("Starting phase") > -1)
                            {
                                if (line.IndexOf("phase 1") > -1)
                                {
                                    output.CurrentPhase = "1";
                                }
                                else if (line.IndexOf("phase 2") > -1)
                                {
                                    output.CurrentPhase = "2";
                                }
                                else if (line.IndexOf("phase 3") > -1)
                                {
                                    output.CurrentPhase = "3";
                                }
                                else if (line.IndexOf("phase 4") > -1)
                                {
                                    output.CurrentPhase = "4";
                                }
                            }
                            else if (line.IndexOf("Plot size is") > -1)
                            {
                                output.KSize = line.Substring(14);
                            }
                            else if (line.IndexOf("Buffer size is") > -1)
                            {
                                output.Ram = line.Substring(16);
                            }
                            else if (line.IndexOf("threads of stripe size") > -1)
                            {
                                output.Threads = line.Substring(6, 2);
                            }
                            else if (line.IndexOf("Total time =") > -1)
                            {
                                var totalTime = line.Substring(13);
                                totalTime = totalTime.Substring(0, totalTime.IndexOf(" seconds"));
                                output.TotalTime = totalTime;
                            }
                            else if (line.IndexOf("Copy time =") > -1)
                            {
                                output.IsTransferComplete = true;
                                output.Duration = DateTime.Now.Subtract(output.StartTime);
                                var copyTime = line.Substring(12);
                                output.CopyTime = copyTime.Substring(0, copyTime.IndexOf(" seconds"));
                            }
                            return true;
                        }

                    )
                    
                );
                var runningTask = manager.Process();
                while (runningTask.IsRunning)
                {
                    // this is where we will putput and listen for input
                }
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

        private static void displayOutputs(ChiaPlotOutput left, ChiaPlotOutput center, ChiaPlotOutput right) 
        {
            var width = 50;
            var now = DateTime.Now;
            var leftTime = now.Subtract(left.StartTime);
            var centerTime = now.Subtract(center.StartTime);
            var rightTime = now.Subtract(right.StartTime);
            var leftId = !string.IsNullOrWhiteSpace(left.Id) && left.Id.Length > 10 ? left.Id.Substring(left.Id.Length - 10) : "waiting...";
            var centerId = !string.IsNullOrWhiteSpace(center.Id) && center.Id.Length > 10 ? center.Id.Substring(center.Id.Length - 10) : "waiting...";
            var rightId = !string.IsNullOrWhiteSpace(right.Id) && right.Id.Length > 10 ? right.Id.Substring(right.Id.Length - 10) : "waiting...";
            Console.WriteLine($"ID: {leftId}".PadRight(width - 1) + "|" + $"ID: {centerId}".PadRight(width - 1) + "|" + $"ID: {rightId}");
            Console.WriteLine($"KSize: {left.KSize}".PadRight(width - 1) + "|" + $"KSize: {center.KSize}".PadRight(width - 1) + "|" + $"KSize: {right.KSize}");
            Console.WriteLine($"Memory: {left.Ram}".PadRight(width - 1) + "|" + $"Memory: {center.Ram}".PadRight(width - 1) + "|" + $"Memory: {right.Ram}");
            Console.WriteLine($"Threads: {left.Threads}".PadRight(width - 1) + "|" + $"Threads: {center.Threads}".PadRight(width - 1) + "|" + $"Threads: {right.Threads}");
            Console.WriteLine($"Destination: {left.DestinationDrive}".PadRight(width - 1) + "|" + $"Destination: {center.DestinationDrive}".PadRight(width - 1) + "|" + $"Destination: {right.DestinationDrive}");
            Console.WriteLine($"Temp: {left.TempDrive}".PadRight(width - 1) + "|" + $"Temp: {center.TempDrive}".PadRight(width - 1) + "|" + $"Temp: {right.TempDrive}");
            Console.WriteLine($"Xfering: {left.IsPlotComplete}".PadRight(width - 1) + "|" + $"Xfering: {center.IsPlotComplete}".PadRight(width - 1) + "|" + $"Xfering: {right.IsPlotComplete}");
            Console.WriteLine($"Phase: {left.CurrentPhase}".PadRight(width - 1) + "|" + $"Phase: {center.CurrentPhase}".PadRight(width - 1) + "|" + $"Phase: {right.CurrentPhase}");
            Console.WriteLine($"Start: {left.StartTime.ToString("T")} ({leftTime.Hours}:{leftTime.Minutes}:{leftTime.Seconds})".PadRight(width - 1) + "|" + $"Start: {center.StartTime.ToString("T")} ({centerTime.Hours}:{centerTime.Minutes}:{centerTime.Seconds})".PadRight(width - 1) + "|" + $"Start: {right.StartTime.ToString("T")} ({rightTime.Hours}:{rightTime.Minutes}:{rightTime.Seconds})");
            Console.WriteLine(string.Empty.PadRight(width * 3, '-'), Color.BlueViolet);
            Console.WriteLine($"({leftId}): {left.Output}");
            Console.WriteLine($"({centerId}): {center.Output}");
            Console.WriteLine($"({rightId}): {right.Output}");
            Console.WriteLine(string.Empty.PadRight(width * 3, '-'), Color.BlueViolet);
        }
    }
}
