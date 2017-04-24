using System;
using ConfigService.Interfaces;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace ConfigService.Client
{
    public static class ConfigServiceClientFactory
    {
        internal static IServiceProvider _serviceProvider {get; set;}
        public const int DefaultUpdateCheckIntervalInHours = 1;
        public static void OverrideServiceProvider(IServiceProvider provider)
        {
            _serviceProvider = provider;
        }
        ///<summary>
        ///Gets a config service client which keeps track of ALL the configurations in the entire config service and 
        ///tries to hold them all in memory. You probably don't want this, but it's here
        ///</summary>
        public static ConfigServiceClient GetDefaultServiceClient(IEnumerable<string> configsOfInterest)
        {
            IKvpConfigService provider =  _serviceProvider.GetRequiredService<IKvpConfigService>();
            ILogger logger =  _serviceProvider.GetRequiredService<ILogger>();
            var configUpdateTimespan = TimeSpan.FromHours(DefaultUpdateCheckIntervalInHours);
            var updateStrategy = new BasicConfigUpdateStrategy(provider, logger, configUpdateTimespan, configsOfInterest.ToList(), null);
            return new ConfigServiceClient(provider, updateStrategy, logger);
        }
    }
}
