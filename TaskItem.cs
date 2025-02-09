using System;
using System.Text.Json.Serialization;

namespace TaskManager
{
    public class TaskItem
    {
        public string Name { get; set; }
        public string Memo { get; set; }
        public TimeSpan EstimatedTime { get; set; }
        public TimeSpan ElapsedTime { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime CreatedAt { get; set; }

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