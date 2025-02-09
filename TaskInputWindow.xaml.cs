using System;
using System.Text.RegularExpressions;
using System.Windows;

namespace TaskManager
{
    public partial class TaskInputWindow : Window
    {
        private static readonly Regex TimeRegex = new Regex(@"^([0-9]{1,2}):([0-9]{2})$");
        public TaskItem? CreatedTask { get; private set; }

        public TaskInputWindow()
        {
            InitializeComponent();
            TimeTextBox.Text = "01:00"; // デフォルト1時間
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
            {
                MessageBox.Show("タイトルを入力してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!ValidateAndParseTime(TimeTextBox.Text, out TimeSpan estimatedTime))
            {
                MessageBox.Show(
                    "予定時間は「HH:mm」の形式で入力してください。\n例: 01:30（1時間30分）", 
                    "エラー", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Warning
                );
                return;
            }

            CreatedTask = new TaskItem(
                TitleTextBox.Text.Trim(),
                MemoTextBox.Text?.Trim() ?? "",
                estimatedTime
            );

            DialogResult = true;
            Close();
        }

        private bool ValidateAndParseTime(string timeText, out TimeSpan result)
        {
            result = TimeSpan.Zero;
            
            if (string.IsNullOrWhiteSpace(timeText))
                return false;

            var match = TimeRegex.Match(timeText);
            if (!match.Success)
                return false;

            if (!int.TryParse(match.Groups[1].Value, out int hours) || 
                !int.TryParse(match.Groups[2].Value, out int minutes))
                return false;

            if (hours < 0 || minutes < 0 || minutes >= 60)
                return false;

            result = new TimeSpan(hours, minutes, 0);
            return true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}