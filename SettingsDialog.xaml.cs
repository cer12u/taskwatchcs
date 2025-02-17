using System;
using System.Windows;
using TaskManager.Services;

namespace TaskManager
{
    /// <summary>
    /// 設定ダイアログのインタラクションロジック
    /// </summary>
    public partial class SettingsDialog : Window
    {
        private readonly SettingsService settingsService;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SettingsDialog(SettingsService settingsService)
        {
            InitializeComponent();
            this.settingsService = settingsService;
            InitializeTimeComboBoxes();
            InitializeNotificationSettings();
            LoadCurrentSettings();
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
        }

        /// <summary>
        /// 通知設定コントロールの初期化
        /// </summary>
        private void InitializeNotificationSettings()
        {
            // 通知間隔の選択肢を設定（5-60分、5分刻み）
            for (int i = 5; i <= 60; i += 5)
            {
                NotificationIntervalComboBox.Items.Add(i);
            }

            // 通知設定の有効/無効に応じてコントロールの状態を更新
            NotificationsEnabledCheckBox.Checked += (s, e) => UpdateNotificationControlsState();
            NotificationsEnabledCheckBox.Unchecked += (s, e) => UpdateNotificationControlsState();
        }

        /// <summary>
        /// 通知関連コントロールの状態を更新
        /// </summary>
        private void UpdateNotificationControlsState()
        {
            bool isEnabled = NotificationsEnabledCheckBox.IsChecked ?? false;
            NotificationIntervalComboBox.IsEnabled = isEnabled;
            EstimatedTimeNotificationCheckBox.IsEnabled = isEnabled;
        }

        /// <summary>
        /// 現在の設定を読み込み
        /// </summary>
        private void LoadCurrentSettings()
        {
            // リセット時刻の設定
            HoursComboBox.SelectedItem = settingsService.Settings.ResetTime.Hours;
            MinutesComboBox.SelectedItem = settingsService.Settings.ResetTime.Minutes - (settingsService.Settings.ResetTime.Minutes % 5);

            // 通知設定
            NotificationsEnabledCheckBox.IsChecked = settingsService.Settings.NotificationsEnabled;
            NotificationIntervalComboBox.SelectedItem = settingsService.Settings.NotificationInterval;
            EstimatedTimeNotificationCheckBox.IsChecked = settingsService.Settings.EstimatedTimeNotificationEnabled;

            // アーカイブ設定
            AutoArchiveEnabledCheckBox.IsChecked = settingsService.Settings.AutoArchiveEnabled;
            InactiveTasksEnabledCheckBox.IsChecked = settingsService.Settings.InactiveTasksEnabled;

            UpdateNotificationControlsState();
        }

        /// <summary>
        /// OKボタンクリック時の処理
        /// </summary>
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (HoursComboBox.SelectedItem == null || MinutesComboBox.SelectedItem == null)
            {
                MessageBox.Show("時刻を選択してください。", 
                              "エラー", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Warning);
                return;
            }

            if (NotificationsEnabledCheckBox.IsChecked == true && NotificationIntervalComboBox.SelectedItem == null)
            {
                MessageBox.Show("通知間隔を選択してください。", 
                              "エラー", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Warning);
                return;
            }

            var hours = (int)HoursComboBox.SelectedItem;
            var minutes = (int)MinutesComboBox.SelectedItem;

            // 設定を保存する前に、現在の設定を保持
            var oldResetTime = settingsService.Settings.ResetTime;
            var oldAutoArchive = settingsService.Settings.AutoArchiveEnabled;
            var oldInactiveTasks = settingsService.Settings.InactiveTasksEnabled;

            // 新しい設定を保存
            settingsService.Settings.ResetTime = new TimeSpan(hours, minutes, 0);
            settingsService.Settings.NotificationsEnabled = NotificationsEnabledCheckBox.IsChecked ?? false;
            settingsService.Settings.NotificationInterval = (int)(NotificationIntervalComboBox.SelectedItem ?? 30);
            settingsService.Settings.EstimatedTimeNotificationEnabled = EstimatedTimeNotificationCheckBox.IsChecked ?? false;
            settingsService.Settings.AutoArchiveEnabled = AutoArchiveEnabledCheckBox.IsChecked ?? true;
            settingsService.Settings.InactiveTasksEnabled = InactiveTasksEnabledCheckBox.IsChecked ?? true;

            // 設定が変更された場合、確認メッセージを表示
            if (oldResetTime != settingsService.Settings.ResetTime ||
                oldAutoArchive != settingsService.Settings.AutoArchiveEnabled ||
                oldInactiveTasks != settingsService.Settings.InactiveTasksEnabled)
            {
                MessageBox.Show("設定を変更しました。次回のリセット時刻から新しい設定が適用されます。",
                              "設定変更",
                              MessageBoxButton.OK,
                              MessageBoxImage.Information);
            }

            settingsService.Save();

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