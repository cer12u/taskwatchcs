using System;
using System.Windows;

namespace TaskManager
{
    /// <summary>
    /// タスク編集ダイアログ。
    /// タスク名、メモ、予定時間、経過時間の編集機能を提供します。
    /// </summary>
    public partial class TaskEditDialog : Window
    {
        /// <summary>
        /// 編集後のタスク名
        /// </summary>
        public string? TaskName { get; private set; }

        /// <summary>
        /// 編集後のメモ
        /// </summary>
        public string? Memo { get; private set; }

        /// <summary>
        /// 編集後の予定時間
        /// </summary>
        public TimeSpan EstimatedTime { get; private set; }

        /// <summary>
        /// 編集後の経過時間
        /// </summary>
        public TimeSpan ElapsedTime { get; private set; }

        /// <summary>
        /// TaskEditDialogのコンストラクタ
        /// </summary>
        public TaskEditDialog(string taskName, string memo, TimeSpan estimatedTime, TimeSpan elapsedTime)
        {
            InitializeComponent();
            InitializeTimeComboBoxes();

            TitleTextBox.Text = taskName;
            MemoTextBox.Text = memo;

            // 予定時間の設定
            EstimatedHoursComboBox.SelectedItem = (int)estimatedTime.TotalHours;
            EstimatedMinutesComboBox.SelectedItem = estimatedTime.Minutes;

            // 経過時間の設定
            ElapsedHoursComboBox.SelectedItem = (int)elapsedTime.TotalHours;
            ElapsedMinutesComboBox.SelectedItem = elapsedTime.Minutes;
        }

        /// <summary>
        /// 時間選択コンボボックスの初期化
        /// </summary>
        private void InitializeTimeComboBoxes()
        {
            // 時間の選択肢を設定（0-23時間）
            for (int i = 0; i <= 23; i++)
            {
                EstimatedHoursComboBox.Items.Add(i);
                ElapsedHoursComboBox.Items.Add(i);
            }

            // 分の選択肢を設定（0-55分、5分刻み）
            for (int i = 0; i <= 55; i += 5)
            {
                EstimatedMinutesComboBox.Items.Add(i);
                ElapsedMinutesComboBox.Items.Add(i);
            }
        }

        /// <summary>
        /// 保存ボタンクリック時の処理
        /// </summary>
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
            {
                MessageBox.Show("タイトルを入力してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                TitleTextBox.Focus();
                return;
            }

            if (EstimatedHoursComboBox.SelectedItem == null || EstimatedMinutesComboBox.SelectedItem == null ||
                ElapsedHoursComboBox.SelectedItem == null || ElapsedMinutesComboBox.SelectedItem == null)
            {
                MessageBox.Show("時間を選択してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            TaskName = TitleTextBox.Text.Trim();
            Memo = MemoTextBox.Text?.Trim() ?? "";

            int estimatedHours = (int)EstimatedHoursComboBox.SelectedItem;
            int estimatedMinutes = (int)EstimatedMinutesComboBox.SelectedItem;
            EstimatedTime = new TimeSpan(estimatedHours, estimatedMinutes, 0);

            int elapsedHours = (int)ElapsedHoursComboBox.SelectedItem;
            int elapsedMinutes = (int)ElapsedMinutesComboBox.SelectedItem;
            ElapsedTime = new TimeSpan(elapsedHours, elapsedMinutes, 0);

            DialogResult = true;
            Close();
        }

        /// <summary>
        /// キャンセルボタンクリック時の処理
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}