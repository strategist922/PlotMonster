using System;
using System.Collections.Generic;
using Core.Business.Implementation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Core.Utiltiy.Abstraction;
using PlotMonster.Business.Abstraction;
using PlotMonster.ResourceAccess.Abstraction;
using PlotMonster.ResourceAccess.Implementation;

namespace PlotMonster.Client.Feature
{
    public static class ChiaPlotFeature
    {
        public static void AddChiaPlotFeature(this IServiceCollection serviceCollection, Action<ChiaPlotterFeatureContextConfiguration> featureConfigDelegate)
        {
            serviceCollection.TryAddScoped<IMapper<string, ChiaPlotOutput>, ChiaPlotOutputMapper>();
            serviceCollection.TryAddScoped<IChiaPlotOutputRepository, ChiaPlotOutputRepository>();

            var featureConfig = new ChiaPlotterFeatureContextConfiguration();
            featureConfigDelegate.Invoke(featureConfig);

            serviceCollection.AddScoped<IChiaPlotManager>(sp => {
                var ignoredDrives = new List<string>();
                if (featureConfig.SystemType == WellknownSystemType.Linux64Bit)
                {
                    return new ChiaPlotManager(
                        // just rules to check if a new plot process should start
                        // at this point, we only care about the list of outputs
                        // this should return a ChiaPlotOutput that should be started.
                        new RulesEngine<ChiaPlotOutput>(
                            // @TODO - add rules
                            // outputs => 
                            // {  // this is used to determine how it starts
                            //     return !ignoredDrives.Any(d => d == output.DestinationDrive || d == output.TempDrive);
                            // },
                            outputs => 
                            {

                            }
                        ),
                        new ChiaPlotOutputRepository(

                        ),
                        new PlotSizeDeterminationEngine
                        {

                        },
                        new ProcessResourceAccess
                        {

                        }
                    );
                }
            });
        }
    }
}