using System;
using System.IO;
using System.Text.Json;

namespace TaskManager.Services
{
    public class SettingsService
    {
        private readonly TaskLogger logger;
        private readonly ExceptionHandlingService exceptionHandler;
        private readonly string settingsPath;
        private Settings? cachedSettings;

        public SettingsService(TaskLogger logger, string? customSettingsPath = null)
        {
            this.logger = logger;
            this.exceptionHandler = new ExceptionHandlingService(logger);
            this.settingsPath = customSettingsPath ?? "settings.json";
        }

        private T ExecuteSafe<T>(string operation, Func<T> action)
        {
            try
            {
                return action();
            }
            catch (Exception ex)
            {
                exceptionHandler.HandleException(operation, ex);
                throw;
            }
        }

        public Settings GetSettings()
        {
            return ExecuteSafe("設定の読み込み", () =>
            {
                if (cachedSettings != null)
                {
                    return cachedSettings;
                }

                if (!File.Exists(settingsPath))
                {
                    cachedSettings = new Settings();
                    SaveSettings(cachedSettings);
                    return cachedSettings;
                }

                try
                {
                    var json = File.ReadAllText(settingsPath);
                    cachedSettings = JsonSerializer.Deserialize<Settings>(json) ?? new Settings();
                    return cachedSettings;
                }
                catch (Exception ex)
                {
                    logger.LogError("設定ファイルの読み込みに失敗しました", ex);
                    cachedSettings = new Settings();
                    SaveSettings(cachedSettings);
                    return cachedSettings;
                }
            });
        }

        public void SaveSettings(Settings settings)
        {
            exceptionHandler.SafeExecute("設定の保存", () =>
            {
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(settingsPath, json);
                cachedSettings = settings;
            });
        }

        public string GetArchiveFilePath(DateTime date)
        {
            return Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "archives",
                $"completed_tasks_{date:yyyyMMdd}.json"
            );
        }

        public bool NeedsReset()
        {
            return ExecuteSafe("リセット状態の確認", () =>
            {
                var settings = GetSettings();
                if (settings.LastResetTime == null)
                {
                    return true;
                }

                var now = DateTime.Now;
                var lastReset = settings.LastResetTime.Value;
                
                // 日付が変わっているかチェック
                return lastReset.Date < now.Date;
            });
        }

        public void UpdateLastResetTime()
        {
            exceptionHandler.SafeExecute("最終リセット時刻の更新", () =>
            {
                var settings = GetSettings();
                settings.LastResetTime = DateTime.Now;
                SaveSettings(settings);
            });
        }
    }
}