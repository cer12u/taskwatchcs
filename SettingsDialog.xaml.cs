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
        private readonly Settings settings;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SettingsDialog()
        {
            InitializeComponent();
            settingsService = new SettingsService(new TaskLogger());
            settings = settingsService.GetSettings();
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
            HoursComboBox.SelectedItem = settings.ResetTime.Hours;
            MinutesComboBox.SelectedItem = settings.ResetTime.Minutes - (settings.ResetTime.Minutes % 5);

            // 通知設定
            NotificationsEnabledCheckBox.IsChecked = settings.NotificationsEnabled;
            NotificationIntervalComboBox.SelectedItem = settings.NotificationInterval;
            EstimatedTimeNotificationCheckBox.IsChecked = settings.EstimatedTimeNotificationEnabled;

            // アーカイブ設定
            AutoArchiveEnabledCheckBox.IsChecked = settings.AutoArchiveEnabled;
            InactiveTasksEnabledCheckBox.IsChecked = settings.InactiveTasksEnabled;

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
            var oldResetTime = settings.ResetTime;
            var oldAutoArchive = settings.AutoArchiveEnabled;
            var oldInactiveTasks = settings.InactiveTasksEnabled;

            // 新しい設定を保存
            settings.ResetTime = new TimeSpan(hours, minutes, 0);
            settings.NotificationsEnabled = NotificationsEnabledCheckBox.IsChecked ?? false;
            settings.NotificationInterval = (int)(NotificationIntervalComboBox.SelectedItem ?? 30);
            settings.EstimatedTimeNotificationEnabled = EstimatedTimeNotificationCheckBox.IsChecked ?? false;
            settings.AutoArchiveEnabled = AutoArchiveEnabledCheckBox.IsChecked ?? true;
            settings.InactiveTasksEnabled = InactiveTasksEnabledCheckBox.IsChecked ?? true;

            // 設定が変更された場合、確認メッセージを表示
            if (oldResetTime != settings.ResetTime ||
                oldAutoArchive != settings.AutoArchiveEnabled ||
                oldInactiveTasks != settings.InactiveTasksEnabled)
            {
                MessageBox.Show("設定を変更しました。次回のリセット時刻から新しい設定が適用されます。",
                              "設定変更",
                              MessageBoxButton.OK,
                              MessageBoxImage.Information);
            }

            settingsService.SaveSettings(settings);

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