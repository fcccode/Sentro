﻿using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Sentro.Utilities
{
    /*
        Responsibility : Hold all settings for the system
        TODO: replace with a hardcoded version insted of dynamic
    */
    class Settings
    {
        public const string Tag = "Settings";
        private static Settings _settingsInstance;
        private static FileLogger _fileLogger;
        public dynamic Setting;

        public static Settings GetInstance()
        {
            return _settingsInstance ?? (_settingsInstance = new Settings());
        }

        private Settings()
        {
            
            Reload();
            //Task.Run(() =>
            //{
            //    while (true)
            //    {
            //        Reload();                    
            //        Thread.Sleep(Convert.ToInt32(Setting.SettingsReloadRate));
            //    }
            //});
        }

        private void Reload()
        {
            try
            {
                var jsonSettings = File.ReadAllText("settings.json");
                Setting = JObject.Parse(jsonSettings);
                Setting = JsonConvert.DeserializeObject<dynamic>(jsonSettings);                
            }
            catch (Exception e)
            {
                if(_fileLogger == null)
                    _fileLogger = FileLogger.GetInstance();
                _fileLogger.Error(Tag, e.ToString());
            }
        }

        public class ARP
        {
            public uint frequency;
            public string interfaceIp;
            public bool targetAll;
            public string[] targetIps;
            public string[] doNotTarget;

        }

        public class CACHE
        {
            public string path;
        }

    }    
}
