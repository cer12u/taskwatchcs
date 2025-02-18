using System;
using System.Windows.Threading;
using Microsoft.Toolkit.Uwp.Notifications;

namespace TaskManager.Services
{
    public class NotificationService
    {
        private readonly SettingsService settingsService;
        private readonly TaskLogger logger;
        private readonly ExceptionHandlingService exceptionHandler;
        private string? currentNotificationId;

        public NotificationService(
            SettingsService settingsService,
            TaskLogger logger)
        {
            this.settingsService = settingsService;
            this.logger = logger;
            this.exceptionHandler = new ExceptionHandlingService(logger);
        }

        public void ClearAllNotifications()
        {
            try
            {
                // 通知をクリア
                ToastNotificationManagerCompat.History.Clear();
                // アプリケーションと関連付けられた通知も完全にクリア
                ToastNotificationManagerCompat.Uninstall();
            }
            catch (Exception ex)
            {
                logger.LogError("通知のクリア中にエラーが発生", ex);
            }
        }

        public void ScheduleTaskNotification(TaskItem task, bool isRunning)
        {
            exceptionHandler.SafeExecute("通知のスケジュール", () =>
            {
                var settings = settingsService.GetSettings();
                if (!settings.NotificationsEnabled)
                {
                    return;
                }

                ClearCurrentNotification();

                var notificationId = Guid.NewGuid().ToString();
                currentNotificationId = notificationId;

                var builder = new ToastContentBuilder()
                    .AddText($"タスク: {task.Name}")
                    .AddText($"開始から{settings.NotificationInterval}分が経過しました。")
                    .SetToastScenario(ToastScenario.Default);

                if (settings.EstimatedTimeNotificationEnabled && task.ElapsedTime > task.EstimatedTime)
                {
                    builder.AddText($"予定時間を{(task.ElapsedTime - task.EstimatedTime).TotalMinutes:0}分超過しています。");
                }

                var notificationTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMinutes(settings.NotificationInterval)
                };

                notificationTimer.Tick += (s, e) =>
                {
                    if (isRunning)
                    {
                        builder.Show();
                        notificationTimer.Stop();
                    }
                    else
                    {
                        notificationTimer.Stop();
                    }
                };

                notificationTimer.Start();
            });
        }

        public void ClearCurrentNotification()
        {
            if (currentNotificationId != null)
            {
                ToastNotificationManagerCompat.History.Remove(currentNotificationId);
                currentNotificationId = null;
            }
        }
    }
}