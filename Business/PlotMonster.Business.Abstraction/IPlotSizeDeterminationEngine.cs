namespace PlotMonster.Business.Abstraction
{
    public interface IPlotSizeDeterminationEngine
    {
        Task<string> DeterminePlotSizeAsync(string tempDrive, string destDrive, CancellationToken cancellationToken);
    }
}