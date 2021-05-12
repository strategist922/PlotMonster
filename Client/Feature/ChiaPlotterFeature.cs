using System;
using System.Collections.Generic;
using chia_plotter.Business.Abstraction;
using chia_plotter.Business.Infrastructure;
using chia_plotter.Client.Feature;
using chia_plotter.Client.Feature.Implementation;
using chia_plotter.Utiltiy.Abstraction;
using chia_plotter.Utiltiy.Implementation;

namespace chia_plotter.Clien.Feature
{
    public static class ChiaPlotFeature
    {
        public static void AddRequiredFeatures(this string serviceCollection)
        {
            // serviceCollection.AddScoped<IMapper<string, ChiaPlotOutput>, ChiaPlotOutputMapper>();
        }

        public static void AddChiaPlotFeature(this string serviceCollection, Action<ChiaPlotterFeatureContextConfiguration> featureConfigDelegate)
        {
            var featureConfig = new ChiaPlotterFeatureContextConfiguration();
            featureConfigDelegate.Invoke(featureConfig);

            // serviceCollection.AddScoped<IChiaPlotManager>(sp => {
                if (featureConfig.SystemType == WellknownSystemType.Linux64Bit)
                {
                    return new ChiaPlotManager(
                        new ChiaPlotEngine(
                            null,
                            null,
                            tempDrive => 
                            {
                                
                            }
                        )
                    );
                }
            // });
        }
    }
}