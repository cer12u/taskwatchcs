using System;
using System.Collections.ObjectModel;
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

        public TaskManagerService(
            ObservableCollection<TaskItem> inProgressTasks,
            ObservableCollection<TaskItem> pendingTasks,
            ObservableCollection<TaskItem> completedTasks,
            TaskLogger logger)
        {
            this.inProgressTasks = inProgressTasks;
            this.pendingTasks = pendingTasks;
            this.completedTasks = completedTasks;
            this.logger = logger;
        }

        public TaskManagerResult RemoveTaskFromCurrentCollection(TaskItem task)
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
                return TaskManagerResult.Failed("タスクの削除中にエラーが発生しました", ex);
            }
        }

        public TaskManagerResult MoveTaskToState(TaskItem task, TaskStatus newStatus)
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
                return TaskManagerResult.Failed("タスクの状態変更中にエラーが発生しました", ex);
            }
        }

        public TaskManagerResult SaveTasks()
        {
            try
            {
                // Note: この実装はMainWindowから移動する必要があります
                return TaskManagerResult.Succeeded("タスクが正常に保存されました");
            }
            catch (Exception ex)
            {
                logger.LogTrace($"タスクの保存中にエラーが発生しました: {ex.Message}");
                return TaskManagerResult.Failed("タスクの保存中にエラーが発生しました", ex);
            }
        }
    }
}