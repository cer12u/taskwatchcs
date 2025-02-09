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
            InitializeTimeComboBoxes();
        }

        private void InitializeTimeComboBoxes()
        {
            // 時間の選択肢を設定（0-23時間）
            for (int i = 0; i <= 23; i++)
            {
                HoursComboBox.Items.Add(i);
            }

            // 分の選択肢を設定（0-55分、5分刻み）
            for (int i = 0; i <= 55; i += 5)
            {
                MinutesComboBox.Items.Add(i);
            }

            // デフォルト値を設定（1時間）
            HoursComboBox.SelectedItem = 1;
            MinutesComboBox.SelectedItem = 0;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
            {
                MessageBox.Show("タイトルを入力してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (HoursComboBox.SelectedItem == null || MinutesComboBox.SelectedItem == null)
            {
                MessageBox.Show("予定時間を選択してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int hours = (int)HoursComboBox.SelectedItem;
            int minutes = (int)MinutesComboBox.SelectedItem;
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