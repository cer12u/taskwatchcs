using System.Windows;

namespace TaskManager
{
    public partial class TaskEditDialog : Window
    {
        public string TaskName { get; private set; }
        public string Memo { get; private set; }

        public TaskEditDialog(string currentName, string currentMemo)
        {
            InitializeComponent();
            TaskNameTextBox.Text = currentName;
            MemoTextBox.Text = currentMemo;
            TaskName = currentName;
            Memo = currentMemo;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TaskNameTextBox.Text))
            {
                MessageBox.Show("タスク名を入力してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                TaskNameTextBox.Focus();
                return;
            }

            TaskName = TaskNameTextBox.Text.Trim();
            Memo = MemoTextBox.Text?.Trim() ?? "";
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}