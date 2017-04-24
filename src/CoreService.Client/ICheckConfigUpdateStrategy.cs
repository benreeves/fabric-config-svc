using System;
using System.Collections.Generic;
using ConfigService.Interfaces;

namespace ConfigService.Client
{
    public interface ICheckConfigUpdateStrategy
    {
        //TODO Add support for overriding frequency checks for specific configs
        
        ///<summary>
        ///The default time period for which to check for config updates
        ///</summary>
        TimeSpan DefaultFrequency {get; set;}

        ///<summary>
        ///A list of config names for which you want to check for any updates to the latest config value
        ///</summary>
        List<string> ConfigsOfInterest {get; set; }

        ///<summary>
        ///The location where latest config values are stored
        ///</summary>
        IDictionary<string, KvpConfig> UpdateTarget {get; set;}

        void StartLookingForUpdates();
        void StopLookingForUpdates();
        void TriggerUpdates();
    }
}
