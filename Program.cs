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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using NLog.Extensions.Logging;
using NLog;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace chia_plotter
{
    class Program
    {
        private static IServiceProvider ConfigureServices(IServiceCollection services)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(System.IO.Directory.GetCurrentDirectory())
                .AddJsonFile("plot-settings.json", optional: false, reloadOnChange: true)
                .Build();
            services.Configure<ChiaPlotManagerContextConfiguration>(config.GetSection("PlotConfiguration"));
            services.AddSingleton<IConfiguration>(config);
            services.AddLogging(builder => {
                builder.AddNLog("NLog.config");
            });
            services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
            var serviceProvider = services.BuildServiceProvider(); 

            return serviceProvider;            
        }
        
        static async Task Main(string[] args)
        {
            var testing = false;

            var serviceCollection = new ServiceCollection();
            var serviceProvider = ConfigureServices(serviceCollection);
            var programLogger = serviceProvider.GetRequiredService<ILogger<Program>>();
            var chiaPlotsManagerLogger = serviceProvider.GetRequiredService<ILogger<ChiaPlotsManager>>();
            var baseDir = Directory.GetCurrentDirectory();
            var config = serviceProvider.GetRequiredService<IOptions<ChiaPlotManagerContextConfiguration>>().Value;
            
            var repo = new ChiaPlotProcessRepository();
            try
            {
                var manager = new ChiaPlotsManager(
                    config, 
                    repo, 
                    (
                        temp,
                        destination,
                        K,
                        Ram,
                        Threads,
                        processRepo,
                        farmerPublicKey,
                        poolPublicKey
                    ) => {
                        if (!testing) {
                            return new ChiaPlotProcessChannel(temp, destination, K, Ram, Threads, processRepo, farmerPublicKey, poolPublicKey);
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
                        foreach (var uniqueOutput in outputs.Where(o => o.IsTransferComplete == false).OrderBy(o => o.CurrentPhase)) 
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
                  
                        var avg = outputs.Where(o => o.IsTransferComplete && o.Duration != default).Select(o => o.Duration);
                        var averageTime = TimeSpan.FromSeconds(avg.Any() ? avg.Average(timespan => timespan.TotalSeconds) : 0);
                        var skippedTempDrive = outputs.Where(o => o.InvalidDrive == o.TempDrive && o.TempDrive != o.DestinationDrive);
                        programLogger.LogInformation($"Completed: {outputs.Where(o => o.IsTransferComplete).Count()} plots with an average time of {averageTime.Hours}:{averageTime.Minutes}:{averageTime.Seconds}");
                        programLogger.LogInformation($"Skipped {skippedTempDrive.Count()} temp drive{(skippedTempDrive.Count() != 1 ? "s" : string.Empty)}.");
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
                            programLogger.LogError(ex, "Clean trash exception");
                        }
                        return Task.CompletedTask;
                    },
                    chiaPlotsManagerLogger
                );
                await manager.Process();
            }
            catch(Exception ex) 
            {
                Console.WriteLine($"Exception");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                programLogger.LogError(ex, "Main exception");
            }
            finally
            {
                Console.WriteLine("==================");
                var processes = repo.GetAll();
                foreach(var process in processes)
                {
                    try
                    {
                        if (process.HasExited)
                        {
                            continue;
                        }
                        Console.WriteLine($"Killing process {process.Id}");
                        
                        process.Kill(true);
                        process.Close();
                    }
                    catch(InvalidOperationException)
                    {
                        
                    }
                    
                }
                Console.WriteLine("Done... press any key to exit");
                Console.ReadLine();
            }
        }

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
            displayBuilder.Line7.Append($"Start: {chiaPlotOutput.StartTime.ToString("T")}".PadRight(width - 1) + "|");

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
