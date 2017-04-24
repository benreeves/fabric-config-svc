using System;
using System.Collections.Generic;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.Extensions.Logging;
using ConfigService.Interfaces;
using Microsoft.ServiceFabric.Data;
using MongoDB.Driver;
using MongoDB.Bson;

namespace ConfigService
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    public sealed class CoreService : StatefulService, IKvpConfigService
    {
        public const string KvpDictionaryName = "KvpDictionary";
        public static string _backupCxnString { get; private set; }
        public static string _backupDbName { get; private set; }
        public static string _backupKvpCollection { get; private set; }
        private readonly ILogger _logger;
        private static object _lockObj = new object();
        private static MongoClient _backupDbClient;
        private MongoClient DbClient
        {
            get
            {
                if (_backupDbClient == null)
                {
                    lock (_lockObj)
                        if (_backupDbClient == null)
                            _backupDbClient = new MongoClient(_backupCxnString);
                }
                return _backupDbClient;
            }
        }

        public CoreService(StatefulServiceContext context, ILogger logger)
            : base(context)
        {
            this._logger = logger;
            var config = context.CodePackageActivationContext.GetConfigurationPackageObject("Config");
            var section = config.Settings.Sections["AppSettings"];
            _backupCxnString = section.Parameters["BackupStoreCxn"].Value;
            _backupDbName = section.Parameters["BackupDbName"].Value;
            _backupKvpCollection = section.Parameters["BackupKvpCollection"].Value;
        }
        /// <summary>
        /// This constructor is currently only used for testing purposes
        /// </summary>
        /// <param name="context"></param>
        /// <param name="reliableStateManagerReplica"></param>
        public CoreService(StatefulServiceContext context, IReliableStateManagerReplica reliableStateManagerReplica, ILogger logger)
            : base(context, reliableStateManagerReplica)
        {
            this._logger = logger;
            var config = context.CodePackageActivationContext.GetConfigurationPackageObject("Config");
            var section = config?.Settings.Sections["AppSettings"];
            _backupCxnString = section?.Parameters["BackupStoreCxn"].Value ?? "mongodb://benreeves:4ExhPj2MD1la@ds111791.mlab.com:11791/config-svc-backup";
            _backupDbName = section?.Parameters["BackupDbName"].Value ?? "config-svc-backup";
            _backupKvpCollection = section?.Parameters["BackupKvpCollection"].Value ?? "kvp-settings";
        }
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new List<ServiceReplicaListener>() {
                new ServiceReplicaListener((context) =>
                        this.CreateServiceRemotingListener(context))
            };
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {

            var kvpDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<ConfigKey, KvpConfig>>(KvpDictionaryName);
            long result = 0;
            using (var tx = this.StateManager.CreateTransaction())
            {
                //do we have any values? Should we get from backup?
                result = await kvpDictionary.GetCountAsync(tx);
            }

            //get from backup nosql store
            if (result == 0)
            {
                ServiceEventSource.Current.ServiceMessage(this.Context, "No config values found, updating from backup: {0}", DateTime.UtcNow);
                var db = DbClient.GetDatabase(_backupDbName);
                var kvpCollection = db.GetCollection<BsonDocument>(_backupKvpCollection);
                var kvps = await kvpCollection.Find(new BsonDocument()).ToListAsync();
                //var temp = await kvpCollection.CountAsync(new BsonDocument());
                foreach (var config in kvps)
                {
                    using (var tx = this.StateManager.CreateTransaction())
                    {

                        var name = config.GetValue("Name").AsString;
                        var version = config.GetValue("Version").AsString;
                        var type = config.GetValue("Type").AsString;
                        var value = config.GetValue("Value").AsString;
                        var x = new KvpConfig()
                        {
                            Name = name,
                            Version = version,
                            Type = type,
                            Value = value
                        };
                        var key = new ConfigKey(name, version);
                        await kvpDictionary.AddAsync(tx, key, x);
                        await tx.CommitAsync();
                    }
                }
            }
        }

        public async Task<KvpConfig> GetConfigSetting(ConfigKey config)
        {
            var kvpDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<ConfigKey, KvpConfig>>(KvpDictionaryName);
            using (var tx = this.StateManager.CreateTransaction())
            {
                var configValue = await kvpDictionary.TryGetValueAsync(tx, new ConfigKey(config));
                if (configValue.HasValue) return configValue.Value;
                return null;
            }
        }
        public async Task DeleteConfigSetting(ConfigKey config)
        {
            var kvpDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<ConfigKey, KvpConfig>>(KvpDictionaryName);
            using (var tx = this.StateManager.CreateTransaction())
            {
                var configValue = await kvpDictionary.TryGetValueAsync(tx, new ConfigKey(config));
                if (configValue.HasValue)
                {
                    var success = await kvpDictionary.TryRemoveAsync(tx, new ConfigKey(config));
                    if (success.HasValue)
                    {
                        await tx.CommitAsync();
                        _logger.Log(LogLevel.Information, 0, $"Config {config.Name} v {config.Version} was removed from the config service",
                                null, (msg, ex) => msg);
                        Task.Run(async () => await this.RemoveConfigFromBackup(config));
                    }

                }
            }
        }

        public async Task AddConfigSetting(KvpConfig config)
        {
            var kvpDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<ConfigKey, KvpConfig>>(KvpDictionaryName);
            bool saved = false;
            using (var tx = this.StateManager.CreateTransaction())
            {
                var configValue = await kvpDictionary.TryGetValueAsync(tx, new ConfigKey(config));
                if (configValue.HasValue)
                {
                    string msg = $"You are trying to add a config for which the SemVer you specified already exists: {config.Name}, {config.Version}. That is not allowed";
                    _logger.Log(LogLevel.Error, 0, msg, null, (ms, ex) => ms);
                    tx.Abort();
                    throw new InvalidOperationException(msg);
                }
                else
                {
                    await kvpDictionary.AddAsync(tx, new ConfigKey(config), new KvpConfig(config));
                    await tx.CommitAsync();
                    saved = true;
                }
            }
            if (saved)
            {
                Task.Run(async () => await this.BackupConfigSetting(config));
            }

        }

        public async Task<KvpConfig> GetLatestConfigSetting(string name)
        {
            var kvpDictionary = await this.StateManager.GetOrAddAsync<IReliableDictionary<ConfigKey, KvpConfig>>(KvpDictionaryName);
            KvpConfig config = null;
            using (var tx = this.StateManager.CreateTransaction())
            {
                var asyncEnumerable = await kvpDictionary.CreateEnumerableAsync(tx);
                var kvps = asyncEnumerable.GetAsyncEnumerator();
                SemVer greatestRelease = null;
                while (await kvps.MoveNextAsync(new CancellationToken()))
                {
                    var kvp = kvps.Current;
                    if (kvp.Key.Name != name) continue;
                    if (greatestRelease == null) greatestRelease = new SemVer(kvp.Key.Version);
                    var version = new SemVer(kvp.Key.Version);
                    if (greatestRelease < version)
                    {
                        greatestRelease = version;
                        config = (KvpConfig)kvp.Value;
                    }
                }
            }
            return config;
        }

        ///<summary>
        ///Backs up config settings to mongodb store. Currently abandons all hope of saving if an exception is thrown. that's what 
        ///stateful services are for anyways right? lol
        ///</summary>
        //TODO add some way to add failures to a queue of shit that needs to be backed up
        private async Task BackupConfigSetting(KvpConfig config)
        {
            var db = DbClient.GetDatabase(_backupDbName);
            var collection = db.GetCollection<BsonDocument>(_backupKvpCollection);
            var toInsert = new BsonDocument
            {
                {"Type" , config.Type},
                {"Value" , config.Value},
                {"Name" , config.Name},
                {"Version" , config.Version},
            };

            try
            {
                var exists = await collection.FindAsync(toInsert);
                if (exists != null)
                {
                    _logger.Log(LogLevel.Warning, 0, $"Attempted to backup a config which already exists {config.Name}: {config.Value}. Aborting",
                            null, (msg, exx) => msg);
                    return;
                }
                await collection.InsertOneAsync(toInsert);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, 0, "Exception of type {0} occured while saving config to backup: {1} | {2}", ex,
                        (msg, exx) => String.Format(msg, exx.Message, exx.Source, exx.StackTrace));
            }
        }
        ///<summary>
        ///Backs up config settings to mongodb store. Currently abandons all hope of saving if an exception is thrown. that's what 
        ///stateful services are for anyways right? lol
        ///</summary>
        //TODO add some way to add failures to a queue of shit that needs to be backed up
        private async Task RemoveConfigFromBackup(ConfigKey config)
        {
            var db = DbClient.GetDatabase(_backupDbName);
            var collection = db.GetCollection<BsonDocument>(_backupKvpCollection);
            var toDelete = new BsonDocument
            {
                {"Name" , config.Name},
                {"Version" , config.Version},
            };

            try
            {
                var exists = await collection.FindAsync(toDelete);
                if (exists == null)
                {
                    _logger.Log(LogLevel.Warning, 0, $"Attempted to remove a config from backup which does not exists {config.Name}: {config.Version}. Aborting",
                            null, (msg, exx) => msg);
                    return;
                }
                await collection.DeleteOneAsync(toDelete);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, 0, "Exception of type {0} occured while saving config to backup: {1} | {2}", ex,
                        (msg, exx) => String.Format(msg, exx.Message, exx.Source, exx.StackTrace));
            }
        }
    }
}
