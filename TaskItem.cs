using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace TaskManager
{
    /// <summary>
    /// タスクの情報を管理するクラス。
    /// INotifyPropertyChangedを実装し、UIへの自動更新を提供します。
    /// </summary>
    public class TaskItem : INotifyPropertyChanged
    {
        private string name;
        private string memo;
        private TimeSpan elapsedTime;
        private bool isCompleted;

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
        /// タスクの完了状態
        /// </summary>
        public bool IsCompleted
        {
            get => isCompleted;
            set
            {
                isCompleted = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// タスクの完了日時
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// タスクの作成日時
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// プロパティ変更通知イベント
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// プロパティ変更通知を発行
        /// </summary>
        /// <param name="propertyName">変更されたプロパティ名</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// JSONデシリアライズ用のコンストラクタ
        /// </summary>
        [JsonConstructor]
        public TaskItem()
        {
            Name = "";
            Memo = "";
            EstimatedTime = TimeSpan.Zero;
            ElapsedTime = TimeSpan.Zero;
            IsCompleted = false;
            CreatedAt = DateTime.Now;
        }

        /// <summary>
        /// 新規タスク作成用のコンストラクタ
        /// </summary>
        /// <param name="name">タスク名</param>
        /// <param name="memo">メモ</param>
        /// <param name="estimatedTime">予定時間</param>
        public TaskItem(string name, string memo, TimeSpan estimatedTime)
        {
            Name = name;
            Memo = memo;
            EstimatedTime = estimatedTime;
            ElapsedTime = TimeSpan.Zero;
            IsCompleted = false;
            CreatedAt = DateTime.Now;
        }

        /// <summary>
        /// タスクを完了状態にする
        /// </summary>
        public void Complete()
        {
            IsCompleted = true;
            CompletedAt = DateTime.Now;
        }

        /// <summary>
        /// タスクの経過時間を追加
        /// </summary>
        /// <param name="duration">追加する時間</param>
        public void AddElapsedTime(TimeSpan duration)
        {
            ElapsedTime += duration;
        }
    }
}