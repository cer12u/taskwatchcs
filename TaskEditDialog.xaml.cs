using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

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
        /// 編集後の優先度
        /// </summary>
        public TaskPriority Priority { get; private set; }

        private static readonly Regex TimeFormatRegex = new(@"^([0-9]{1,2}):([0-5][0-9]):([0-5][0-9])$");

        /// <summary>
        /// TaskEditDialogのコンストラクタ
        /// </summary>
        public TaskEditDialog(string taskName, string memo, TimeSpan estimatedTime, TimeSpan elapsedTime, TaskPriority priority)
        {
            logger.LogTrace($"TaskEditDialog コンストラクタ開始: taskName={taskName}, estimatedTime={estimatedTime}, elapsedTime={elapsedTime}, priority={priority}");
            
            try
            {
                InitializeComponent();

                // 時間選択コンボボックスを初期化
                InitializeTimeComboBoxes();
                UpdateLayout(); // レイアウトを更新

                // 予定時間の設定
                var hours = (int)estimatedTime.TotalHours;
                var rawMinutes = estimatedTime.Minutes;
                // 5分単位に丸める（切り上げ）
                var minutes = ((rawMinutes + 4) / 5 * 5) % 60;
                if (minutes < rawMinutes) hours++; // 60分を超えた場合は時間を1増やす
                
                logger.LogTrace($"予定時間の初期値設定: 入力値={estimatedTime}, 生の分={rawMinutes}, 変換後={hours}時間{minutes}分");

                // コンボボックスの選択前の状態を確認
                logger.LogTrace($"時間コンボボックスの選択前: Items.Count={EstimatedHoursComboBox.Items.Count}, SelectedItem={EstimatedHoursComboBox.SelectedItem}");
                logger.LogTrace($"分コンボボックスの選択前: Items.Count={EstimatedMinutesComboBox.Items.Count}, SelectedItem={EstimatedMinutesComboBox.SelectedItem}");

                // 時間の値を設定
                EstimatedHoursComboBox.SelectedItem = hours;
                EstimatedMinutesComboBox.SelectedItem = minutes;
                UpdateLayout(); // レイアウトを更新

                // その他の項目を設定
                TitleTextBox.Text = taskName;
                MemoTextBox.Text = memo;

                // コンボボックスの選択後の状態を確認
                logger.LogTrace($"時間コンボボックスの選択後: SelectedItem={EstimatedHoursComboBox.SelectedItem}, SelectedIndex={EstimatedHoursComboBox.SelectedIndex}");
                logger.LogTrace($"分コンボボックスの選択後: SelectedItem={EstimatedMinutesComboBox.SelectedItem}, SelectedIndex={EstimatedMinutesComboBox.SelectedIndex}");

                // 経過時間の設定
                ElapsedTimeTextBox.Text = elapsedTime.ToString(@"hh\:mm\:ss");
                logger.LogTrace($"経過時間の初期値設定: {ElapsedTimeTextBox.Text}");

                // 優先度の設定
                PriorityComboBox.SelectedIndex = (int)priority;
                logger.LogTrace($"優先度の初期値設定: {priority} (index: {(int)priority})");
            }
            catch (Exception ex)
            {
                logger.LogTrace($"TaskEditDialog 初期化エラー: {ex.Message}");
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
                logger.LogTrace("時間選択コンボボックスの初期化開始");

                // 時間の選択肢を設定（0-23時間）
                EstimatedHoursComboBox.Items.Clear();
                for (int i = 0; i <= 23; i++)
                {
                    EstimatedHoursComboBox.Items.Add(i);
                }
                logger.LogTrace($"時間コンボボックス: {EstimatedHoursComboBox.Items.Count}個のアイテムを追加");

                // 分の選択肢を設定（0-55分、5分刻み）
                EstimatedMinutesComboBox.Items.Clear();
                for (int i = 0; i <= 55; i += 5)
                {
                    EstimatedMinutesComboBox.Items.Add(i);
                }
                logger.LogTrace($"分コンボボックス: {EstimatedMinutesComboBox.Items.Count}個のアイテムを追加");

                // コンボボックスの状態を確認
                logger.LogTrace($"時間コンボボックスの状態: IsEnabled={EstimatedHoursComboBox.IsEnabled}, IsVisible={EstimatedHoursComboBox.IsVisible}, Items.Count={EstimatedHoursComboBox.Items.Count}");
                logger.LogTrace($"分コンボボックスの状態: IsEnabled={EstimatedMinutesComboBox.IsEnabled}, IsVisible={EstimatedMinutesComboBox.IsVisible}, Items.Count={EstimatedMinutesComboBox.Items.Count}");

                // 選択状態を確認
                logger.LogTrace($"時間コンボボックスの選択状態: SelectedItem={EstimatedHoursComboBox.SelectedItem}, SelectedIndex={EstimatedHoursComboBox.SelectedIndex}");
                logger.LogTrace($"分コンボボックスの選択状態: SelectedItem={EstimatedMinutesComboBox.SelectedItem}, SelectedIndex={EstimatedMinutesComboBox.SelectedIndex}");
            }
            catch (Exception ex)
            {
                logger.LogTrace($"時間選択コンボボックスの初期化エラー: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// 経過時間の入力値を検証
        /// </summary>
        private readonly TaskLogger logger = new();

        private bool ValidateElapsedTime(out TimeSpan elapsedTime)
        {
            elapsedTime = TimeSpan.Zero;
            logger.LogTrace($"経過時間の検証開始: 入力値 = {ElapsedTimeTextBox.Text}");

            var match = TimeFormatRegex.Match(ElapsedTimeTextBox.Text);
            if (!match.Success)
            {
                logger.LogValidation("経過時間", false, $"不正な形式: {ElapsedTimeTextBox.Text}");
                MessageBox.Show("経過時間は HH:mm:ss 形式で入力してください。\n例: 01:30:45",
                              "エラー",
                              MessageBoxButton.OK,
                              MessageBoxImage.Warning);
                ElapsedTimeTextBox.Focus();
                return false;
            }

            int hours = int.Parse(match.Groups[1].Value);
            int minutes = int.Parse(match.Groups[2].Value);
            int seconds = int.Parse(match.Groups[3].Value);
            logger.LogTrace($"解析結果: hours={hours}, minutes={minutes}, seconds={seconds}");

            if (hours > 23)
            {
                logger.LogValidation("経過時間", false, $"時間が範囲外: {hours}");
                MessageBox.Show("時間は0-23の範囲で入力してください。",
                              "エラー",
                              MessageBoxButton.OK,
                              MessageBoxImage.Warning);
                ElapsedTimeTextBox.Focus();
                return false;
            }

            elapsedTime = new TimeSpan(hours, minutes, seconds);
            logger.LogValidation("経過時間", true, $"検証成功: {elapsedTime}");
            return true;
        }

        /// <summary>
        /// 時間コンボボックスの選択変更時の処理
        /// </summary>
        private void EstimatedHoursComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EstimatedHoursComboBox.SelectedItem is int hours)
            {
                logger.LogTrace($"時間が変更されました: {hours}時間");

                // 23時間を超える場合は23時間に制限
                if (hours > 23)
                {
                    logger.LogTrace($"時間が制限されました: {hours} -> 23");
                    EstimatedHoursComboBox.SelectedItem = 23;
                    return;
                }

                logger.LogTrace($"ComboBox状態: SelectedItem={EstimatedHoursComboBox.SelectedItem}, SelectedIndex={EstimatedHoursComboBox.SelectedIndex}");
            }
        }

        /// <summary>
        /// 分コンボボックスの選択変更時の処理
        /// </summary>
        private void EstimatedMinutesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EstimatedMinutesComboBox.SelectedItem is int minutes)
            {
                logger.LogTrace($"分が変更されました: {minutes}分");

                // 5分単位に丸める
                int roundedMinutes = ((minutes + 4) / 5 * 5) % 60;
                if (roundedMinutes != minutes)
                {
                    logger.LogTrace($"分が5分単位に丸められました: {minutes} -> {roundedMinutes}");
                    EstimatedMinutesComboBox.SelectedItem = roundedMinutes;
                    return;
                }

                // 55分を超える場合は55分に制限
                if (minutes > 55)
                {
                    logger.LogTrace($"分が制限されました: {minutes} -> 55");
                    EstimatedMinutesComboBox.SelectedItem = 55;
                    return;
                }

                logger.LogTrace($"ComboBox状態: SelectedItem={EstimatedMinutesComboBox.SelectedItem}, SelectedIndex={EstimatedMinutesComboBox.SelectedIndex}");
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

            logger.LogTrace("タスク編集の保存開始");

            // 予定時間の検証
            if (EstimatedHoursComboBox.SelectedItem == null || EstimatedMinutesComboBox.SelectedItem == null)
            {
                logger.LogValidation("予定時間", false, "未選択");
                MessageBox.Show("予定時間を選択してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 経過時間の検証
            if (!ValidateElapsedTime(out TimeSpan elapsedTime))
            {
                return;
            }

            // 優先度の検証
            if (PriorityComboBox.SelectedItem == null)
            {
                logger.LogValidation("優先度", false, "未選択");
                MessageBox.Show("優先度を選択してください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            TaskName = TitleTextBox.Text.Trim();
            Memo = MemoTextBox.Text?.Trim() ?? "";

            // 予定時間の設定
            int estimatedHours = (int)EstimatedHoursComboBox.SelectedItem;
            int estimatedMinutes = (int)EstimatedMinutesComboBox.SelectedItem;
            EstimatedTime = new TimeSpan(estimatedHours, estimatedMinutes, 0);
            logger.LogTrace($"予定時間設定: {estimatedHours}時間 {estimatedMinutes}分 => {EstimatedTime}");

            // 経過時間の設定
            ElapsedTime = elapsedTime;
            logger.LogTrace($"経過時間設定: {ElapsedTime}");

            // 優先度の保存（SelectedIndex: 0=Low, 1=Medium, 2=High）
            Priority = (TaskPriority)PriorityComboBox.SelectedIndex;
            System.Diagnostics.Debug.WriteLine($"Selected Priority Index: {PriorityComboBox.SelectedIndex}, Priority: {Priority}");

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

        /// <summary>
        /// ウィンドウ読み込み完了時の処理
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            logger.LogTrace("ウィンドウ読み込み完了");

            // コンボボックスの状態を再確認
            logger.LogTrace($"時間コンボボックスの状態: SelectedItem={EstimatedHoursComboBox.SelectedItem}, SelectedIndex={EstimatedHoursComboBox.SelectedIndex}");
            logger.LogTrace($"分コンボボックスの状態: SelectedItem={EstimatedMinutesComboBox.SelectedItem}, SelectedIndex={EstimatedMinutesComboBox.SelectedIndex}");

            // UIを強制的に更新
            EstimatedHoursComboBox.UpdateLayout();
            EstimatedMinutesComboBox.UpdateLayout();
            ElapsedTimeTextBox.UpdateLayout();
        }
    }
}