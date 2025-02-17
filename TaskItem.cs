using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace TaskManager
{
    public class TaskValidationResult
    {
        public bool IsValid { get; }
        public string Message { get; }

        public TaskValidationResult(bool isValid, string message = "")
        {
            IsValid = isValid;
            Message = message;
        }

        public static TaskValidationResult Success() => new(true);
        public static TaskValidationResult Error(string message) => new(false, message);
    }

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
    /// タスクの優先度を表す列挙型
    /// </summary>
    public enum TaskPriority
    {
        Low,    // 低
        Medium, // 中
        High    // 高
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
        private TimeSpan estimatedTime;
        private TaskStatus status;
        private TaskPriority priority = TaskPriority.Medium;
        private bool isProcessing;

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
                var validation = ValidateName(value);
                if (!validation.IsValid)
                {
                    throw new ArgumentException(validation.Message);
                }
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
                var validation = ValidateMemo(value);
                if (!validation.IsValid)
                {
                    throw new ArgumentException(validation.Message);
                }
                memo = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// タスクの予定時間
        /// </summary>
        public TimeSpan EstimatedTime
        {
            get => estimatedTime;
            set
            {
                var validation = ValidateEstimatedTime(value);
                if (!validation.IsValid)
                {
                    throw new ArgumentException(validation.Message);
                }
                estimatedTime = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// タスクの経過時間
        /// </summary>
        public TimeSpan ElapsedTime
        {
            get => elapsedTime;
            set
            {
                var validation = ValidateElapsedTime(value);
                if (!validation.IsValid)
                {
                    throw new ArgumentException(validation.Message);
                }
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
        /// タスクの優先度
        /// </summary>
        public TaskPriority Priority
        {
            get => priority;
            set
            {
                priority = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// タスクが処理中かどうか
        /// </summary>
        public bool IsProcessing
        {
            get => isProcessing;
            set
            {
                isProcessing = value;
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
        /// <param name="priority">優先度</param>
        public TaskItem(string name, string memo = "", TimeSpan estimatedTime = default, TaskPriority priority = TaskPriority.Medium)
        {
            Id = GenerateTimeBasedUuid();
            
            var nameValidation = ValidateName(name);
            if (!nameValidation.IsValid)
            {
                throw new ArgumentException(nameValidation.Message, nameof(name));
            }

            var memoValidation = ValidateMemo(memo);
            if (!memoValidation.IsValid)
            {
                throw new ArgumentException(memoValidation.Message, nameof(memo));
            }

            var estimatedTimeValidation = ValidateEstimatedTime(estimatedTime);
            if (!estimatedTimeValidation.IsValid)
            {
                throw new ArgumentException(estimatedTimeValidation.Message, nameof(estimatedTime));
            }

            Name = name;
            Memo = memo;
            EstimatedTime = estimatedTime;
            ElapsedTime = TimeSpan.Zero;
            Status = TaskStatus.InProgress;
            Priority = priority;
            CreatedAt = DateTime.Now;
            LastWorkedAt = CreatedAt;
        }

        /// <summary>
        /// タスクの状態を完了に変更
        /// </summary>
        public void SetCompleted()
        {
            Status = TaskStatus.Completed;
            CompletedAt = DateTime.Now;
            UpdateLastWorkedTime();
        }

        /// <summary>
        /// タスクの状態を保留に変更
        /// </summary>
        public void SetPending()
        {
            Status = TaskStatus.Pending;
            UpdateLastWorkedTime();
        }

        /// <summary>
        /// タスクの状態を進行中に変更
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

        private TaskValidationResult ValidateName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return TaskValidationResult.Error("タスク名を入力してください。");
            }
            if (value.Length > 100)
            {
                return TaskValidationResult.Error("タスク名は100文字以内で入力してください。");
            }
            return TaskValidationResult.Success();
        }

        private TaskValidationResult ValidateMemo(string? value)
        {
            if (value?.Length > 1000)
            {
                return TaskValidationResult.Error("メモは1000文字以内で入力してください。");
            }
            return TaskValidationResult.Success();
        }

        private TaskValidationResult ValidateEstimatedTime(TimeSpan value)
        {
            if (value < TimeSpan.Zero)
            {
                return TaskValidationResult.Error("予定時間は0以上の値を入力してください。");
            }
            if (value > TimeSpan.FromHours(24))
            {
                return TaskValidationResult.Error("予定時間は24時間以内で入力してください。");
            }
            return TaskValidationResult.Success();
        }

        private TaskValidationResult ValidateElapsedTime(TimeSpan value)
        {
            if (value < TimeSpan.Zero)
            {
                return TaskValidationResult.Error("経過時間は0以上の値を入力してください。");
            }
            return TaskValidationResult.Success();
        }

        public TaskValidationResult Validate()
        {
            var nameValidation = ValidateName(Name);
            if (!nameValidation.IsValid)
            {
                return nameValidation;
            }

            var memoValidation = ValidateMemo(Memo);
            if (!memoValidation.IsValid)
            {
                return memoValidation;
            }

            var estimatedTimeValidation = ValidateEstimatedTime(EstimatedTime);
            if (!estimatedTimeValidation.IsValid)
            {
                return estimatedTimeValidation;
            }

            var elapsedTimeValidation = ValidateElapsedTime(ElapsedTime);
            if (!elapsedTimeValidation.IsValid)
            {
                return elapsedTimeValidation;
            }

            return TaskValidationResult.Success();
        }
    }
}