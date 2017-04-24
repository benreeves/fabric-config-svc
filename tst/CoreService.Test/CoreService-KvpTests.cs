using ConfigService.Interfaces;
using ServiceFabric.Mocks;
using System;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceFabric.Data.Collections;

namespace ConfigService.Test
{
    public class CoreServiceTests : IDisposable
    {
        CoreService _service;
        public CoreServiceTests()
        {
            var mockedLogger = new Mock<ILogger>().Object;
            var context = MockStatefulServiceContextFactory.Default;
            var stateManager = new MockReliableStateManager();
            _service = new CoreService(context, stateManager, mockedLogger);
        }
        public void Dispose()
        {
            _service = null;
        }
        
        [Fact]
        public async void ShouldSaveConfig()
        {
            var dict = new MockReliableDictionary<ConfigKey, KvpConfig>();
            using (var tx = new MockTransaction())
            {
                var config = new KvpConfig()
                {
                    Value = "Test",
                    Name = "Test",
                    Type = "String",
                    Version = "1.0.0"
                };
                var key = new ConfigKey(config);
                await dict.TryAddAsync(tx, key, config);
                await tx.CommitAsync();
            }
            using (var tx = new MockTransaction())
            {
                var config = new KvpConfig()
                {
                    Value = "Test",
                    Name = "Test",
                    Type = "String",
                    Version = "1.0.0"
                };
                var key = new ConfigKey(config);
                var val = await dict.TryGetValueAsync(tx, key);
                Assert.True(val.HasValue);
                Assert.Equal(val.Value.Name, config.Name);
                Assert.Equal(val.Value.Type, config.Type);
                Assert.Equal(val.Value.Version, config.Version);
                Assert.Equal(val.Value.Value, config.Value);
            }
        }
        [Fact]
        public async void ShouldGeKvpConfigByKey()
        {
            var dict = await _service.StateManager.GetOrAddAsync<IReliableDictionary<ConfigKey, KvpConfig>>(CoreService.KvpDictionaryName);
            KvpConfig config = null;
            using (var tx = new MockTransaction())
            {
                config = new KvpConfig()
                {
                    Value = "Test",
                    Name = "Test",
                    Type = "String",
                    Version = "1.0.0"
                };
                var key = new ConfigKey(config);
                await dict.TryAddAsync(tx, key, config);
                await tx.CommitAsync();
            }
            if (await dict.GetCountAsync(new MockTransaction()) != 1) Assert.False(true, "The config key was not saved. The test will not proceed");
            var val = await _service.GetConfigSetting(new ConfigKey("Test", "1.0.0"));
            Assert.True(val != null, "The srevice returned a null response. That ain't right");
            Assert.True(val.Equals(config), "The service returned a value but it did not equal the stored config");
        }
        [Fact]
        public async void ShouldNotGetNonExistantKvpConfig()
        {
            var dict = await _service.StateManager.GetOrAddAsync<IReliableDictionary<ConfigKey, KvpConfig>>(CoreService.KvpDictionaryName);
            KvpConfig config = null;
            using (var tx = new MockTransaction())
            {
                config = new KvpConfig()
                {
                    Value = "Test",
                    Name = "Test",
                    Type = "String",
                    Version = "1.0.0"
                };
                var key = new ConfigKey(config);
                await dict.TryAddAsync(tx, key, config);
                await tx.CommitAsync();
            }
            if (await dict.GetCountAsync(new MockTransaction()) != 1) Assert.False(true, "The config key was not saved. The test will not proceed");
            var val = await _service.GetConfigSetting(new ConfigKey("NotAKey", "1.0.0"));
            Assert.True(val == null, "The service returned a response.... That ain't right");
        }
        [Fact]
        public async void ShouldNotGetKvpConfig_IfVersionDoesNotExist()
        {
            var dict = await _service.StateManager.GetOrAddAsync<IReliableDictionary<ConfigKey, KvpConfig>>(CoreService.KvpDictionaryName);
            KvpConfig config = null;
            using (var tx = new MockTransaction())
            {
                config = new KvpConfig()
                {
                    Value = "Test",
                    Name = "Test",
                    Type = "String",
                    Version = "1.0.0"
                };
                var key = new ConfigKey(config);
                await dict.TryAddAsync(tx, key, config);
                await tx.CommitAsync();
            }
            if (await dict.GetCountAsync(new MockTransaction()) != 1) Assert.False(true, "The config key was not saved. The test will not proceed");
            var val = await _service.GetConfigSetting(new ConfigKey("Test", "1.1.0"));
            Assert.True(val == null, "The service returned a response.... That ain't right");
        }
        [Fact]
        public async void ShouldAddNewConfig()
        {
            KvpConfig config = new KvpConfig()
            {
                Value = "Test",
                Name = "Test",
                Type = "String",
                Version = "1.0.0"
            };
            await _service.AddConfigSetting(config);
            var dict = await _service.StateManager.GetOrAddAsync<IReliableDictionary<ConfigKey, KvpConfig>>(CoreService.KvpDictionaryName);
            if (await dict.GetCountAsync(new MockTransaction()) != 1) Assert.False(true, "The config key was not saved. The test will not proceed");
            var val = await _service.GetConfigSetting(new ConfigKey("Test", "1.0.0"));
            Assert.True(val.Name == config.Name, "The config value saved was not equal to that passed into the service");
            Assert.True(val.Version == config.Version, "The config value saved was not equal to that passed into the service");
            Assert.True(val.Type == config.Type, "The config value saved was not equal to that passed into the service");
            Assert.True(val.Value == config.Value, "The config value saved was not equal to that passed into the service");
        }
        [Fact]
        public async void ShouldThrowInvalidOpEx_WhenConfigAlreadyExists()
        {
            var dict = await _service.StateManager.GetOrAddAsync<IReliableDictionary<ConfigKey, KvpConfig>>(CoreService.KvpDictionaryName);
            KvpConfig config = null;
            using (var tx = new MockTransaction())
            {
                config = new KvpConfig()
                {
                    Value = "Test",
                    Name = "Test",
                    Type = "String",
                    Version = "1.0.0"
                };
                var key = new ConfigKey(config);
                await dict.TryAddAsync(tx, key, config);
                await tx.CommitAsync();
            }
            KvpConfig newconfig = new KvpConfig()
            {
                Value = "Test",
                Name = "Test",
                Type = "String",
                Version = "1.0.0"
            };
            await Assert.ThrowsAnyAsync<InvalidOperationException>(() => _service.AddConfigSetting(newconfig));
        }
        [Fact]
        public async void ShouldGetLatestKvpConfigByName()
        {
            var dict = await _service.StateManager.GetOrAddAsync<IReliableDictionary<ConfigKey, KvpConfig>>(CoreService.KvpDictionaryName);
            KvpConfig config = null;
            using (var tx = new MockTransaction())
            {
                config = new KvpConfig()
                {
                    Value = "Test",
                    Name = "Test",
                    Type = "String",
                    Version = "1.1.0"
                };
                var key = new ConfigKey(config);
                await dict.TryAddAsync(tx, key, config);
                var config2 = new KvpConfig()
                {
                    Value = "Test",
                    Name = "Test",
                    Type = "String",
                    Version = "1.0.0"
                };
                var key2 = new ConfigKey(config2);
                await dict.TryAddAsync(tx, key2, config2);
                await tx.CommitAsync();
            }
            if (await dict.GetCountAsync(new MockTransaction()) != 2) Assert.False(true, "The config key was not saved. The test will not proceed");
            var val = await _service.GetLatestConfigSetting("Test");
            Assert.True(val != null, "The srevice returned a null response. That ain't right");
            Assert.True(val.Equals(config), "The service returned a value but it did not equal the stored config");
        }
    }
}
