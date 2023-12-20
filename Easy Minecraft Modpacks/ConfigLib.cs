using System;
using System.IO;
using Newtonsoft.Json;

namespace Libraries
{
    public class ConfigLib<T> where T : class
    {
        private string ConfigCache;
        
        private System.Timers.Timer timer;

        public ConfigLib(string configPath, int RefreshInterval = 1000)
        {
            ConfigPath = configPath;

            if (!File.Exists(ConfigPath))
            {
                File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(Activator.CreateInstance(typeof(T)), Formatting.Indented));
            }

            InternalConfig = JsonConvert.DeserializeObject<T>(File.ReadAllText(ConfigPath));

            timer = new System.Timers.Timer(RefreshInterval);

            ConfigCache = JsonConvert.SerializeObject(InternalConfig);

            timer.Elapsed += (sender, e) =>
            {
                if (JsonConvert.SerializeObject(InternalConfig, Formatting.Indented) != ConfigCache)
                {
                    ConfigCache = JsonConvert.SerializeObject(InternalConfig, Formatting.Indented);

                    SaveConfig();

                    //MessageBox.Show("Saved!");
                }
            };

            timer.Enabled = true;
            timer.Start();
        }

        private string ConfigPath
        {
            get;
        }

        public T InternalConfig
        {
            get; private set;
        }

        public void SaveConfig()
        {
            File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(InternalConfig,  Formatting.Indented));
        }

        public void Dispose()
        {
            timer.Stop();
            timer.Dispose();
            timer = null;
        }
    }
}
