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
            ElapsedHoursTextBox.Text = ((int)elapsedTime.TotalHours).ToString();
            ElapsedMinutesTextBox.Text = elapsedTime.Minutes.ToString();
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
            }

            // 分の選択肢を設定（0-55分、5分刻み）
            for (int i = 0; i <= 55; i += 5)
            {
                EstimatedMinutesComboBox.Items.Add(i);
            }
        }

        /// <summary>
        /// 経過時間の入力値を検証
        /// </summary>
        private bool ValidateElapsedTime(out int hours, out int minutes)
        {
            hours = 0;
            minutes = 0;

            // 時間の検証
            if (!int.TryParse(ElapsedHoursTextBox.Text, out hours) || hours < 0)
            {
                MessageBox.Show("経過時間（時間）は0以上の整数を入力してください。", 
                              "エラー", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Warning);
                ElapsedHoursTextBox.Focus();
                return false;
            }

            // 分の検証
            if (!int.TryParse(ElapsedMinutesTextBox.Text, out minutes) || minutes < 0 || minutes > 59)
            {
                MessageBox.Show("経過時間（分）は0-59の整数を入力してください。", 
                              "エラー", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Warning);
                ElapsedMinutesTextBox.Focus();
                return false;
            }

            return true;
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

            if (EstimatedHoursComboBox.SelectedItem == null || EstimatedMinutesComboBox.SelectedItem == null)
            {
                MessageBox.Show("予定時間を選択してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!ValidateElapsedTime(out int elapsedHours, out int elapsedMinutes))
            {
                return;
            }

            TaskName = TitleTextBox.Text.Trim();
            Memo = MemoTextBox.Text?.Trim() ?? "";

            int estimatedHours = (int)EstimatedHoursComboBox.SelectedItem;
            int estimatedMinutes = (int)EstimatedMinutesComboBox.SelectedItem;
            EstimatedTime = new TimeSpan(estimatedHours, estimatedMinutes, 0);
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