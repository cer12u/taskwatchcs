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
            ElapsedTimeTextBox.TextChanged += ElapsedTimeTextBox_TextChanged;
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

            // 予定時間の更新
            if (EstimatedHoursComboBox.SelectedItem != null && EstimatedMinutesComboBox.SelectedItem != null)
            {
                int hours = (int)EstimatedHoursComboBox.SelectedItem;
                int minutes = (int)EstimatedMinutesComboBox.SelectedItem;
                EstimatedTime = new TimeSpan(hours, minutes, 0);
            }

            // 経過時間の検証と更新
            if (TimeSpan.TryParse(ElapsedTimeTextBox.Text, out TimeSpan elapsedTime))
            {
                if (elapsedTime.TotalHours > 999999) // 不当に大きな値をチェック
                {
                    MessageBox.Show("経過時間が大きすぎます。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                ElapsedTime = elapsedTime;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("経過時間の形式が正しくありません。HH:MM:SS形式で入力してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                ElapsedTimeTextBox.Focus();
                return;
            }
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

        private void ElapsedTimeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TimeSpan.TryParse(ElapsedTimeTextBox.Text, out TimeSpan parsed))
            {
                ElapsedTimeTextBox.Background = Brushes.White;
                ElapsedTimeTextBox.ToolTip = null;
            }
            else
            {
                ElapsedTimeTextBox.Background = new SolidColorBrush(Color.FromRgb(255, 200, 200));
                ElapsedTimeTextBox.ToolTip = "形式: HH:MM:SS";
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
            {
                MessageBox.Show("タスク名を入力してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            TaskName = TitleTextBox.Text.Trim();
            Memo = MemoTextBox.Text.Trim();
            Priority = (TaskPriority)PriorityComboBox.SelectedIndex;

            // 予定時間の取得
            int hours = (int)EstimatedHoursComboBox.SelectedItem;
            int minutes = (int)EstimatedMinutesComboBox.SelectedItem;
            EstimatedTime = new TimeSpan(hours, minutes, 0);

            // 経過時間の検証
            if (TimeSpan.TryParse(ElapsedTimeTextBox.Text, out TimeSpan elapsedTime))
            {
                if (elapsedTime.TotalHours > 999999) // 不当に大きな値をチェック
                {
                    MessageBox.Show("経過時間が大きすぎます。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                ElapsedTime = elapsedTime;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("経過時間の形式が正しくありません。HH:MM:SS形式で入力してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                ElapsedTimeTextBox.Focus();
            }
        }
    }
}