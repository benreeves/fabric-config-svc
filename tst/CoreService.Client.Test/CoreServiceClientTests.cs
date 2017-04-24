using Moq;
using ServiceFabric.Mocks;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using System.Threading;
using Xunit;
using Microsoft.Extensions.Logging;
using ConfigService.Interfaces;

namespace ConfigService.Client.Test
{
    public class CoreServiceClientFixture
    {
        public CoreService _service { get; private set; }
        //I know, I know. But we can't mock extension methods so I need to add the mocked services to a real DI container
        public IServiceCollection _dependencies { get; set; }
        public CoreServiceClientFixture()
        {
            var mockedLogger = new Mock<ILogger>().Object;
            var context = MockStatefulServiceContextFactory.Default;
            var stateManager = new MockReliableStateManager();
            _service = new CoreService(context, stateManager, mockedLogger);
            var dependencies = new ServiceCollection();
            dependencies.AddSingleton<ILogger>(new Mock<ILogger>().Object);
            dependencies.AddSingleton<IKvpConfigService>(this._service);
            _dependencies = dependencies;
        }
    }
    public class CoreServiceClientTests : IClassFixture<CoreServiceClientFixture>
    {
        public CoreServiceClientFixture _fixture { get; set; }
        public CoreServiceClientTests(CoreServiceClientFixture fixture)
        {
            this._fixture = fixture;
        }
        [Fact]
        public void ClientFactory_ShouldCreateConfigClient()
        {
            var configs = new List<string>(){"Test"};
            ConfigServiceClientFactory.OverrideServiceProvider(_fixture._dependencies.BuildServiceProvider());
            var client = ConfigServiceClientFactory.GetDefaultServiceClient(configs);
            Assert.NotNull(client);
        }
        [Fact]
        public void BasicUpdater_ShouldRetrieveConfigs()
        {
            KvpConfig config = new KvpConfig()
            {
                Value = "Test",
                Name = "Test",
                Type = "String",
                Version = "1.0.0"
            };
            var providerMock = new Mock<IKvpConfigService>();
            providerMock.Setup(svc => svc.GetLatestConfigSetting(It.Is<string>(x => x =="Test"))).Returns(Task<KvpConfig>.FromResult(config));
            var logger = new Mock<ILogger>().Object;
            var updateTarget = new Dictionary<string, KvpConfig>();
            var basicUpdater = new BasicConfigUpdateStrategy(providerMock.Object, logger, TimeSpan.FromHours(6), new List<string>(){"Test"},
                updateTarget);
            basicUpdater.TriggerUpdates();
            Assert.True(updateTarget.Keys.Count > 0);

        }
        [Fact]
        public void BasicUpdater_ShouldRetrieveNewerConfig()
        {
            KvpConfig older_config = new KvpConfig()
            {
                Value = "Test",
                Name = "Test",
                Type = "String",
                Version = "1.0.0"
            };
            KvpConfig config = new KvpConfig()
            {
                Value = "Test",
                Name = "Test",
                Type = "String",
                Version = "1.1.0"
            };
            var providerMock = new Mock<IKvpConfigService>();
            providerMock.Setup(svc => svc.GetLatestConfigSetting(It.Is<string>(x => x =="Test"))).Returns(Task<KvpConfig>.FromResult(config));
            var logger = new Mock<ILogger>().Object;
            var updateTarget = new Dictionary<string, KvpConfig>(){["Test"]=older_config};
            var basicUpdater = new BasicConfigUpdateStrategy(providerMock.Object, logger, TimeSpan.FromHours(6), new List<string>(){"Test"},
                updateTarget);
            basicUpdater.TriggerUpdates();
            Assert.True(updateTarget.Keys.Count == 1);
            Assert.True(updateTarget["Test"].Version == "1.1.0");

        }
        [Fact]
        public void BasicUpdater_ShouldNotThrowErrorWhenConfig_DoesNotExist()
        {
            var logger = new Mock<ILogger>().Object;
            var updateTarget = new Dictionary<string, KvpConfig>();
            var basicUpdater = new BasicConfigUpdateStrategy(_fixture._service, logger, TimeSpan.FromHours(6), new List<string>(){"DoesntExistLOL"},
                updateTarget);
            basicUpdater.TriggerUpdates();
            Assert.True(updateTarget.Keys.Count == 0);
        }
        [Fact]
        public void BasicUpdater_ShouldGetNewerConfig_FromSchedule()
        {
            KvpConfig older_config = new KvpConfig()
            {
                Value = "Test",
                Name = "Test",
                Type = "String",
                Version = "1.0.0"
            };
            KvpConfig config = new KvpConfig()
            {
                Value = "Test",
                Name = "Test",
                Type = "String",
                Version = "1.1.0"
            };
            var providerMock = new Mock<IKvpConfigService>();
            providerMock.Setup(svc => svc.GetLatestConfigSetting(It.Is<string>(x => x =="Test"))).Returns(Task<KvpConfig>.FromResult(config));
            var logger = new Mock<ILogger>().Object;
            var updateTarget = new Dictionary<string, KvpConfig>(){["Test"]=older_config};
            var basicUpdater = new BasicConfigUpdateStrategy(providerMock.Object, logger, TimeSpan.FromMilliseconds(100), new List<string>(){"Test"},
                updateTarget);
            basicUpdater.StartLookingForUpdates();
            Thread.Sleep(TimeSpan.FromMilliseconds(200));
            basicUpdater.StopLookingForUpdates();
            Assert.True(updateTarget.Keys.Count == 1);
            Assert.True(updateTarget["Test"].Version == "1.1.0");

        }
    }
}
