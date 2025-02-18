using System;
using System.Windows;
using TaskManager.Services;

namespace TaskManager.Services
{
    public interface IDialogService
    {
        bool? ShowDialog(Window dialog);
        void ShowError(string message, string title = "エラー");
        void ShowWarning(string message, string title = "警告");
        void ShowInfo(string message, string title = "情報");
    }

    public class DialogService : IDialogService
    {
        private readonly ExceptionHandlingService exceptionHandler;
        private readonly TaskLogger logger;
        private readonly SettingsService settingsService;

        public DialogService(ExceptionHandlingService exceptionHandler, TaskLogger logger, SettingsService settingsService)
        {
            this.exceptionHandler = exceptionHandler;
            this.logger = logger;
            this.settingsService = settingsService;
        }

        public bool? ShowDialog(Window dialog)
        {
            return dialog.ShowDialog();
        }

        public void ShowError(string message, string title = "エラー")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public void ShowWarning(string message, string title = "警告")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        public void ShowInfo(string message, string title = "情報")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public bool ShowTaskEditDialog(Window owner, TaskItem task, out TaskEditDialogResult? result)
        {
            TaskEditDialogResult? tempResult = null;
            var success = exceptionHandler.ExecuteSafe("タスク編集", () =>
            {
                logger.LogTrace($"タスク編集開始: {task.Name}");
                logger.LogTrace($"編集前の値: EstimatedTime={task.EstimatedTime}, ElapsedTime={task.ElapsedTime}, Priority={task.Priority}");

                var dialog = new TaskEditDialog(
                    task.Name,
                    task.Memo ?? "",
                    task.EstimatedTime,
                    task.ElapsedTime,
                    task.Priority)
                {
                    Owner = owner
                };

                if (dialog.ShowDialog() == true && dialog.TaskName != null)
                {
                    logger.LogTrace($"タスク編集の保存: {dialog.TaskName}");
                    logger.LogTrace($"変更後の値: EstimatedTime={dialog.EstimatedTime}, ElapsedTime={dialog.ElapsedTime}, Priority={dialog.Priority}");

                    tempResult = new TaskEditDialogResult(
                        dialog.TaskName,
                        dialog.Memo ?? "",
                        dialog.EstimatedTime,
                        dialog.ElapsedTime,
                        dialog.Priority);
                    return true;
                }

                logger.LogTrace("タスク編集がキャンセルされました");
                return false;
            });

            result = tempResult;
            return success;
        }

        public bool ShowTaskInputDialog(Window owner, out TaskItem? createdTask)
        {
            var inputWindow = new TaskInputWindow { Owner = owner };
            var result = inputWindow.ShowDialog() == true;
            createdTask = inputWindow.CreatedTask;
            return result;
        }

        public bool ShowSettingsDialog(Window owner)
        {
            var dialog = new SettingsDialog(settingsService) { Owner = owner };
            return dialog.ShowDialog() == true;
        }
    }

    public record TaskEditDialogResult(
        string Name,
        string Memo,
        TimeSpan EstimatedTime,
        TimeSpan ElapsedTime,
        TaskPriority Priority);
}