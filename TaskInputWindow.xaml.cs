using System;
using System.Windows;

namespace TaskManager
{
    /// <summary>
    /// 新規タスク作成ダイアログ。
    /// タスク名、メモ、予定時間の入力機能を提供します。
    /// </summary>
    public partial class TaskInputWindow : Window
    {
        /// <summary>
        /// 作成されたタスク
        /// </summary>
        public TaskItem? CreatedTask { get; private set; }

        /// <summary>
        /// その他タスクの経過時間を加算するかどうか
        /// </summary>
        public bool AddOtherTime { get; private set; }

        /// <summary>
        /// その他タスクの経過時間
        /// </summary>
        private TimeSpan otherTaskTime;

        /// <summary>
        /// TaskInputWindowのコンストラクタ
        /// </summary>
        /// <param name="otherTaskElapsedTime">その他タスクの経過時間（ある場合）</param>
        public TaskInputWindow(TimeSpan? otherTaskElapsedTime = null)
        {
            InitializeComponent();
            InitializeTimeComboBoxes();

            // その他タスクの時間がある場合のみチェックボックスを表示
            if (otherTaskElapsedTime.HasValue && otherTaskElapsedTime.Value > TimeSpan.Zero)
            {
                otherTaskTime = otherTaskElapsedTime.Value;
                AddOtherTimeCheckBox.Content = $"「その他」の作業時間 ({otherTaskElapsedTime.Value:hh\\:mm}) を加算する";
                AddOtherTimeCheckBox.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// 時間選択コンボボックスの初期化
        /// </summary>
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

            // デフォルト値を設定（30分）
            HoursComboBox.SelectedItem = 0;
            MinutesComboBox.SelectedItem = 30;
        }

        /// <summary>
        /// 追加ボタンクリック時の処理
        /// </summary>
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
            {
                MessageBox.Show("タイトルを入力してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                TitleTextBox.Focus();
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

            // その他タスクの時間を加算
            if (AddOtherTimeCheckBox.IsChecked == true)
            {
                CreatedTask.AddElapsedTime(otherTaskTime);
                AddOtherTime = true;
            }

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