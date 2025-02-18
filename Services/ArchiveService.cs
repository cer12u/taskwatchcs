using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace TaskManager.Services
{
    public class ArchiveService
    {
        private readonly ObservableCollection<TaskItem> completedTasks;
        private readonly TaskLogger logger;
        private readonly TaskManagerService taskManager;
        private readonly ExceptionHandlingService exceptionHandler;
        private readonly SettingsService settingsService;

        public ArchiveService(
            ObservableCollection<TaskItem> completedTasks,
            TaskLogger logger,
            TaskManagerService taskManager,
            SettingsService settingsService)
        {
            this.completedTasks = completedTasks;
            this.logger = logger;
            this.taskManager = taskManager;
            this.settingsService = settingsService;
            this.exceptionHandler = new ExceptionHandlingService(logger);
        }

        public void CheckAndArchiveTasks()
        {
            exceptionHandler.SafeExecute("アーカイブのチェック", () =>
            {
                if (settingsService.NeedsReset())
                {
                    var settings = settingsService.GetSettings();
                    if (settings.AutoArchiveEnabled)
                    {
                        ArchiveCompletedTasks(DateTime.Now.AddDays(-1));
                    }
                    settingsService.UpdateLastResetTime();
                }
            });
        }

        private void ArchiveCompletedTasks(DateTime date)
        {
            exceptionHandler.SafeExecute("タスクのアーカイブ", () =>
            {
                var tasksToArchive = completedTasks
                    .Where(t => t.CompletedAt?.Date <= date.Date)
                    .ToList();

                if (tasksToArchive.Any())
                {
                    var archiveFile = settingsService.GetArchiveFilePath(date);
                    var json = JsonSerializer.Serialize(tasksToArchive);
                    File.WriteAllText(archiveFile, json);

                    foreach (var task in tasksToArchive)
                    {
                        completedTasks.Remove(task);
                    }

                    SaveTasks();
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