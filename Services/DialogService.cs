using System;
using System.Windows;

namespace TaskManager
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
    }
}