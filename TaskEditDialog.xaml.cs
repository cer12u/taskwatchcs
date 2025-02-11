using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace TaskManager
{
    /// <summary>
    /// タスク編集ダイアログのインタラクションロジック
    /// </summary>
    public partial class TaskEditDialog : Window
    {
        public string? TaskName { get; private set; }
        public string? Memo { get; private set; }
        public TimeSpan EstimatedTime { get; private set; }
        public TimeSpan ElapsedTime { get; private set; }
        public TaskPriority Priority { get; private set; }

        public TaskEditDialog(string taskName, string memo, TimeSpan estimatedTime, TimeSpan elapsedTime, TaskPriority priority)
        {
            InitializeComponent();

            TitleTextBox.Text = taskName;
            MemoTextBox.Text = memo;
            ElapsedTime = elapsedTime;
            ElapsedTimeTextBox.Text = elapsedTime.ToString(@"hh\:mm\:ss");
            Priority = priority;

            // 時間の選択肢を設定
            for (int i = 0; i <= 24; i++)
            {
                EstimatedHoursComboBox.Items.Add(i);
            }
            for (int i = 0; i < 60; i += 15)
            {
                EstimatedMinutesComboBox.Items.Add(i);
            }

            // 予定時間を設定
            EstimatedHoursComboBox.SelectedItem = (int)estimatedTime.TotalHours;
            EstimatedMinutesComboBox.SelectedItem = estimatedTime.Minutes - (estimatedTime.Minutes % 15);

            // 優先度を設定
            PriorityComboBox.SelectedIndex = (int)priority;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TitleTextBox.Focus();
            TitleTextBox.SelectAll();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
            {
                MessageBox.Show("タイトルを入力してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            TaskName = TitleTextBox.Text;
            Memo = MemoTextBox.Text;
            Priority = (TaskPriority)PriorityComboBox.SelectedIndex;

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void EstimatedHoursComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateEstimatedTime();
        }

        private void EstimatedMinutesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateEstimatedTime();
        }

        private void UpdateEstimatedTime()
        {
            if (EstimatedHoursComboBox.SelectedItem != null && EstimatedMinutesComboBox.SelectedItem != null)
            {
                int hours = (int)EstimatedHoursComboBox.SelectedItem;
                int minutes = (int)EstimatedMinutesComboBox.SelectedItem;
                EstimatedTime = new TimeSpan(hours, minutes, 0);
            }
        }
    }
}