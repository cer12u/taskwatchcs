using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace TaskManager
{
    /// <summary>
    /// タスクのステータスを表す列挙型
    /// </summary>
    public enum TaskStatus
    {
        InProgress,  // 進行中
        Pending,     // 保留中
        Completed    // 完了
    }

    /// <summary>
    /// タスクの情報を管理するクラス。
    /// INotifyPropertyChangedを実装し、UIへの自動更新を提供します。
    /// </summary>
    public class TaskItem : INotifyPropertyChanged
    {
        private string name = string.Empty;
        private string memo = string.Empty;
        private TimeSpan elapsedTime;
        private TaskStatus status;

        /// <summary>
        /// タスクの一意識別子
        /// タイムスタンプベースのUUID
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// タスクの作成日時
        /// </summary>
        public DateTime CreatedAt { get; private set; }

        /// <summary>
        /// タスクの最終作業日時
        /// </summary>
        public DateTime LastWorkedAt { get; private set; }

        /// <summary>
        /// タスクの名前
        /// </summary>
        public string Name
        {
            get => name;
            set
            {
                name = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// タスクのメモ
        /// </summary>
        public string Memo
        {
            get => memo;
            set
            {
                memo = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// タスクの予定時間
        /// </summary>
        public TimeSpan EstimatedTime { get; set; }

        /// <summary>
        /// タスクの経過時間
        /// </summary>
        public TimeSpan ElapsedTime
        {
            get => elapsedTime;
            set
            {
                elapsedTime = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// タスクのステータス
        /// </summary>
        public TaskStatus Status
        {
            get => status;
            set
            {
                status = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// タスクの完了日時
        /// </summary>
        public DateTime? CompletedAt { get; private set; }

        /// <summary>
        /// プロパティ変更通知イベント
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// プロパティ変更通知を発行
        /// </summary>
        /// <param name="propertyName">変更されたプロパティ名</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// タイムスタンプベースのUUIDを生成
        /// </summary>
        private static string GenerateTimeBasedUuid()
        {
            var now = DateTime.UtcNow;
            var ticks = now.Ticks;
            var guid = Guid.NewGuid();
            return $"{ticks:x16}-{guid:N}";
        }

        /// <summary>
        /// JSONデシリアライズ用のコンストラクタ
        /// </summary>
        [JsonConstructor]
        public TaskItem()
        {
            Id = GenerateTimeBasedUuid();
            CreatedAt = DateTime.Now;
            LastWorkedAt = CreatedAt;
            Status = TaskStatus.InProgress;
        }

        /// <summary>
        /// 新規タスク作成用のコンストラクタ
        /// </summary>
        /// <param name="name">タスク名</param>
        /// <param name="memo">メモ</param>
        /// <param name="estimatedTime">予定時間</param>
        public TaskItem(string name, string memo, TimeSpan estimatedTime)
        {
            Id = GenerateTimeBasedUuid();
            Name = name;
            Memo = memo;
            EstimatedTime = estimatedTime;
            ElapsedTime = TimeSpan.Zero;
            Status = TaskStatus.InProgress;
            CreatedAt = DateTime.Now;
            LastWorkedAt = CreatedAt;
        }

        /// <summary>
        /// タスクを完了状態にする
        /// </summary>
        public void Complete()
        {
            Status = TaskStatus.Completed;
            CompletedAt = DateTime.Now;
            UpdateLastWorkedTime();
        }

        /// <summary>
        /// タスクを保留状態にする
        /// </summary>
        public void SetPending()
        {
            Status = TaskStatus.Pending;
            UpdateLastWorkedTime();
        }

        /// <summary>
        /// タスクを進行中状態にする
        /// </summary>
        public void SetInProgress()
        {
            Status = TaskStatus.InProgress;
            UpdateLastWorkedTime();
        }

        /// <summary>
        /// タスクの経過時間を追加
        /// </summary>
        /// <param name="duration">追加する時間</param>
        public void AddElapsedTime(TimeSpan duration)
        {
            ElapsedTime += duration;
            UpdateLastWorkedTime();
        }

        /// <summary>
        /// 最終作業時刻を更新
        /// </summary>
        private void UpdateLastWorkedTime()
        {
            LastWorkedAt = DateTime.Now;
        }

        /// <summary>
        /// 最後の作業から指定時間が経過しているかチェック
        /// </summary>
        /// <param name="duration">経過時間</param>
        /// <returns>指定時間が経過している場合はtrue</returns>
        public bool IsInactive(TimeSpan duration)
        {
            return DateTime.Now - LastWorkedAt > duration;
        }
    }
}