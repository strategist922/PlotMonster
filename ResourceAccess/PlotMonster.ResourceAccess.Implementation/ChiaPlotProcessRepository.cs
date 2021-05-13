using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace chia_plotter.ResourceAccess.Infrastructure
{
    // we dont need this... it's up to the manager to track these processes and kill accordingly
    public class ChiaPlotProcessRepository
    {
        private List<Process> processes {get;set;}
        public ChiaPlotProcessRepository()
        {
            processes = new List<Process>();
        }

        public void Add(Process process)
        {
            processes.Add(process);
        }

        public List<Process> GetAll()
        {
            return processes;
        }
    }
}