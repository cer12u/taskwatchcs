using System;

namespace TaskManager
{
    public class TaskItem
    {
        public string Name { get; set; }
        public string Memo { get; set; }
        public TimeSpan EstimatedTime { get; set; }
        public TimeSpan ElapsedTime { get; set; }

        public TaskItem(string name, string memo, TimeSpan estimatedTime)
        {
            Name = name;
            Memo = memo;
            EstimatedTime = estimatedTime;
            ElapsedTime = TimeSpan.Zero;
        }
    }
}