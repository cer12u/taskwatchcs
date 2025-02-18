using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace TaskManager.Services
{
    public class InactiveTaskService
    {
        private readonly ObservableCollection<TaskItem> inProgressTasks;
        private readonly ObservableCollection<TaskItem> pendingTasks;
        private readonly TaskLogger logger;
        private readonly TaskManagerService taskManager;
        private readonly ExceptionHandlingService exceptionHandler;
        private readonly SettingsService settingsService;
        private static readonly TimeSpan InactiveDuration = TimeSpan.FromHours(72);

        public InactiveTaskService(
            ObservableCollection<TaskItem> inProgressTasks,
            ObservableCollection<TaskItem> pendingTasks,
            TaskLogger logger,
            TaskManagerService taskManager,
            SettingsService settingsService)
        {
            this.inProgressTasks = inProgressTasks;
            this.pendingTasks = pendingTasks;
            this.logger = logger;
            this.taskManager = taskManager;
            this.settingsService = settingsService;
            this.exceptionHandler = new ExceptionHandlingService(logger);
        }

        public void CheckInactiveTasks()
        {
            exceptionHandler.SafeExecute("非アクティブタスクのチェック", () =>
            {
                var settings = settingsService.GetSettings();
                if (settings.InactiveTasksEnabled)
                {
                    var inactiveTasks = inProgressTasks
                        .Where(task => task.IsInactive(InactiveDuration))
                        .ToList();

                    foreach (var task in inactiveTasks)
                    {
                        inProgressTasks.Remove(task);
                        task.SetPending();
                        pendingTasks.Add(task);
                        logger.LogTaskStop(task, TimeSpan.Zero);
                    }

                    if (inactiveTasks.Any())
                    {
                        SaveTasks();
                    }
                }
            });
        }

        private void SaveTasks()
        {
            var result = taskManager.SaveTasks();
            if (!result.Success)
            {
                throw new TaskManagerException("タスクの保存に失敗しました", result.Exception);
            }
        }
    }
}