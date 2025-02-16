using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace TaskManager.Services
{
    public class TaskStateManagerService : ITaskStateManager
    {
        private readonly ObservableCollection<TaskItem> inProgressTasks;
        private readonly ObservableCollection<TaskItem> pendingTasks;
        private readonly ObservableCollection<TaskItem> completedTasks;
        private readonly TaskLogger logger;
        private TaskItem? activeTask;

        private readonly ReadOnlyObservableCollection<TaskItem> readOnlyInProgressTasks;
        private readonly ReadOnlyObservableCollection<TaskItem> readOnlyPendingTasks;
        private readonly ReadOnlyObservableCollection<TaskItem> readOnlyCompletedTasks;

        public event EventHandler<TaskStateChangedEventArgs>? TaskStateChanged;

        public TaskStateManagerService(
            ObservableCollection<TaskItem> inProgressTasks,
            ObservableCollection<TaskItem> pendingTasks,
            ObservableCollection<TaskItem> completedTasks,
            TaskLogger logger)
        {
            this.inProgressTasks = inProgressTasks;
            this.pendingTasks = pendingTasks;
            this.completedTasks = completedTasks;
            this.logger = logger;

            readOnlyInProgressTasks = new ReadOnlyObservableCollection<TaskItem>(inProgressTasks);
            readOnlyPendingTasks = new ReadOnlyObservableCollection<TaskItem>(pendingTasks);
            readOnlyCompletedTasks = new ReadOnlyObservableCollection<TaskItem>(completedTasks);
        }

        public ReadOnlyObservableCollection<TaskItem> InProgressTasks => readOnlyInProgressTasks;
        public ReadOnlyObservableCollection<TaskItem> PendingTasks => readOnlyPendingTasks;
        public ReadOnlyObservableCollection<TaskItem> CompletedTasks => readOnlyCompletedTasks;

        public TaskManagerResult ChangeTaskState(TaskItem task, TaskStatus newStatus)
        {
            if (task == null)
            {
                return TaskManagerResult.Failed("タスクが指定されていません");
            }

            try
            {
                var oldStatus = task.Status;
                var currentCollection = GetCollectionForStatus(oldStatus);
                
                if (!currentCollection.Remove(task))
                {
                    return TaskManagerResult.Failed("タスクが見つかりませんでした");
                }

                UpdateTaskStatus(task, newStatus);
                var newCollection = GetCollectionForStatus(newStatus);
                newCollection.Add(task);

                logger.LogTaskStateChange(task, oldStatus, newStatus);
                OnTaskStateChanged(task, oldStatus, newStatus);

                return TaskManagerResult.Succeeded("タスクの状態が正常に更新されました");
            }
            catch (Exception ex)
            {
                logger.LogTrace($"タスク状態の変更中にエラーが発生しました: {ex.Message}");
                return TaskManagerResult.Failed("タスクの状態変更中にエラーが発生しました", ex);
            }
        }

        public void SetActiveTask(TaskItem task)
        {
            var oldTask = activeTask;
            activeTask = task;
            
            if (oldTask != null)
            {
                oldTask.IsProcessing = false;
            }
            
            if (activeTask != null)
            {
                activeTask.IsProcessing = true;
                logger.LogTrace($"アクティブタスクを設定: {task.Name}");
            }
        }

        public TaskItem? GetActiveTask() => activeTask;

        private ObservableCollection<TaskItem> GetCollectionForStatus(TaskStatus status)
        {
            return status switch
            {
                TaskStatus.InProgress => inProgressTasks,
                TaskStatus.Pending => pendingTasks,
                TaskStatus.Completed => completedTasks,
                _ => throw new ArgumentException($"未対応のタスク状態です: {status}")
            };
        }

        private void UpdateTaskStatus(TaskItem task, TaskStatus newStatus)
        {
            switch (newStatus)
            {
                case TaskStatus.InProgress:
                    task.SetInProgress();
                    break;
                case TaskStatus.Pending:
                    task.SetPending();
                    break;
                case TaskStatus.Completed:
                    task.SetCompleted();
                    break;
                default:
                    throw new ArgumentException($"未対応のタスク状態です: {newStatus}");
            }
        }

        private void OnTaskStateChanged(TaskItem task, TaskStatus oldStatus, TaskStatus newStatus)
        {
            TaskStateChanged?.Invoke(this, new TaskStateChangedEventArgs(task, oldStatus, newStatus));
        }
    }
}