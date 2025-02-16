using System;
using System.IO;
using System.Text.Json;
using System.Collections.ObjectModel;
using System.Linq;

namespace TaskManager.Services
{
    public class TaskPersistenceService : ITaskPersistence
    {
        private readonly ObservableCollection<TaskItem> inProgressTasks;
        private readonly ObservableCollection<TaskItem> pendingTasks;
        private readonly ObservableCollection<TaskItem> completedTasks;
        private readonly TaskLogger logger;
        private readonly string dataDirectory;

        public TaskPersistenceService(
            ObservableCollection<TaskItem> inProgressTasks,
            ObservableCollection<TaskItem> pendingTasks,
            ObservableCollection<TaskItem> completedTasks,
            TaskLogger logger,
            string dataDirectory = "data")
        {
            this.inProgressTasks = inProgressTasks;
            this.pendingTasks = pendingTasks;
            this.completedTasks = completedTasks;
            this.logger = logger;
            this.dataDirectory = dataDirectory;
            EnsureDataDirectoryExists();
        }

        public TaskManagerResult SaveTasks()
        {
            try
            {
                var allTasks = new
                {
                    InProgress = inProgressTasks,
                    Pending = pendingTasks,
                    Completed = completedTasks
                };

                var path = Path.Combine(dataDirectory, "tasks.json");
                var json = JsonSerializer.Serialize(allTasks);
                File.WriteAllText(path, json);
                logger.LogInfo("タスクが正常に保存されました");
                return TaskManagerResult.Succeeded("タスクが正常に保存されました");
            }
            catch (Exception ex)
            {
                logger.LogError("タスクの保存中にエラーが発生しました", ex);
                return TaskManagerResult.Failed("タスクの保存中にエラーが発生しました", ex);
            }
        }

        public TaskManagerResult LoadTasks()
        {
            try
            {
                var path = Path.Combine(dataDirectory, "tasks.json");
                if (File.Exists(path))
                {
                    var json = File.ReadAllText(path);
                    var data = JsonSerializer.Deserialize<TaskCollection>(json);
                    if (data != null)
                    {
                        inProgressTasks.Clear();
                        pendingTasks.Clear();
                        completedTasks.Clear();

                        foreach (var task in data.InProgress)
                            inProgressTasks.Add(task);

                        foreach (var task in data.Pending)
                            pendingTasks.Add(task);

                        foreach (var task in data.Completed)
                            completedTasks.Add(task);

                        logger.LogInfo("タスクが正常に読み込まれました");
                        return TaskManagerResult.Succeeded("タスクが正常に読み込まれました");
                    }
                }
                return TaskManagerResult.Succeeded("タスクファイルが存在しないため、新規作成します");
            }
            catch (Exception ex)
            {
                logger.LogError("タスクの読み込み中にエラーが発生しました", ex);
                return TaskManagerResult.Failed("タスクの読み込み中にエラーが発生しました", ex);
            }
        }

        public TaskManagerResult CreateBackup(DateTime timestamp)
        {
            try
            {
                var backupDir = Path.Combine(dataDirectory, "backups");
                Directory.CreateDirectory(backupDir);

                var backupPath = Path.Combine(backupDir, $"tasks_{timestamp:yyyyMMddHHmmss}.json");
                var currentPath = Path.Combine(dataDirectory, "tasks.json");

                if (File.Exists(currentPath))
                {
                    File.Copy(currentPath, backupPath, true);
                    logger.LogInfo($"バックアップを作成しました: {backupPath}");
                    return TaskManagerResult.Succeeded("バックアップが正常に作成されました");
                }

                return TaskManagerResult.Failed("バックアップ元のファイルが存在しません");
            }
            catch (Exception ex)
            {
                logger.LogError("バックアップの作成中にエラーが発生しました", ex);
                return TaskManagerResult.Failed("バックアップの作成中にエラーが発生しました", ex);
            }
        }

        public TaskManagerResult RestoreFromBackup(DateTime timestamp)
        {
            try
            {
                var backupPath = Path.Combine(dataDirectory, "backups", $"tasks_{timestamp:yyyyMMddHHmmss}.json");
                var currentPath = Path.Combine(dataDirectory, "tasks.json");

                if (!File.Exists(backupPath))
                {
                    return TaskManagerResult.Failed("指定されたバックアップファイルが存在しません");
                }

                File.Copy(backupPath, currentPath, true);
                var loadResult = LoadTasks();
                
                if (loadResult.Success)
                {
                    logger.LogInfo($"バックアップからの復元が完了しました: {backupPath}");
                    return TaskManagerResult.Succeeded("バックアップからの復元が完了しました");
                }

                return loadResult;
            }
            catch (Exception ex)
            {
                logger.LogError("バックアップからの復元中にエラーが発生しました", ex);
                return TaskManagerResult.Failed("バックアップからの復元中にエラーが発生しました", ex);
            }
        }

        public TaskManagerResult ArchiveCompletedTasks(DateTime beforeDate)
        {
            try
            {
                var archiveDir = Path.Combine(dataDirectory, "archives");
                Directory.CreateDirectory(archiveDir);

                var tasksToArchive = completedTasks.Where(t => t.CompletedAt <= beforeDate).ToList();
                if (!tasksToArchive.Any())
                {
                    return TaskManagerResult.Succeeded("アーカイブ対象のタスクが存在しません");
                }

                var archivePath = Path.Combine(archiveDir, $"completed_tasks_{beforeDate:yyyyMMdd}.json");
                var archiveData = new { CompletedTasks = tasksToArchive };
                var json = JsonSerializer.Serialize(archiveData);
                File.WriteAllText(archivePath, json);

                foreach (var task in tasksToArchive)
                {
                    completedTasks.Remove(task);
                }

                logger.LogInfo($"完了タスクをアーカイブしました: {archivePath}");
                return TaskManagerResult.Succeeded("完了タスクのアーカイブが完了しました");
            }
            catch (Exception ex)
            {
                logger.LogError("タスクのアーカイブ中にエラーが発生しました", ex);
                return TaskManagerResult.Failed("タスクのアーカイブ中にエラーが発生しました", ex);
            }
        }

        public TaskManagerResult<TaskCollection> LoadArchivedTasks(DateTime date)
        {
            try
            {
                var archivePath = Path.Combine(dataDirectory, "archives", $"completed_tasks_{date:yyyyMMdd}.json");
                if (!File.Exists(archivePath))
                {
                    return TaskManagerResult<TaskCollection>.Failed("指定された日付のアーカイブファイルが存在しません");
                }

                var json = File.ReadAllText(archivePath);
                var data = JsonSerializer.Deserialize<ArchiveData>(json);
                if (data?.CompletedTasks == null)
                {
                    return TaskManagerResult<TaskCollection>.Failed("アーカイブデータの読み込みに失敗しました");
                }

                var result = new TaskCollection
                {
                    Completed = new ObservableCollection<TaskItem>(data.CompletedTasks)
                };

                logger.LogInfo($"アーカイブされたタスクを読み込みました: {archivePath}");
                return TaskManagerResult<TaskCollection>.Succeeded(result, "アーカイブされたタスクの読み込みが完了しました");
            }
            catch (Exception ex)
            {
                logger.LogError("アーカイブされたタスクの読み込み中にエラーが発生しました", ex);
                return TaskManagerResult<TaskCollection>.Failed("アーカイブされたタスクの読み込み中にエラーが発生しました", ex);
            }
        }

        private void EnsureDataDirectoryExists()
        {
            try
            {
                if (!Directory.Exists(dataDirectory))
                {
                    Directory.CreateDirectory(dataDirectory);
                    logger.LogInfo($"データディレクトリを作成しました: {dataDirectory}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError("データディレクトリの作成に失敗しました", ex);
                throw new TaskManagerException("データディレクトリの作成に失敗しました", ex);
            }
        }

        private class ArchiveData
        {
            public TaskItem[]? CompletedTasks { get; set; }
        }
    }
}