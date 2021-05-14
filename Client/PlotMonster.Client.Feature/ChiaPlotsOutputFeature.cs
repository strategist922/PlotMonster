using Microsoft.Extensions.DependencyInjection;

namespace PlotMonster.Client.Feature
{
    public static class ChiaPlotsOutoutFeature
    {
        public static void AddChiaPlotsOutputFeature(this IServiceCollection serviceCollection, Action<ChiaPlotterFeatureContextConfiguration> featureConfigDelegate)
        {
            serviceCollection.TryAddScoped<IChiaPlotOutputRepository, ChiaPlotOutputRepository>();
            serviceCollection.AddScoped<IChiaPlotOutputManager, ChiaPlotOutputManager>();
        }
    }
}