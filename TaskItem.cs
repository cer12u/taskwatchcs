using System;

namespace TaskManager
{
    public class TaskItem
    {
        public string Name { get; set; }
        public TimeSpan ElapsedTime { get; set; }

        public TaskItem(string name)
        {
            Name = name;
            ElapsedTime = TimeSpan.Zero;
        }
    }
}