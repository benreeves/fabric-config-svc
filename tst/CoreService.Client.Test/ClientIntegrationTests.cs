using System;
using System.Collections.Generic;
using Xunit;
using ConfigService.Interfaces;

namespace ConfigService.Client.Test
{
    public class ClientIntegrationTestsFixture : IDisposable
    {
        /*
           This is the button which turns on and off running these tests.
           You may ask, why would you want to not run them?
           Because it requires a cluster being deployed locally on your machine, so
           if you want to continuously run the test suite for TDD or whatnot, it's a pain to
           deal with full integration tests. Plus, it modifies external state in the backup DB,
           so if you eff something up in code you probs don't want to corrupt the external store.
           Finally, since I'm lazy and like clicking run all, this just makes them all pass.
         */
        public bool RunTheseTests = true;
        public ClientIntegrationTestsFixture()
        {
            Bootstrapper.Execute();
            BootstrapMongoDb.BootstrapDb();
        }

        public void Dispose()
        {
            Bootstrapper.Reset();
        }
    }
    public class ClientIntegrationTests : IClassFixture<ClientIntegrationTestsFixture>, IDisposable
    {
        public ClientIntegrationTestsFixture _fixture { get; set; }
        public ConfigServiceClient Client { get; set; }
        public ClientIntegrationTests(ClientIntegrationTestsFixture fixture)
        {
            this._fixture = fixture;
            this.Client = ConfigServiceClientFactory.GetDefaultServiceClient(new List<string>() {"Test"});
            //extra safety!
            if(!_fixture.RunTheseTests) Client = null;
        }
        public void Dispose() 
        {
            this.Client = null;
        }
        [Fact]
        public void ClientFactory_ShouldCreateConfigClient()
        {
            if(!_fixture.RunTheseTests) return;
            var configs = new List<string>(){"Test"};
            var client = ConfigServiceClientFactory.GetDefaultServiceClient(configs);
            Assert.NotNull(client);
        }
        [Fact]
        public async void ShouldRetrieveConfig()
        {
            if(!_fixture.RunTheseTests) return;
            KvpConfig config = new KvpConfig()
            {
                Value = "Test",
                Name = "Test",
                Type = "String",
                Version = "1.0.0"
            };
            var val = await Client.GetConfigSetting(new ConfigKey(config));
            Assert.True(val != null, "The test config was not retrieved...");
            Assert.True(val.Version == config.Version , "The retrieved value did not match expected");
            Assert.True(val.Name == config.Name , "The retrieved value did not match expected");
            Assert.True(val.Type == config.Type , "The retrieved value did not match expected");
            Assert.True(val.Value == config.Value , "The retrieved value did not match expected");
        }
        [Fact]
        public async void ShouldRetrieveLatestConfig()
        {
            if(!_fixture.RunTheseTests) return;
            var val = await Client.GetLatestConfigSetting("Test");
            Assert.True(val != null, "The test config was not retrieved...");
            Assert.True(val.Version == "1.0.0", "The retrieved value did not match expected");
            Assert.True(val.Name == "Test", "The retrieved value did not match expected");
            Assert.True(val.Type == "String", "The retrieved value did not match expected");
            Assert.True(val.Value == "Test", "The retrieved value did not match expected");
        }
        [Fact]
        public async void ShouldNot_RetrieveConfigWhichDoesntExist()
        {
            if(!_fixture.RunTheseTests) return;
            KvpConfig config = new KvpConfig()
            {
                Value = "Test2",
                Name = "sfajslkfjds",
                Type = "String",
                Version = "1.0.0"
            };
            var val = await Client.GetConfigSetting(new ConfigKey(config));
            Assert.True(val == null, "The client retrieved something....");
        }
        [Fact]
        public async void ShouldAdd_NewConfig()
        {
            if(!_fixture.RunTheseTests) return;
            KvpConfig config = new KvpConfig()
            {
                Value = "NewConfig",
                Name = "NewConfig",
                Type = "String",
                Version = "1.0.0"
            };
            await Client.AddConfigSetting(config);
            var val = await Client.GetConfigSetting(new ConfigKey(config));
            Assert.True(val != null, "The client didnt get the new config....");

            //Cleanup -- reset all internal and external state
            await Client.DeleteConfigSetting(new ConfigKey(config));
        }
        [Fact]
        public async void ShouldDeleteConfig()
        {
            if(!_fixture.RunTheseTests) return;
            KvpConfig config = new KvpConfig()
            {
                Value = "Test",
                Name = "Test",
                Type = "String",
                Version = "1.0.0"
            };
            await Client.DeleteConfigSetting(new ConfigKey("Test", "1.0.0"));
            var val = await Client.GetConfigSetting(new ConfigKey("Test", "1.0.0"));
            Assert.True(val == null, "The client didnt delete the config....");

            //Cleanup -- reset all internal and external state
            await Client.AddConfigSetting(config);
        }
        [Fact]
        public async void ShouldAdd_IncrementedVersion()
        {
            if(!_fixture.RunTheseTests) return;
            KvpConfig config = new KvpConfig()
            {
                Value = "new test",
                Name = "Test",
                Type = "String",
                Version = "1.1.0"
            };
            await Client.AddConfigSetting(config);
            var val = await Client.GetConfigSetting(new ConfigKey(config));
            Assert.True(val != null, "The client didnt get the new config....");

            var old_val = await Client.GetConfigSetting(new ConfigKey("Test", "1.0.0"));
            Assert.True(old_val != null, "The old configuration key doesn't exist...");
            Assert.True(old_val.Value != val.Value, "We added a new config but didn't update the value....");
            Assert.True(val.Value == "new test", "We added a new config but didn't update the value....");

            //Cleanup -- reset all internal and external state
            await Client.DeleteConfigSetting(new ConfigKey(config));
        }
    }
}
