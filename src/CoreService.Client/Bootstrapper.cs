namespace ConfigService.Client
{
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.ServiceFabric.Services.Remoting.Client;
    using Microsoft.ServiceFabric.Services.Client;
    using Microsoft.Extensions.Logging;
    using ConfigService.Interfaces;
    using System;
    using ConfigService.Common;

    public static class Bootstrapper
    {
        public static void Execute()
        {
            var services = new ServiceCollection();
            IServicePartitionResolver partitionResolver = ServicePartitionResolver.GetDefault();
            services.AddSingleton<IKvpConfigService>(
                ServiceProxy.Create<IKvpConfigService>(new Uri("fabric:/ConfigService/CoreService"), new ServicePartitionKey(0))
            );
            services.AddSingleton<ILogger>(new DebugLogger());
            var provider = services.BuildServiceProvider();
            ConfigServiceClientFactory._serviceProvider = provider;
        }
        public static void Reset()
        {
            ConfigServiceClientFactory._serviceProvider = null;
        }
    }
}
