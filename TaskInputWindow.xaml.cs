using System;
using System.Windows;

namespace TaskManager
{
    public partial class TaskInputWindow : Window
    {
        public TaskItem? CreatedTask { get; private set; }

        public TaskInputWindow()
        {
            InitializeComponent();
            HoursTextBox.Text = "1";
            MinutesTextBox.Text = "0";
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
            {
                MessageBox.Show("タイトルを入力してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(HoursTextBox.Text, out int hours) || hours < 0)
            {
                MessageBox.Show("時間は0以上の数値を入力してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(MinutesTextBox.Text, out int minutes) || minutes < 0 || minutes >= 60)
            {
                MessageBox.Show("分は0以上60未満の数値を入力してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var estimatedTime = new TimeSpan(hours, minutes, 0);
            CreatedTask = new TaskItem(
                TitleTextBox.Text.Trim(),
                MemoTextBox.Text?.Trim() ?? "",
                estimatedTime
            );

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