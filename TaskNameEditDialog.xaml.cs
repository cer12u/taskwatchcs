using System.Windows;

namespace TaskManager
{
    public partial class TaskNameEditDialog : Window
    {
        public string TaskName { get; private set; }

        public TaskNameEditDialog(string currentName)
        {
            InitializeComponent();
            TaskNameTextBox.Text = currentName;
            TaskName = currentName;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TaskNameTextBox.Text))
            {
                MessageBox.Show("タスク名を入力してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            TaskName = TaskNameTextBox.Text.Trim();
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