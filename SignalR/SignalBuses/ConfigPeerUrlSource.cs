using System;
using System.Collections.Generic;
using System.Configuration;

namespace SignalR.SignalBuses {
    public class ConfigPeerUrlSource : IPeerUrlSource {
        public static string ConfigKey = "SignalR:HttpPeers";

        public IEnumerable<string> GetPeerUrls() {
            var settings = ConfigurationManager.AppSettings[ConfigKey];
            if (String.IsNullOrWhiteSpace(settings)) {
                throw new InvalidOperationException("");
            }

            return settings.Split(',');
        }
    }
}