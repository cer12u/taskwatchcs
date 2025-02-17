using System;
using System.Collections.ObjectModel;

namespace TaskManager.Services
{
    /// <summary>
    /// タスクの状態管理を担当するインターフェース
    /// </summary>
    public interface ITaskStateManager
    {
        /// <summary>
        /// タスクの状態を変更
        /// </summary>
        TaskManagerResult ChangeTaskState(TaskItem task, TaskStatus newStatus);

        /// <summary>
        /// 進行中タスクの取得
        /// </summary>
        ReadOnlyObservableCollection<TaskItem> InProgressTasks { get; }

        /// <summary>
        /// 保留中タスクの取得
        /// </summary>
        ReadOnlyObservableCollection<TaskItem> PendingTasks { get; }

        /// <summary>
        /// 完了タスクの取得
        /// </summary>
        ReadOnlyObservableCollection<TaskItem> CompletedTasks { get; }

        /// <summary>
        /// タスクの状態変更イベント
        /// </summary>
        event EventHandler<TaskStateChangedEventArgs> TaskStateChanged;

        /// <summary>
        /// アクティブなタスクの設定
        /// </summary>
        void SetActiveTask(TaskItem task);

        /// <summary>
        /// アクティブなタスクの取得
        /// </summary>
        TaskItem? GetActiveTask();
    }

    public class TaskStateChangedEventArgs : EventArgs
    {
        public TaskItem Task { get; }
        public TaskStatus OldStatus { get; }
        public TaskStatus NewStatus { get; }

        public TaskStateChangedEventArgs(TaskItem task, TaskStatus oldStatus, TaskStatus newStatus)
        {
            Task = task;
            OldStatus = oldStatus;
            NewStatus = newStatus;
        }
    }
}