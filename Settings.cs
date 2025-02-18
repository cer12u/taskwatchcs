using System;
using System.Text.Json.Serialization;

namespace TaskManager
{
    public class Settings
    {
        public bool InactiveTasksEnabled { get; set; } = true;
        public bool AutoArchiveEnabled { get; set; } = true;
        public DateTime? LastResetTime { get; set; }
        public bool IsTopMost { get; set; } = false;
        public TimeSpan ResetTime { get; set; } = new TimeSpan(0, 0, 0); // 00:00
        public bool NotificationsEnabled { get; set; } = true;
        public int NotificationInterval { get; set; } = 30;
        public bool EstimatedTimeNotificationEnabled { get; set; } = true;

        [JsonConstructor]
        public Settings() { }
    }
}