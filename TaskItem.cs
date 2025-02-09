using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace TaskManager
{
    public class TaskItem : INotifyPropertyChanged
    {
        private string name;
        private string memo;
        private TimeSpan elapsedTime;
        private bool isCompleted;

        public string Name
        {
            get => name;
            set
            {
                name = value;
                OnPropertyChanged();
            }
        }

        public string Memo
        {
            get => memo;
            set
            {
                memo = value;
                OnPropertyChanged();
            }
        }

        public TimeSpan EstimatedTime { get; set; }

        public TimeSpan ElapsedTime
        {
            get => elapsedTime;
            set
            {
                elapsedTime = value;
                OnPropertyChanged();
            }
        }

        public bool IsCompleted
        {
            get => isCompleted;
            set
            {
                isCompleted = value;
                OnPropertyChanged();
            }
        }

        public DateTime? CompletedAt { get; set; }
        public DateTime CreatedAt { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

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

        public TaskItem(string name, string memo, TimeSpan estimatedTime)
        {
            Name = name;
            Memo = memo;
            EstimatedTime = estimatedTime;
            ElapsedTime = TimeSpan.Zero;
            IsCompleted = false;
            CreatedAt = DateTime.Now;
        }

        public void Complete()
        {
            IsCompleted = true;
            CompletedAt = DateTime.Now;
        }

        public void AddElapsedTime(TimeSpan duration)
        {
            ElapsedTime += duration;
        }
    }
}