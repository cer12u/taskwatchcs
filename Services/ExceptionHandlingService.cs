using System;
using System.Windows;

namespace TaskManager.Services
{
    public class ExceptionHandlingService
    {
        private readonly TaskLogger logger;

        public ExceptionHandlingService(TaskLogger logger)
        {
            this.logger = logger;
        }

        public void HandleException(string operation, Exception ex, string? userMessage = null)
        {
            if (ex is TaskManagerException)
            {
                logger.LogError($"操作失敗: {operation}", ex);
                ShowErrorMessage(ex.Message, ex);
            }
            else
            {
                logger.LogError($"予期せぬエラー: {operation}", ex);
                ShowErrorMessage(userMessage ?? "予期せぬエラーが発生しました", ex);
            }
        }

        public void SafeExecute(string operation, Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                HandleException(operation, ex);
            }
        }

        private void ShowErrorMessage(string message, Exception? ex = null)
        {
            var details = ex != null ? $"\n\n詳細: {ex.Message}" : "";
            MessageBox.Show(
                $"{message}{details}",
                "エラー",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
    }
}