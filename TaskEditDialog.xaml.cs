using System.Windows;

namespace TaskManager
{
    /// <summary>
    /// タスクの編集ダイアログ。
    /// タスク名とメモの編集機能を提供します。
    /// </summary>
    public partial class TaskEditDialog : Window
    {
        /// <summary>
        /// 編集後のタスク名
        /// </summary>
        public string TaskName { get; private set; }

        /// <summary>
        /// 編集後のメモ
        /// </summary>
        public string Memo { get; private set; }

        /// <summary>
        /// TaskEditDialogのコンストラクタ
        /// </summary>
        /// <param name="currentName">現在のタスク名</param>
        /// <param name="currentMemo">現在のメモ</param>
        public TaskEditDialog(string currentName, string currentMemo)
        {
            InitializeComponent();
            TaskNameTextBox.Text = currentName;
            MemoTextBox.Text = currentMemo;
            TaskName = currentName;
            Memo = currentMemo;
        }

        /// <summary>
        /// OKボタンクリック時の処理
        /// タスク名が空でないことを確認し、変更を確定します
        /// </summary>
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

        /// <summary>
        /// キャンセルボタンクリック時の処理
        /// 変更を破棄してダイアログを閉じます
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}