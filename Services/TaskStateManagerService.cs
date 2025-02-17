using System;
using System.Collections.Concurrent;
using System.Collections.Generic;  // 追加
using System.Collections.ObjectModel;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

// TaskStatus enumの定義がない場合は、以下の定義を追加
public enum TaskStatus
{
    InProgress,
    Pending,
    Completed
}

namespace TaskManager.Services
{
    public class TaskStateManagerService : ITaskStateManager, IDisposable
    {
        private static readonly Dictionary<string, string> ErrorMessages = new()
        {
            { "TaskNotSpecified", "タスクが指定されていません" },
            { "TaskNotFound", "タスクが見つかりませんでした" },
            { "TaskAlreadyInState", "タスクは既に指定された状態です" },
            { "TaskNotInCollection", "タスクが正しいコレクションに存在しません" },
            { "TaskRemoveFailed", "タスクの削除に失敗しました" },
            { "UnsupportedStatus", "未対応のタスク状態です: {0}" }
        };

        private readonly ObservableCollection<TaskItem> inProgressTasks;
        private readonly ObservableCollection<TaskItem> pendingTasks;
        private readonly ObservableCollection<TaskItem> completedTasks;
        private readonly TaskLogger logger;
        private TaskItem? activeTask;

        private readonly ReadOnlyObservableCollection<TaskItem> readOnlyInProgressTasks;
        private readonly ReadOnlyObservableCollection<TaskItem> readOnlyPendingTasks;
        private readonly ReadOnlyObservableCollection<TaskItem> readOnlyCompletedTasks;

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private bool _disposed;

        private readonly ConcurrentDictionary<TaskStatus, ObservableCollection<TaskItem>> _taskCollections;
        private readonly TaskStateTransition _stateTransition;

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

            _taskCollections = new ConcurrentDictionary<TaskStatus, ObservableCollection<TaskItem>>();
            _taskCollections[TaskStatus.InProgress] = inProgressTasks;
            _taskCollections[TaskStatus.Pending] = pendingTasks;
            _taskCollections[TaskStatus.Completed] = completedTasks;

            _stateTransition = new TaskStateTransition();
        }

        public ReadOnlyObservableCollection<TaskItem> InProgressTasks => readOnlyInProgressTasks;
        public ReadOnlyObservableCollection<TaskItem> PendingTasks => readOnlyPendingTasks;
        public ReadOnlyObservableCollection<TaskItem> CompletedTasks => readOnlyCompletedTasks;

        public TaskManagerResult ChangeTaskState(TaskItem task, TaskStatus newStatus)
        {
            ValidateNotDisposed();

            _semaphore.Wait();
            try
            {
                using var performance = new PerformanceScope(logger, "タスク状態の変更");
                return PerformTaskStateChange(task, newStatus);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private TaskManagerResult PerformTaskStateChange(TaskItem task, TaskStatus newStatus)
        {
            if (task == null)
            {
                return TaskManagerResult.Failed(ErrorMessages["TaskNotSpecified"]);
            }

            try
            {
                var oldStatus = task.Status;
                if (oldStatus == newStatus)
                {
                    return TaskManagerResult.Succeeded(ErrorMessages["TaskAlreadyInState"]);
                }

                if (!_stateTransition.IsTransitionAllowed(oldStatus, newStatus))
                {
                    return TaskManagerResult.Failed(_stateTransition.GetTransitionErrorMessage(oldStatus, newStatus));
                }

                var currentCollection = GetCollectionForStatus(oldStatus);
                if (!currentCollection.Contains(task))
                {
                    return TaskManagerResult.Failed(ErrorMessages["TaskNotInCollection"]);
                }

                if (!currentCollection.Remove(task))
                {
                    return TaskManagerResult.Failed(ErrorMessages["TaskRemoveFailed"]);
                }

                try
                {
                    UpdateTaskStatus(task, newStatus);
                    var newCollection = GetCollectionForStatus(newStatus);
                    newCollection.Add(task);

                    logger.LogTaskStateChange(task, oldStatus, newStatus);
                    OnTaskStateChanged(task, oldStatus, newStatus);

                    return TaskManagerResult.Succeeded("タスクの状態が正常に更新されました");
                }
                catch (Exception)
                {
                    // ロールバック
                    UpdateTaskStatus(task, oldStatus);
                    currentCollection.Add(task);
                    throw;
                }
            }
            catch (Exception ex) when (ex is not TaskStateException)
            {
                logger.LogError($"タスク状態の変更中にエラーが発生: {ex}");
                return TaskManagerResult.Failed("タスクの状態変更中にエラーが発生しました", ex);
            }
        }

        public async Task<TaskManagerResult> ChangeTaskStateAsync(
            TaskItem task, 
            TaskStatus newStatus, 
            CancellationToken cancellationToken = default)
        {
            ValidateNotDisposed();

            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                using var performance = new PerformanceScope(logger, "タスク状態の非同期変更");
                
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    return PerformTaskStateChange(task, newStatus);
                }
                catch (OperationCanceledException)
                {
                    logger.LogTrace("タスク状態の変更がキャンセルされました");
                    return TaskManagerResult.Failed("操作がキャンセルされました");
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void SetActiveTask(TaskItem task)
        {
            ValidateNotDisposed();
            _semaphore.Wait();
            try
            {
                var oldTask = activeTask;
                activeTask = task;
                
                if (oldTask != null)
                {
                    oldTask.IsProcessing = false;
                    logger.LogTrace($"アクティブタスクを解除: {oldTask.Name}");
                }
                
                if (activeTask != null)
                {
                    activeTask.IsProcessing = true;
                    logger.LogTrace($"アクティブタスクを設定: {task.Name}");
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<TaskItem?> SetActiveTaskAsync(
            TaskItem task, 
            CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var oldTask = activeTask;
                SetActiveTask(task);
                return oldTask;
            }, cancellationToken);
        }

        public TaskItem? GetActiveTask() => activeTask;

        private ObservableCollection<TaskItem> GetCollectionForStatus(TaskStatus status)
        {
            if (_taskCollections.TryGetValue(status, out var collection))
            {
                return collection;
            }
            throw new ArgumentException(string.Format(ErrorMessages["UnsupportedStatus"], status));
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

        private void ValidateNotDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(TaskStateManagerService));
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _semaphore.Dispose();
                    _taskCollections.Clear();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public class TaskStateException : Exception
        {
            public TaskStateException(string message) : base(message) { }
            public TaskStateException(string message, Exception innerException) 
                : base(message, innerException) { }
        }
    }

    internal sealed class PerformanceScope : IDisposable
    {
        private readonly TaskLogger _logger;
        private readonly string _operationName;
        private readonly Stopwatch _stopwatch;

        public PerformanceScope(TaskLogger logger, string operationName)
        {
            _logger = logger;
            _operationName = operationName;
            _stopwatch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            _logger.LogTrace($"{_operationName}の処理時間: {_stopwatch.ElapsedMilliseconds}ms");
        }
    }
}