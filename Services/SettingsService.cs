using System;
using System.IO;
using System.Text.Json;
using TaskManager.Models;

namespace TaskManager.Services
{
    public class SettingsService
    {
        private readonly TaskLogger logger;
        private readonly ExceptionHandlingService exceptionHandler;
        private readonly string settingsPath;
        private Settings? cachedSettings;
        private readonly JsonSerializerOptions jsonOptions;
        private static readonly TimeSpan DefaultResetCheckInterval = TimeSpan.FromMinutes(1);

        public event EventHandler? SettingsChanged;

        public SettingsService(TaskLogger logger, string? customSettingsPath = null)
        {
            this.logger = logger;
            this.exceptionHandler = new ExceptionHandlingService(logger);
            this.settingsPath = customSettingsPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
            this.jsonOptions = new JsonSerializerOptions { WriteIndented = true };

            // 設定ファイルのディレクトリを確実に作成
            Directory.CreateDirectory(Path.GetDirectoryName(settingsPath) ?? "");
        }

        public Settings GetSettings()
        {
            return exceptionHandler.ExecuteSafe("設定の読み込み", () =>
            {
                if (cachedSettings != null)
                {
                    return cachedSettings.Clone();
                }

                if (!File.Exists(settingsPath))
                {
                    cachedSettings = new Settings();
                    SaveSettings(cachedSettings);
                    return cachedSettings.Clone();
                }

                try
                {
                    var json = File.ReadAllText(settingsPath);
                    cachedSettings = JsonSerializer.Deserialize<Settings>(json) ?? new Settings();
                    return cachedSettings.Clone();
                }
                catch (Exception ex)
                {
                    logger.LogError("設定ファイルの読み込みに失敗しました", ex);
                    cachedSettings = new Settings();
                    SaveSettings(cachedSettings);
                    return cachedSettings.Clone();
                }
            });
        }

        public void SaveSettings(Settings settings)
        {
            exceptionHandler.SafeExecute("設定の保存", () =>
            {
                var json = JsonSerializer.Serialize(settings, jsonOptions);
                File.WriteAllText(settingsPath, json);
                cachedSettings = settings.Clone();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            });
        }

        public string GetArchiveFilePath(DateTime date)
        {
            var archivePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "archives"
            );
            Directory.CreateDirectory(archivePath);
            
            return Path.Combine(
                archivePath,
                $"completed_tasks_{date:yyyyMMdd}.json"
            );
        }

        public bool NeedsReset()
        {
            return exceptionHandler.ExecuteSafe("リセット状態の確認", () =>
            {
                var settings = GetSettings();
                if (settings.LastResetTime == null)
                {
                    return true;
                }

                var now = DateTime.Now;
                var lastReset = settings.LastResetTime.Value;
                var nextResetTime = lastReset.Date.Add(settings.ResetTime);

                // 前回のリセット時刻から次のリセット時刻を超えているかチェック
                if (now >= nextResetTime)
                {
                    return true;
                }

                return false;
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

        public void UpdateSetting<T>(string propertyName, T value)
        {
            exceptionHandler.SafeExecute($"設定の更新: {propertyName}", () =>
            {
                var settings = GetSettings();
                var property = typeof(Settings).GetProperty(propertyName);
                if (property != null && property.CanWrite)
                {
                    property.SetValue(settings, value);
                    SaveSettings(settings);
                }
                else
                {
                    throw new ArgumentException($"設定プロパティ '{propertyName}' が見つからないか、書き込みができません。");
                }
            });
        }

        public TimeSpan GetResetCheckInterval()
        {
            return DefaultResetCheckInterval;
        }
    }
}