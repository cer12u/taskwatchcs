using System;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

namespace TaskManager.Services
{
    /// <summary>
    /// タスクデータの永続化を担当するインターフェース
    /// </summary>
    public interface ITaskPersistence
    {
        /// <summary>
        /// タスクデータの保存
        /// </summary>
        TaskManagerResult SaveTasks();

        /// <summary>
        /// タスクデータの読み込み
        /// </summary>
        TaskManagerResult LoadTasks();

        /// <summary>
        /// バックアップの作成
        /// </summary>
        TaskManagerResult CreateBackup(DateTime timestamp);

        /// <summary>
        /// バックアップからの復元
        /// </summary>
        TaskManagerResult RestoreFromBackup(DateTime timestamp);

        /// <summary>
        /// 完了タスクのアーカイブ
        /// </summary>
        TaskManagerResult ArchiveCompletedTasks(DateTime beforeDate);

        /// <summary>
        /// アーカイブされたタスクの取得
        /// </summary>
        TaskManagerResult<TaskCollection> LoadArchivedTasks(DateTime date);
    }

    /// <summary>
    /// タスクコレクションのデータ構造
    /// </summary>
    public class TaskCollection
    {
        public ObservableCollection<TaskItem> InProgress { get; set; } = new();
        public ObservableCollection<TaskItem> Pending { get; set; } = new();
        public ObservableCollection<TaskItem> Completed { get; set; } = new();
    }

    /// <summary>
    /// ジェネリック型のTaskManagerResult
    /// </summary>
    public class TaskManagerResult<T> : TaskManagerResult
    {
        public T? Data { get; }

        private TaskManagerResult(bool success, string message, T? data = default, Exception? exception = null)
            : base(success, message, exception)
        {
            Data = data;
        }

        public static TaskManagerResult<T> Succeeded(T data, string message = "操作が成功しました")
            => new(true, message, data);

        public new static TaskManagerResult<T> Failed(string message, Exception? ex = null)
            => new(false, message, default, ex);
    }
}