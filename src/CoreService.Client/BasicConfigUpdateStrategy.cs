using System;
using System.Collections.Generic;
using ConfigService.Interfaces;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ConfigService.Client
{
    public class BasicConfigUpdateStrategy : ICheckConfigUpdateStrategy
    {
        private readonly IKvpConfigService _provider;
        private readonly ILogger _logger;
        private System.Threading.Timer _timer;
        public BasicConfigUpdateStrategy(IKvpConfigService provider, ILogger logger)
        {
            this.DefaultFrequency = (DefaultFrequency == default(TimeSpan)) ? TimeSpan.FromHours(1) : DefaultFrequency;
            this.ConfigsOfInterest = ConfigsOfInterest ?? new List<string>();
            this.UpdateTarget = UpdateTarget ?? new Dictionary<string, KvpConfig>();
            this._provider = provider;
            this._logger = logger;
        }
        public BasicConfigUpdateStrategy(IKvpConfigService provider, ILogger logger, TimeSpan defaultFrequency, List<string> configsOfInterest,
                IDictionary<string, KvpConfig> updateTarget) : this(provider, logger)
        {
            this.DefaultFrequency = defaultFrequency;
            this.ConfigsOfInterest = configsOfInterest;
            this.UpdateTarget = updateTarget;
        }

        
        ///<summary>
        ///The default time period for which to check for config updates
        ///</summary>
        public TimeSpan DefaultFrequency {get; set;}
        ///<summary>
        ///A list of config names for which you want to check for any updates to the latest config value
        ///</summary>
        public List<string> ConfigsOfInterest {get; set; }

        ///<summary>
        ///The location where latest config values are stored
        ///</summary>
        public IDictionary<string, KvpConfig> UpdateTarget {get; set;}

        public void StartLookingForUpdates() 
        {
            _timer = new System.Threading.Timer((e) =>
            {
                TriggerUpdates();
            }, null, TimeSpan.Zero, DefaultFrequency);

        }
        public void StopLookingForUpdates()
        {
            _timer = null;
        }

        public void TriggerUpdates()
        {
            Task[] jobs = new Task[ConfigsOfInterest.Count];
            for(var i = 0; i < ConfigsOfInterest.Count; i ++)
            {
                jobs[i] = UpdateLatestConfig(ConfigsOfInterest[i]);
            }
            try 
            {
                bool success = Task.WaitAll(jobs, TimeSpan.FromMinutes(2));
                if(!success) _logger.Log(LogLevel.Warning, 0, $"The class {nameof(BasicConfigUpdateStrategy)} was unable to update the configurations within 2 minutes", null, (msg, ex) => msg);

            }
            catch(AggregateException ex) 
            {
                 _logger.Log(LogLevel.Error, 0, $"One or more exceptions occured while processing updates: {0}", ex, (msg, exc) => String.Format(msg, exc));
            }

        }

        private async Task UpdateLatestConfig(string name) 
        {
            var val = await _provider.GetLatestConfigSetting(name);
            if (val == null)
                _logger.Log(LogLevel.Warning, 0, $"The config {name} was not found in the config service. Skipping...", null, (msg, exc) => msg);
            else
                this.UpdateTarget[name] = new KvpConfig(val);
        }
    }
}
