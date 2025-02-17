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

        public ArchiveService(
            ObservableCollection<TaskItem> completedTasks,
            TaskLogger logger,
            TaskManagerService taskManager)
        {
            this.completedTasks = completedTasks;
            this.logger = logger;
            this.taskManager = taskManager;
        }

        public void CheckAndArchiveTasks()
        {
            var settings = Settings.Instance;
            if (settings.NeedsReset())
            {
                if (settings.AutoArchiveEnabled)
                {
                    ArchiveCompletedTasks(DateTime.Now.AddDays(-1));
                }
                settings.UpdateLastResetTime();
            }
        }

        private void ArchiveCompletedTasks(DateTime date)
        {
            try
            {
                var tasksToArchive = completedTasks
                    .Where(t => t.CompletedAt?.Date <= date.Date)
                    .ToList();

                if (tasksToArchive.Any())
                {
                    var archiveFile = Settings.GetArchiveFilePath(date);
                    var json = JsonSerializer.Serialize(tasksToArchive);
                    File.WriteAllText(archiveFile, json);

                    foreach (var task in tasksToArchive)
                    {
                        completedTasks.Remove(task);
                    }

                    SaveTasks();
                }
            }
            catch (Exception ex)
            {
                logger.LogError("タスクのアーカイブ中にエラーが発生しました", ex);
                throw;
            }
        }

        private void SaveTasks()
        {
            var result = taskManager.SaveTasks();
            if (!result.Success)
            {
                logger.LogError("タスクの保存中にエラーが発生しました", result.Exception);
                throw new InvalidOperationException("タスクの保存に失敗しました", result.Exception);
            }
        }
    }
}