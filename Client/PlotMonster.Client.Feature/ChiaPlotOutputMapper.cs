using System;
using Core.Utiltiy.Abstraction;
using PlotMonster.Business.Abstraction;
using PlotMonster.ResourceAccess.Abstraction;

namespace PlotMonster.Client.Feature
{
    public class ChiaPlotOutputMapper : IMapper<string, ChiaPlotOutput>
    {
        // this needs to be injectable.
        // the engine should create a channel and use it for all outputs for all processes... 
        //  oh.. everything gets 2 channels, in and out.
        //  need a mapper factory that returns a new mapper per process... maybe.
        //
        //plot process resource access needs in and out channel where the in channel can get kill meesages.  now we dont need a process repo since we can kill all at once
        //       
        private ChiaPlotOutput chiaPlotOutput;
        private Func<string, ChiaPlotOutput> mapProcess;
        public ChiaPlotOutputMapper(ChiaPlotOutput chiaPlotOutput)
        {
            this.chiaPlotOutput = chiaPlotOutput;
            Func<string, ChiaPlotOutput> afterIdFoundProcess = input =>
            {
                if (input.IndexOf("Final File size: ") > -1)
                {
                    chiaPlotOutput.IsPlotComplete = true;
                }
                else if (input.IndexOf("Time for phase") > -1)
                {
                    if (input.IndexOf("phase 1") > -1)
                    {
                        
                    }
                    else if (input.IndexOf("phase 2") > -1)
                    {
                        
                    }
                    else if (input.IndexOf("phase 3") > -1)
                    {
                        
                    }
                    else if (input.IndexOf("phase 4") > -1)
                    {
                        
                    }
                }
                else if (input.IndexOf("Starting phase") > -1)
                {
                    if (input.IndexOf("phase 1") > -1)
                    {
                        chiaPlotOutput.CurrentPhase = "1";
                    }
                    else if (input.IndexOf("phase 2") > -1)
                    {
                        chiaPlotOutput.CurrentPhase = "2";
                    }
                    else if (input.IndexOf("phase 3") > -1)
                    {
                        chiaPlotOutput.CurrentPhase = "3";
                    }
                    else if (input.IndexOf("phase 4") > -1)
                    {
                        chiaPlotOutput.CurrentPhase = "4";
                    }
                }
                else if (input.IndexOf("Plot size is") > -1)
                {
                    chiaPlotOutput.KSize = input.Substring(14);
                }
                else if (input.IndexOf("Buffer size is") > -1)
                {
                    chiaPlotOutput.Ram = input.Substring(16);
                }
                else if (input.IndexOf("threads of stripe size") > -1)
                {
                    chiaPlotOutput.Threads = input.Substring(6, 2);
                }
                else if (input.IndexOf("Total time =") > -1)
                {
                    var totalTime = input.Substring(13);
                    totalTime = totalTime.Substring(0, totalTime.IndexOf(" seconds"));
                    chiaPlotOutput.TotalTime = totalTime;
                }
                else if (input.IndexOf("Copy time =") > -1)
                {
                    chiaPlotOutput.IsTransferComplete = true;
                    chiaPlotOutput.Duration = DateTime.Now.Subtract(chiaPlotOutput.StartTime);
                    var copyTime = input.Substring(12);
                    chiaPlotOutput.CopyTime = copyTime.Substring(0, copyTime.IndexOf(" seconds"));
                }
                return chiaPlotOutput;
            };
            this.mapProcess = input =>
            {
                if (string.IsNullOrEmpty(chiaPlotOutput.Id))
                {
                    if (input.IndexOf("ID:") > -1)
                    {
                        chiaPlotOutput.Id = input.Substring(4);
                        chiaPlotOutput.StartTime = DateTime.Now;
                        //chiaPlotOutput.TempDrive = chiaPlotEngineContextConfiguration.TempDrive;
                        //chiaPlotOutput.DestinationDrive = chiaPlotEngineContextConfiguration.DestinationDrive;
                        this.mapProcess = afterIdFoundProcess;
                        return chiaPlotOutput;
                    }
                    return chiaPlotOutput;
                }
                return null;
            };
        }
        public ChiaPlotOutput Map(string input)
        {
            chiaPlotOutput.Output = input;
            return mapProcess(input);
        }
    }
}