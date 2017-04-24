using ConfigService.Common;
using ConfigService.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Bson.Serialization;
using System;

namespace ConfigService
{
    public static class Bootstrapper 
    {
        public static IServiceProvider _dependencies {get; private set;}
        public static void Execute() 
        {
            var services = new ServiceCollection();
            services.AddSingleton<ILogger>(new DebugLogger());
            _dependencies = services.BuildServiceProvider();
            BsonClassMap.RegisterClassMap<KvpConfig>();
        }
    }
}
