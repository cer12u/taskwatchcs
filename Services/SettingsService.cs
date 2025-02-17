using System;
using System.IO;
using System.Text.Json;

namespace TaskManager.Services
{
    public class SettingsService
    {
        private readonly TaskLogger logger;
        private readonly ExceptionHandlingService exceptionHandler;
        private readonly Settings settings;

        public Settings Settings => settings;

        public SettingsService(TaskLogger logger)
        {
            this.logger = logger;
            this.exceptionHandler = new ExceptionHandlingService(logger);
            this.settings = Settings.Instance;
        }

        public bool AutoArchiveEnabled => settings.AutoArchiveEnabled;
        public bool InactiveTasksEnabled => settings.InactiveTasksEnabled;
        public DateTime LastResetTime => settings.LastResetTime;

        public void UpdateLastResetTime()
        {
            exceptionHandler.SafeExecute("最終リセット時刻の更新", () =>
            {
                settings.UpdateLastResetTime();
                Save();
            });
        }

        public bool NeedsReset()
        {
            return settings.NeedsReset();
        }

        public string GetArchiveFilePath(DateTime date)
        {
            return Settings.GetArchiveFilePath(date);
        }

        public void Save()
        {
            exceptionHandler.SafeExecute("設定の保存", () =>
            {
                var json = JsonSerializer.Serialize(settings);
                File.WriteAllText("settings.json", json);
            });
        }

        public void LoadSettings()
        {
            exceptionHandler.SafeExecute("設定の読み込み", () =>
            {
                if (File.Exists("settings.json"))
                {
                    var json = File.ReadAllText("settings.json");
                    var loadedSettings = JsonSerializer.Deserialize<Settings>(json);
                    if (loadedSettings != null)
                    {
                        settings.LoadFrom(loadedSettings);
                    }
                }
            });
        }

        public void UpdateSettings(bool autoArchiveEnabled, bool inactiveTasksEnabled)
        {
            exceptionHandler.SafeExecute("設定の更新", () =>
            {
                settings.AutoArchiveEnabled = autoArchiveEnabled;
                settings.InactiveTasksEnabled = inactiveTasksEnabled;
                Save();
            });
        }
    }
}