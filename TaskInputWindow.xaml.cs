using System;
using System.Windows;
using System.Windows.Controls;

namespace TaskManager
{
    /// <summary>
    /// 新規タスク作成ダイアログ。
    /// タスク名、メモ、予定時間の入力機能を提供します。
    /// </summary>
    public partial class TaskInputWindow : Window
    {
        private readonly TaskLogger logger = new();

        /// <summary>
        /// 作成されたタスク
        /// </summary>
        public TaskItem? CreatedTask { get; private set; }

        /// <summary>
        /// TaskInputWindowのコンストラクタ
        /// </summary>
        public TaskInputWindow()
        {
            logger.LogTrace("TaskInputWindow コンストラクタ開始");
            
            try
            {
                InitializeComponent();
                InitializeTimeComboBoxes();

                // デフォルトの優先度を設定（Medium = 1）
                PriorityComboBox.SelectedIndex = 1;
                logger.LogTrace("優先度の初期値設定: Medium (1)");

                UpdateLayout(); // レイアウトを更新
            }
            catch (Exception ex)
            {
                logger.LogTrace($"TaskInputWindow 初期化エラー: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 時間選択コンボボックスの初期化
        /// </summary>
        private void InitializeTimeComboBoxes()
        {
            logger.LogTrace("時間選択コンボボックスの初期化開始");

            try
            {
                // 時間の選択肢を設定（0-23時間）
                HoursComboBox.Items.Clear();
                for (int i = 0; i <= 23; i++)
                {
                    HoursComboBox.Items.Add(i);
                }
                logger.LogTrace($"時間コンボボックス: {HoursComboBox.Items.Count}個のアイテムを追加");

                // 分の選択肢を設定（0-55分、5分刻み）
                MinutesComboBox.Items.Clear();
                for (int i = 0; i <= 55; i += 5)
                {
                    MinutesComboBox.Items.Add(i);
                }
                logger.LogTrace($"分コンボボックス: {MinutesComboBox.Items.Count}個のアイテムを追加");

                // デフォルト値を設定（30分）
                HoursComboBox.SelectedItem = 0;
                MinutesComboBox.SelectedItem = 30;
                logger.LogTrace("デフォルト値を設定: 0時間30分");

                // コンボボックスの状態を確認
                logger.LogTrace($"時間コンボボックスの状態: SelectedItem={HoursComboBox.SelectedItem}, SelectedIndex={HoursComboBox.SelectedIndex}");
                logger.LogTrace($"分コンボボックスの状態: SelectedItem={MinutesComboBox.SelectedItem}, SelectedIndex={MinutesComboBox.SelectedIndex}");
            }
            catch (Exception ex)
            {
                logger.LogTrace($"時間選択コンボボックスの初期化エラー: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// ウィンドウ読み込み完了時の処理
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            logger.LogTrace("ウィンドウ読み込み完了");

            // コンボボックスの状態を再確認
            logger.LogTrace($"時間コンボボックスの状態: SelectedItem={HoursComboBox.SelectedItem}, SelectedIndex={HoursComboBox.SelectedIndex}");
            logger.LogTrace($"分コンボボックスの状態: SelectedItem={MinutesComboBox.SelectedItem}, SelectedIndex={MinutesComboBox.SelectedIndex}");

            // UIを強制的に更新
            HoursComboBox.UpdateLayout();
            MinutesComboBox.UpdateLayout();
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

            if (PriorityComboBox.SelectedItem == null)
            {
                MessageBox.Show("優先度を選択してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            // 優先度の取得（SelectedIndex: 0=Low, 1=Medium, 2=High）
            var priority = (TaskPriority)PriorityComboBox.SelectedIndex;
            System.Diagnostics.Debug.WriteLine($"Selected Priority Index: {PriorityComboBox.SelectedIndex}, Priority: {priority}");

            var task = new TaskItem(
                TitleTextBox.Text.Trim(),
                MemoTextBox.Text?.Trim() ?? "",
                estimatedTime
            );
            task.Priority = priority;
            CreatedTask = task;

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