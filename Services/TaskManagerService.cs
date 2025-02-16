using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace TaskManager
{
    public class TaskManagerResult
    {
        public bool Success { get; }
        public string Message { get; }
        public Exception? Exception { get; }

        public TaskManagerResult(bool success, string message, Exception? exception = null)
        {
            Success = success;
            Message = message;
            Exception = exception;
        }

        public static TaskManagerResult Succeeded(string message = "操作が成功しました") 
            => new TaskManagerResult(true, message);

        public static TaskManagerResult Failed(string message, Exception? ex = null) 
            => new TaskManagerResult(false, message, ex);
    }

    public class TaskManagerService
    {
        private readonly ObservableCollection<TaskItem> inProgressTasks;
        private readonly ObservableCollection<TaskItem> pendingTasks;
        private readonly ObservableCollection<TaskItem> completedTasks;
        private readonly TaskLogger logger;
        private readonly string dataDirectory;

        public TaskManagerService(
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

        public TaskManagerResult ExecuteOperation(string operation, Func<TaskManagerResult> action)
        {
            try
            {
                logger.LogInfo($"操作開始: {operation}");
                var result = action();
                if (!result.Success)
                {
                    logger.LogWarning($"操作失敗: {operation}, 理由: {result.Message}");
                }
                return result;
            }
            catch (TaskManagerException ex)
            {
                // 既知の業務例外
                logger.LogError($"業務例外が発生: {operation}", ex);
                return TaskManagerResult.Failed(ex.Message, ex);
            }
            catch (Exception ex)
            {
                // 予期せぬシステム例外
                logger.LogError($"システム例外が発生: {operation}", ex);
                return TaskManagerResult.Failed("予期せぬエラーが発生しました。詳細はログを確認してください。", ex);
            }
        }

        public TaskManagerResult RemoveTaskFromCurrentCollection(TaskItem task)
        {
            return ExecuteOperation("タスクの削除", () =>
            {
                try
                {
                    if (task == null)
                    {
                        return TaskManagerResult.Failed("タスクが指定されていません");
                    }

                    bool removed = false;
                    switch (task.Status)
                    {
                        case TaskStatus.InProgress:
                            removed = inProgressTasks.Remove(task);
                            break;
                        case TaskStatus.Pending:
                            removed = pendingTasks.Remove(task);
                            break;
                        case TaskStatus.Completed:
                            removed = completedTasks.Remove(task);
                            break;
                    }

                    return removed 
                        ? TaskManagerResult.Succeeded("タスクが正常に削除されました")
                        : TaskManagerResult.Failed("タスクが見つかりませんでした");
                }
                catch (Exception ex)
                {
                    logger.LogTrace($"タスク削除中にエラーが発生しました: {ex.Message}");
                    throw new TaskManagerException("タスクの削除中にエラーが発生しました", ex);
                }
            });
        }

        public TaskManagerResult MoveTaskToState(TaskItem task, TaskStatus newStatus)
        {
            return ExecuteOperation("タスクの状態変更", () =>
            {
                try
                {
                    if (task == null)
                    {
                        return TaskManagerResult.Failed("タスクが指定されていません");
                    }

                    var removeResult = RemoveTaskFromCurrentCollection(task);
                    if (!removeResult.Success)
                    {
                        return removeResult;
                    }

                    var oldStatus = task.Status;
                    switch (newStatus)
                    {
                        case TaskStatus.InProgress:
                            task.SetInProgress();
                            inProgressTasks.Add(task);
                            break;
                        case TaskStatus.Pending:
                            task.SetPending();
                            pendingTasks.Add(task);
                            break;
                        case TaskStatus.Completed:
                            task.SetCompleted();
                            completedTasks.Add(task);
                            logger.LogTaskComplete(task);
                            break;
                    }

                    logger.LogTaskStateChange(task, oldStatus, newStatus);
                    var saveResult = SaveTasks();
                    return saveResult.Success 
                        ? TaskManagerResult.Succeeded("タスクの状態が正常に更新されました")
                        : TaskManagerResult.Failed($"タスクの保存に失敗しました: {saveResult.Message}");
                }
                catch (Exception ex)
                {
                    logger.LogTrace($"タスク状態の変更中にエラーが発生しました: {ex.Message}");
                    throw new TaskManagerException("タスクの状態変更中にエラーが発生しました", ex);
                }
            });
        }

        public TaskManagerResult SaveTasks()
        {
            return ExecuteOperation("タスクの保存", () =>
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
                    throw new TaskManagerException("タスクの保存中にエラーが発生しました", ex);
                }
            });
        }

        public TaskManagerResult LoadTasks()
        {
            return ExecuteOperation("タスクの読み込み", () =>
            {
                try
                {
                    var path = Path.Combine(dataDirectory, "tasks.json");
                    if (File.Exists(path))
                    {
                        var json = File.ReadAllText(path);
                        var data = JsonSerializer.Deserialize<TaskData>(json);
                        if (data != null)
                        {
                            inProgressTasks.Clear();
                            pendingTasks.Clear();
                            completedTasks.Clear();

                            if (data.InProgress != null)
                                foreach (var task in data.InProgress)
                                    inProgressTasks.Add(task);

                            if (data.Pending != null)
                                foreach (var task in data.Pending)
                                    pendingTasks.Add(task);

                            if (data.Completed != null)
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
                    throw new TaskManagerException("タスクの読み込み中にエラーが発生しました", ex);
                }
            });
        }

        private class TaskData
        {
            public TaskItem[]? InProgress { get; set; }
            public TaskItem[]? Pending { get; set; }
            public TaskItem[]? Completed { get; set; }
        }
    }

    public class TaskManagerException : Exception
    {
        public TaskManagerException(string message) : base(message) { }
        public TaskManagerException(string message, Exception innerException) 
            : base(message, innerException) { }
    }
}