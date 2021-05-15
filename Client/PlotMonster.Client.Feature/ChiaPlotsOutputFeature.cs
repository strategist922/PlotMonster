using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PlotMonster.Business.Abstraction;
using PlotMonster.Business.Implementation;
using PlotMonster.ResourceAccess.Abstraction;
using PlotMonster.ResourceAccess.Implementation;

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