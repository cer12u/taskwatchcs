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

        [JsonConstructor]
        public TaskItem()
        {
            Name = "";
            Memo = "";
            EstimatedTime = TimeSpan.Zero;
            ElapsedTime = TimeSpan.Zero;
        }

        public TaskItem(string name, string memo, TimeSpan estimatedTime)
        {
            Name = name;
            Memo = memo;
            EstimatedTime = estimatedTime;
            ElapsedTime = TimeSpan.Zero;
        }
    }
}