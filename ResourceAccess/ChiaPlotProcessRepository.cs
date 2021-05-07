using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace chia_plotter.ResourceAccess.Infrastructure
{
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