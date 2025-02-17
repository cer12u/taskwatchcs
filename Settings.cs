using System;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace TaskManager
{
    /// <summary>
    /// アプリケーション設定を管理するクラス
    /// </summary>
    public class Settings
    {
        private static readonly string SettingsFile = "settings.json";
        private static Settings? instance;

        /// <summary>
        /// タスクのリセット時刻（24時間形式）
        /// </summary>
        public TimeSpan ResetTime { get; set; } = new TimeSpan(0, 0, 0); // デフォルトは00:00

        /// <summary>
        /// 最後のリセット実行日時
        /// </summary>
        public DateTime LastResetTime { get; set; } = DateTime.MinValue;

        /// <summary>
        /// 通知機能の有効/無効
        /// </summary>
        public bool NotificationsEnabled { get; set; } = true;

        /// <summary>
        /// 定期通知の間隔（分）
        /// </summary>
        public int NotificationInterval { get; set; } = 30;

        /// <summary>
        /// 予定時間超過の通知を有効にするかどうか
        /// </summary>
        public bool EstimatedTimeNotificationEnabled { get; set; } = true;

        /// <summary>
        /// 自動アーカイブを有効にするかどうか
        /// </summary>
        public bool AutoArchiveEnabled { get; set; } = true;

        /// <summary>
        /// 非アクティブタスクの自動移動を有効にするかどうか
        /// </summary>
        public bool InactiveTasksEnabled { get; set; } = true;

        /// <summary>
        /// アーカイブされた完了済みタスクのファイルパスを生成
        /// </summary>
        /// <param name="date">対象日付</param>
        /// <returns>ファイルパス</returns>
        public static string GetArchiveFilePath(DateTime date)
        {
            string archiveDir = "archives";
            if (!Directory.Exists(archiveDir))
            {
                Directory.CreateDirectory(archiveDir);
            }
            return Path.Combine(archiveDir, $"completed_tasks_{date:yyyyMMdd}.json");
        }

        /// <summary>
        /// 設定のシングルトンインスタンスを取得
        /// </summary>
        public static Settings Instance
        {
            get
            {
                instance ??= new Settings();
                return instance;
            }
        }

        /// <summary>
        /// 他の設定インスタンスから値をコピー
        /// </summary>
        public void LoadFrom(Settings other)
        {
            ResetTime = other.ResetTime;
            LastResetTime = other.LastResetTime;
            NotificationsEnabled = other.NotificationsEnabled;
            NotificationInterval = other.NotificationInterval;
            EstimatedTimeNotificationEnabled = other.EstimatedTimeNotificationEnabled;
            AutoArchiveEnabled = other.AutoArchiveEnabled;
            InactiveTasksEnabled = other.InactiveTasksEnabled;
        }

        /// <summary>
        /// 次回のリセット時刻を取得
        /// </summary>
        public DateTime GetNextResetTime()
        {
            var now = DateTime.Now;
            var today = now.Date;
            var resetTimeToday = today.Add(ResetTime);

            // 現在時刻が今日のリセット時刻を過ぎている場合は翌日のリセット時刻を返す
            return now > resetTimeToday ? resetTimeToday.AddDays(1) : resetTimeToday;
        }

        /// <summary>
        /// リセットが必要かどうかを判定
        /// </summary>
        public bool NeedsReset()
        {
            var now = DateTime.Now;
            var lastResetDate = LastResetTime.Date;
            var today = now.Date;
            var resetTimeToday = today.Add(ResetTime);

            // 最後のリセットが今日のリセット時刻より前で、
            // 現在時刻が今日のリセット時刻を過ぎている場合
            return lastResetDate < today && now >= resetTimeToday;
        }

        /// <summary>
        /// 最後のリセット時刻を更新
        /// </summary>
        public void UpdateLastResetTime()
        {
            LastResetTime = DateTime.Now;
        }
    }
}