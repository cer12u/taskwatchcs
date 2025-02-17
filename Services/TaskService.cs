using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace TaskManager.Services
{
    public class TaskService
    {
        private readonly ObservableCollection<TaskItem> inProgressTasks;
        private readonly ObservableCollection<TaskItem> pendingTasks;
        private readonly ObservableCollection<TaskItem> completedTasks;
        private readonly TaskLogger logger;
        private readonly TaskManagerService taskManager;

        public TaskService(
            ObservableCollection<TaskItem> inProgressTasks,
            ObservableCollection<TaskItem> pendingTasks,
            ObservableCollection<TaskItem> completedTasks,
            TaskLogger logger,
            TaskManagerService taskManager)
        {
            this.inProgressTasks = inProgressTasks;
            this.pendingTasks = pendingTasks;
            this.completedTasks = completedTasks;
            this.logger = logger;
            this.taskManager = taskManager;
        }

        public void AddTask(TaskItem task)
        {
            inProgressTasks.Add(task);
            SaveTasks();
        }

        public void DeleteTask(TaskItem task)
        {
            switch (task.Status)
            {
                case TaskStatus.InProgress:
                    inProgressTasks.Remove(task);
                    break;
                case TaskStatus.Pending:
                    pendingTasks.Remove(task);
                    break;
                case TaskStatus.Completed:
                    completedTasks.Remove(task);
                    break;
            }
            SaveTasks();
        }

        public void CompleteTask(TaskItem task)
        {
            switch (task.Status)
            {
                case TaskStatus.InProgress:
                    inProgressTasks.Remove(task);
                    break;
                case TaskStatus.Pending:
                    pendingTasks.Remove(task);
                    break;
            }

            task.SetCompleted();
            completedTasks.Add(task);
            logger.LogTaskComplete(task);
            SaveTasks();
        }

        public void SetPendingTask(TaskItem task)
        {
            inProgressTasks.Remove(task);
            task.SetPending();
            pendingTasks.Add(task);
            logger.LogTaskStop(task, TimeSpan.Zero);
            SaveTasks();
        }

        public void SetInProgressTask(TaskItem task)
        {
            switch (task.Status)
            {
                case TaskStatus.Pending:
                    pendingTasks.Remove(task);
                    break;
                case TaskStatus.Completed:
                    completedTasks.Remove(task);
                    break;
            }

            task.SetInProgress();
            inProgressTasks.Add(task);
            SaveTasks();
        }

        private void SaveTasks()
        {
            var result = taskManager.SaveTasks();
            if (!result.Success)
            {
                logger.LogError("タスクの保存中にエラーが発生しました", result.Exception);
                throw new InvalidOperationException("タスクの保存に失敗しました", result.Exception);
            }
        }
    }
}