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
        private readonly ExceptionHandlingService exceptionHandler;

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
            this.exceptionHandler = new ExceptionHandlingService(logger);
        }

        public void AddTask(TaskItem task)
        {
            exceptionHandler.SafeExecute("タスクの追加", () =>
            {
                inProgressTasks.Add(task);
                SaveTasks();
            });
        }

        public void DeleteTask(TaskItem task)
        {
            exceptionHandler.SafeExecute("タスクの削除", () =>
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
            });
        }

        public void CompleteTask(TaskItem task)
        {
            exceptionHandler.SafeExecute("タスクの完了", () =>
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
            });
        }

        public void SetPendingTask(TaskItem task)
        {
            exceptionHandler.SafeExecute("タスクの保留", () =>
            {
                inProgressTasks.Remove(task);
                task.SetPending();
                pendingTasks.Add(task);
                logger.LogTaskStop(task, TimeSpan.Zero);
                SaveTasks();
            });
        }

        public void SetInProgressTask(TaskItem task)
        {
            exceptionHandler.SafeExecute("タスクの進行中設定", () =>
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
            });
        }

        private void SaveTasks()
        {
            var result = taskManager.SaveTasks();
            if (!result.Success)
            {
                throw new TaskManagerException("タスクの保存に失敗しました", result.Exception);
            }
        }
    }
}