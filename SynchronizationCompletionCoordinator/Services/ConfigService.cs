using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using SynchronizationCompletionCoordinator.Models;

namespace SynchronizationCompletionCoordinator.Services
{
    public static class ConfigService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        private static string ConfigPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AppConfig.ConfigFileName);

        public static AppConfigFile LoadConfig()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath);
                    var cfg = JsonSerializer.Deserialize<AppConfigFile>(json);
                    if (cfg != null) return cfg;
                }
            }
            catch { }

            return new AppConfigFile();
        }

        public static void SaveConfig(AppConfigFile config)
        {
            try
            {
                string json = JsonSerializer.Serialize(config, JsonOptions);
                File.WriteAllText(ConfigPath, json);
            }
            catch { }
        }
    }
}