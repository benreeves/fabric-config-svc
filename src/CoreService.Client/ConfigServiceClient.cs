using ConfigService.Interfaces;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace ConfigService.Client
{
    public class ConfigServiceClient : IKvpConfigService
    {
        internal readonly IKvpConfigService _fabricProvider;
        internal readonly ILogger _logger;
        internal readonly ICheckConfigUpdateStrategy _updateStrategy;
        private readonly ConcurrentDictionary<ConfigKey, KvpConfig> _configCache = new ConcurrentDictionary<ConfigKey, KvpConfig>();
        private readonly ConcurrentDictionary<string, KvpConfig> _latestCache = new ConcurrentDictionary<string, KvpConfig>();
        public ConfigServiceClient(IKvpConfigService fabricProvider, ICheckConfigUpdateStrategy updateStrategy, ILogger logger)
        {
            this._fabricProvider = fabricProvider;
            this._logger = logger;
            this._updateStrategy = updateStrategy;
            _updateStrategy.UpdateTarget = _latestCache;
            _updateStrategy.StartLookingForUpdates();
        }
        public async Task<KvpConfig> GetConfigSetting(ConfigKey config) 
        {
            var key = new ConfigKey(config);
            if(_configCache.ContainsKey(key))
            {
                var success =  _configCache.TryGetValue(key, out KvpConfig val);
                if(success) return val;
            }
            var kvpConfig = await _fabricProvider.GetConfigSetting(config);
            if(kvpConfig == null) return kvpConfig;
            var addSuccess = _configCache.TryAdd(key, new KvpConfig(kvpConfig));
            if(!addSuccess) 
            {
                _logger.Log(LogLevel.Warning, 0, "The given config was not added to cache", null, (val, ex) => val);
            }

            return kvpConfig;
        }
        public async Task<KvpConfig> GetLatestConfigSetting(string name) 
        {
            if(_latestCache.ContainsKey(name))
            {
                var success =  _latestCache.TryGetValue(name, out KvpConfig val);
                if(success) return val;
            }
            var kvpConfig =  await _fabricProvider.GetLatestConfigSetting(name);
            if(kvpConfig == null) return kvpConfig;
            var addSuccess = _latestCache.TryAdd(name, new KvpConfig(kvpConfig));
            if(!addSuccess) 
            {
                _logger.Log(LogLevel.Warning, 0, "The given config was not added to cache", null, (val, ex) => val);
            }
            return kvpConfig;
        }

        public async Task AddConfigSetting(KvpConfig config)
        {
            await _fabricProvider.AddConfigSetting(config);
        }
        public async Task DeleteConfigSetting(ConfigKey config)
        {
            await _fabricProvider.DeleteConfigSetting(config);
        }
        public void UpdateCachedConfigs()
        {
            this._updateStrategy.TriggerUpdates();
        }
    }
}
