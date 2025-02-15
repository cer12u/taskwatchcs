using System;

namespace TaskManager
{
    public class TimerState
    {
        private readonly TaskLogger? logger;

        public DateTime StartTime { get; private set; }
        public TaskItem? ActiveTask { get; private set; }
        public bool IsRunning { get; private set; }

        public TimerState(TaskLogger? logger = null)
        {
            this.logger = logger;
            Reset();
        }

        public void Start(TaskItem? task)
        {
            StartTime = DateTime.Now;
            ActiveTask = task;
            IsRunning = true;

            if (task != null)
            {
                task.IsProcessing = true;
                logger?.LogTrace($"タイマー開始: タスク '{task.Name}'");
            }
            else
            {
                logger?.LogTrace("タイマー開始: その他のタスク");
            }
        }

        public void Stop()
        {
            if (IsRunning && ActiveTask != null)
            {
                var elapsed = DateTime.Now - StartTime;
                ActiveTask.AddElapsedTime(elapsed);
                ActiveTask.IsProcessing = false;
                logger?.LogTrace($"タイマー停止: タスク '{ActiveTask.Name}', 経過時間: {elapsed}");
            }
            Reset();
        }

        public void Reset()
        {
            if (IsRunning)
            {
                logger?.LogTrace("タイマーリセット");
            }
            StartTime = DateTime.Now;
            ActiveTask = null;
            IsRunning = false;
        }

        public TimeSpan GetDisplayTime(TaskItem? selectedTask)
        {
            if (!IsRunning)
            {
                return selectedTask?.ElapsedTime ?? TimeSpan.Zero;
            }

            if (selectedTask == ActiveTask)
            {
                return (selectedTask?.ElapsedTime ?? TimeSpan.Zero) + (DateTime.Now - StartTime);
            }

            return selectedTask?.ElapsedTime ?? TimeSpan.Zero;
        }
    }
}